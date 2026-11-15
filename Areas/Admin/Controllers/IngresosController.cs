using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class IngresosController : Controller
{
    private const int TamanoPagina = 15;
    private const int MaxFilas = 100;

    private readonly ApplicationDbContext _db;
    private readonly IngresoService _ingresos;
    private readonly UserManager<ApplicationUser> _userManager;

    public IngresosController(ApplicationDbContext db, IngresoService ingresos, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _ingresos = ingresos;
        _userManager = userManager;
    }

    // ---------- Listado ----------

    public async Task<IActionResult> Index(IngresoListadoViewModel vm)
    {
        var query = _db.IngresosMercaderia.AsNoTracking().AsQueryable();

        query = vm.Estado switch
        {
            "vigentes" => query.Where(i => !i.Anulado),
            "anulados" => query.Where(i => i.Anulado),
            _ => query
        };

        // Las fechas del filtro son días locales; se convierten a UTC para comparar con la base
        if (vm.Desde.HasValue)
        {
            var desdeUtc = Fechas.AUtc(vm.Desde.Value.Date);
            query = query.Where(i => i.Fecha >= desdeUtc);
        }
        if (vm.Hasta.HasValue)
        {
            var hastaUtc = Fechas.AUtc(vm.Hasta.Value.Date.AddDays(1));
            query = query.Where(i => i.Fecha < hastaUtc);
        }

        var texto = vm.Q?.Trim();
        if (!string.IsNullOrEmpty(texto))
            query = query.Where(i => i.Proveedor.Contains(texto) ||
                                     (i.NumeroComprobante != null && i.NumeroComprobante.Contains(texto)));

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)TamanoPagina));
        vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);

        vm.Ingresos = await query
            .OrderByDescending(i => i.Fecha).ThenByDescending(i => i.IdIngreso)
            .Skip((vm.Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(i => new IngresoFilaViewModel
            {
                IdIngreso = i.IdIngreso,
                Fecha = i.Fecha,
                Proveedor = i.Proveedor,
                NumeroComprobante = i.NumeroComprobante,
                Usuario = i.Usuario.Nombre + " " + i.Usuario.Apellido,
                Unidades = i.Detalles.Sum(d => d.Cantidad),
                Total = i.Detalles.Sum(d => d.Cantidad * d.CostoUnitario),
                Anulado = i.Anulado
            })
            .ToListAsync();

        return View(vm);
    }

    // ---------- Registrar ----------

    // idProducto (opcional): llega desde el listado de productos para precargar la primera fila
    [HttpGet]
    public async Task<IActionResult> Create(int? idProducto)
    {
        var vm = new IngresoFormViewModel();
        await CargarProductosAsync(vm);

        var preseleccionado = idProducto.HasValue && vm.Productos.Any(p => p.Id == idProducto.Value) ? idProducto : null;
        vm.Detalles.Add(new DetalleIngresoFormViewModel { IdProducto = preseleccionado });

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IngresoFormViewModel vm)
    {
        await ValidarDetallesAsync(vm);

        if (ModelState.IsValid)
        {
            var resultado = await _ingresos.RegistrarAsync(_userManager.GetUserId(User)!, vm);
            if (resultado.Ok)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Detalle), new { id = resultado.IdIngreso });
            }
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }

        await CargarProductosAsync(vm);
        if (vm.Detalles.Count == 0)
            vm.Detalles.Add(new DetalleIngresoFormViewModel());
        return View(vm);
    }

    // ---------- Detalle ----------

    public async Task<IActionResult> Detalle(int id)
    {
        var vm = await _db.IngresosMercaderia.AsNoTracking()
            .Where(i => i.IdIngreso == id)
            .Select(i => new IngresoDetalleViewModel
            {
                IdIngreso = i.IdIngreso,
                Fecha = i.Fecha,
                Proveedor = i.Proveedor,
                NumeroComprobante = i.NumeroComprobante,
                Observaciones = i.Observaciones,
                Usuario = i.Usuario.Nombre + " " + i.Usuario.Apellido,
                Anulado = i.Anulado,
                FechaAnulacion = i.FechaAnulacion,
                Items = i.Detalles
                    .OrderBy(d => d.IdDetalleIngreso)
                    .Select(d => new IngresoDetalleItemViewModel
                    {
                        IdProducto = d.IdProducto,
                        Nombre = d.Producto.Nombre,
                        ImagenUrl = d.Producto.ImagenUrl,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return vm is null ? NotFound() : View(vm);
    }

    // ---------- Anular (AJAX) ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id)
    {
        var resultado = await _ingresos.AnularAsync(id);
        return Json(new { ok = resultado.Ok, mensaje = resultado.Mensaje });
    }

    // ---------- Auxiliares ----------

    // Reglas que no se pueden expresar con atributos: al menos una fila, sin productos repetidos, productos existentes
    private async Task ValidarDetallesAsync(IngresoFormViewModel vm)
    {
        if (vm.Detalles.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Agregá al menos un producto.");
            return;
        }

        if (vm.Detalles.Count > MaxFilas)
        {
            ModelState.AddModelError(string.Empty, $"Un ingreso puede tener hasta {MaxFilas} productos.");
            return;
        }

        var ids = vm.Detalles.Where(d => d.IdProducto.HasValue).Select(d => d.IdProducto!.Value).ToList();

        var repetido = ids.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
        {
            var nombre = await _db.Productos.Where(p => p.IdProducto == repetido.Key).Select(p => p.Nombre).FirstOrDefaultAsync();
            ModelState.AddModelError(string.Empty, $"\"{nombre}\" aparece en más de una fila. Unificá las cantidades en una sola.");
        }

        var existentes = await _db.Productos.CountAsync(p => ids.Contains(p.IdProducto));
        if (existentes != ids.Distinct().Count())
            ModelState.AddModelError(string.Empty, "Uno de los productos elegidos ya no existe.");
    }

    // Se listan también los productos dados de baja: se puede reponer stock antes de reactivarlos
    private async Task CargarProductosAsync(IngresoFormViewModel vm) =>
        vm.Productos = await _db.Productos.AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new ProductoOpcion { Id = p.IdProducto, Nombre = p.Nombre, Stock = p.Stock, Activo = p.Activo })
            .ToListAsync();
}

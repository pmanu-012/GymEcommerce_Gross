using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class ProductosController : Controller
{
    private const int TamanoPagina = 10;

    private readonly ApplicationDbContext _db;
    private readonly ImagenService _imagenes;

    public ProductosController(ApplicationDbContext db, ImagenService imagenes)
    {
        _db = db;
        _imagenes = imagenes;
    }

    // ---------- Listado ----------

    public async Task<IActionResult> Index(ProductoListadoViewModel vm)
    {
        var query = _db.Productos.AsNoTracking().AsQueryable();

        query = vm.Estado switch
        {
            "inactivos" => query.Where(p => !p.Activo),
            "todos" => query,
            _ => query.Where(p => p.Activo)
        };

        if (vm.CategoriaId.HasValue)
            query = query.Where(p => p.Subcategoria.IdCategoria == vm.CategoriaId.Value);

        var texto = vm.Q?.Trim();
        if (!string.IsNullOrEmpty(texto))
            query = query.Where(p => p.Nombre.Contains(texto));

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)TamanoPagina));
        vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);

        vm.Productos = await query
            .OrderBy(p => p.Nombre).ThenBy(p => p.IdProducto)
            .Skip((vm.Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new ProductoFilaViewModel
            {
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                ImagenUrl = p.ImagenUrl,
                Categoria = p.Subcategoria.Categoria.Nombre,
                Subcategoria = p.Subcategoria.Nombre,
                Precio = p.Precio,
                PorcentajeDescuento = p.PorcentajeDescuento,
                Stock = p.Stock,
                Activo = p.Activo
            })
            .ToListAsync();

        vm.Categorias = await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem { Text = c.Nombre, Value = c.IdCategoria.ToString() })
            .ToListAsync();

        return View(vm);
    }

    // ---------- Alta ----------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new ProductoFormViewModel();
        await CargarOpcionesAsync(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductoFormViewModel vm)
    {
        await ValidarSubcategoriaAsync(vm);
        if (!ModelState.IsValid)
        {
            await CargarOpcionesAsync(vm);
            return View("Form", vm);
        }

        string? rutaImagen = null;
        if (vm.Imagen is { Length: > 0 })
        {
            var resultado = await _imagenes.GuardarAsync(vm.Imagen);
            if (resultado.Error is not null)
            {
                ModelState.AddModelError(nameof(vm.Imagen), resultado.Error);
                await CargarOpcionesAsync(vm);
                return View("Form", vm);
            }
            rutaImagen = resultado.Ruta;
        }

        var producto = new Producto
        {
            Nombre = vm.Nombre.Trim(),
            Descripcion = Limpiar(vm.Descripcion),
            Precio = vm.Precio!.Value,
            PorcentajeDescuento = vm.PorcentajeDescuento > 0 ? vm.PorcentajeDescuento : null,
            Stock = 0, // el stock inicial se carga con un ingreso de mercadería,
            Uso = Limpiar(vm.Uso),
            Material = Limpiar(vm.Material),
            IdSubcategoria = vm.IdSubcategoria!.Value,
            ImagenUrl = rutaImagen,
            Activo = vm.Activo
        };

        foreach (var id in await DisciplinasValidasAsync(vm.DisciplinasSeleccionadas))
            producto.ProductoDisciplinas.Add(new ProductoDisciplina { IdDisciplina = id });

        foreach (var id in await GruposValidosAsync(vm.GruposSeleccionados))
            producto.ProductoGruposMusculares.Add(new ProductoGrupoMuscular { IdGrupoMuscular = id });

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        TempData["Exito"] = "Producto creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Edición ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.AsNoTracking()
            .Include(p => p.Subcategoria)
            .Include(p => p.ProductoDisciplinas)
            .Include(p => p.ProductoGruposMusculares)
            .FirstOrDefaultAsync(p => p.IdProducto == id);

        if (producto is null)
            return NotFound();

        var vm = new ProductoFormViewModel
        {
            IdProducto = producto.IdProducto,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            PorcentajeDescuento = producto.PorcentajeDescuento,
            Stock = producto.Stock,
            Uso = producto.Uso,
            Material = producto.Material,
            IdCategoria = producto.Subcategoria.IdCategoria,
            IdSubcategoria = producto.IdSubcategoria,
            Activo = producto.Activo,
            ImagenActual = producto.ImagenUrl,
            RowVersion = producto.RowVersion,
            DisciplinasSeleccionadas = producto.ProductoDisciplinas.Select(x => x.IdDisciplina).ToList(),
            GruposSeleccionados = producto.ProductoGruposMusculares.Select(x => x.IdGrupoMuscular).ToList()
        };

        await CargarOpcionesAsync(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductoFormViewModel vm)
    {
        if (id != vm.IdProducto)
            return BadRequest();

        var producto = await _db.Productos
            .Include(p => p.ProductoDisciplinas)
            .Include(p => p.ProductoGruposMusculares)
            .FirstOrDefaultAsync(p => p.IdProducto == id);

        if (producto is null)
            return NotFound();

        vm.Stock = producto.Stock; // para mostrarlo si hay que volver a dibujar el formulario

        await ValidarSubcategoriaAsync(vm);
        if (!ModelState.IsValid)
        {
            vm.ImagenActual = producto.ImagenUrl;
            await CargarOpcionesAsync(vm);
            return View("Form", vm);
        }

        // Imagen: reemplazar, quitar o dejar como está
        string? imagenAnterior = null;
        string? imagenNueva = null;

        if (vm.Imagen is { Length: > 0 })
        {
            var resultado = await _imagenes.GuardarAsync(vm.Imagen);
            if (resultado.Error is not null)
            {
                ModelState.AddModelError(nameof(vm.Imagen), resultado.Error);
                vm.ImagenActual = producto.ImagenUrl;
                await CargarOpcionesAsync(vm);
                return View("Form", vm);
            }
            imagenAnterior = producto.ImagenUrl;
            imagenNueva = resultado.Ruta;
            producto.ImagenUrl = imagenNueva;
        }
        else if (vm.QuitarImagen)
        {
            imagenAnterior = producto.ImagenUrl;
            producto.ImagenUrl = null;
        }

        producto.Nombre = vm.Nombre.Trim();
        producto.Descripcion = Limpiar(vm.Descripcion);
        producto.Precio = vm.Precio!.Value;
        producto.PorcentajeDescuento = vm.PorcentajeDescuento > 0 ? vm.PorcentajeDescuento : null;
        producto.Uso = Limpiar(vm.Uso);
        producto.Material = Limpiar(vm.Material);
        producto.IdSubcategoria = vm.IdSubcategoria!.Value;
        producto.Activo = vm.Activo;

        // Sincroniza las tablas intermedias: borra las desmarcadas y agrega las nuevas
        var disciplinasIds = await DisciplinasValidasAsync(vm.DisciplinasSeleccionadas);
        foreach (var pd in producto.ProductoDisciplinas.Where(x => !disciplinasIds.Contains(x.IdDisciplina)).ToList())
            _db.ProductoDisciplinas.Remove(pd);
        foreach (var idD in disciplinasIds.Except(producto.ProductoDisciplinas.Select(x => x.IdDisciplina)))
            producto.ProductoDisciplinas.Add(new ProductoDisciplina { IdDisciplina = idD });

        var gruposIds = await GruposValidosAsync(vm.GruposSeleccionados);
        foreach (var pg in producto.ProductoGruposMusculares.Where(x => !gruposIds.Contains(x.IdGrupoMuscular)).ToList())
            _db.ProductoGruposMusculares.Remove(pg);
        foreach (var idG in gruposIds.Except(producto.ProductoGruposMusculares.Select(x => x.IdGrupoMuscular)))
            producto.ProductoGruposMusculares.Add(new ProductoGrupoMuscular { IdGrupoMuscular = idG });

        // Concurrencia: si alguien modificó el producto después de abrir el formulario, falla el guardado
        if (vm.RowVersion is not null)
            _db.Entry(producto).Property(p => p.RowVersion).OriginalValue = vm.RowVersion;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _imagenes.Eliminar(imagenNueva); // la imagen recién subida quedó sin usar
            ModelState.AddModelError(string.Empty,
                "Otra persona modificó este producto mientras lo editabas. Volvé al listado y abrilo de nuevo para ver los datos actuales.");
            vm.ImagenActual = (await _db.Productos.AsNoTracking()
                .Where(p => p.IdProducto == id).Select(p => p.ImagenUrl).FirstOrDefaultAsync());
            await CargarOpcionesAsync(vm);
            return View("Form", vm);
        }

        _imagenes.Eliminar(imagenAnterior); // recién ahora se borra la imagen vieja

        TempData["Exito"] = "Producto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Baja lógica / reactivación (se llama por AJAX) ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound(new { ok = false, mensaje = "El producto no existe." });

        producto.Activo = !producto.Activo;
        await _db.SaveChangesAsync();

        return Json(new
        {
            ok = true,
            activo = producto.Activo,
            mensaje = producto.Activo ? "Producto reactivado." : "Producto dado de baja."
        });
    }

    // ---------- Subcategorías de una categoría (para el select dependiente) ----------

    [HttpGet]
    public async Task<IActionResult> Subcategorias(int idCategoria)
    {
        var lista = await _db.Subcategorias.AsNoTracking()
            .Where(s => s.IdCategoria == idCategoria)
            .OrderBy(s => s.Nombre)
            .Select(s => new { id = s.IdSubcategoria, nombre = s.Nombre })
            .ToListAsync();

        return Json(lista);
    }

    // ---------- Auxiliares ----------

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // Verifica que la subcategoría elegida pertenezca a la categoría elegida
    private async Task ValidarSubcategoriaAsync(ProductoFormViewModel vm)
    {
        if (!vm.IdCategoria.HasValue || !vm.IdSubcategoria.HasValue)
            return;

        var valida = await _db.Subcategorias.AnyAsync(s =>
            s.IdSubcategoria == vm.IdSubcategoria.Value && s.IdCategoria == vm.IdCategoria.Value);

        if (!valida)
            ModelState.AddModelError(nameof(vm.IdSubcategoria), "La subcategoría no pertenece a la categoría elegida.");
    }

    private async Task<List<int>> DisciplinasValidasAsync(List<int> ids) =>
        await _db.Disciplinas.Where(d => ids.Contains(d.IdDisciplina)).Select(d => d.IdDisciplina).ToListAsync();

    private async Task<List<int>> GruposValidosAsync(List<int> ids) =>
        await _db.GruposMusculares.Where(g => ids.Contains(g.IdGrupoMuscular)).Select(g => g.IdGrupoMuscular).ToListAsync();

    private async Task CargarOpcionesAsync(ProductoFormViewModel vm)
    {
        vm.Categorias = await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem { Text = c.Nombre, Value = c.IdCategoria.ToString() })
            .ToListAsync();

        vm.Subcategorias = vm.IdCategoria.HasValue
            ? await _db.Subcategorias.AsNoTracking()
                .Where(s => s.IdCategoria == vm.IdCategoria.Value)
                .OrderBy(s => s.Nombre)
                .Select(s => new SelectListItem { Text = s.Nombre, Value = s.IdSubcategoria.ToString() })
                .ToListAsync()
            : new List<SelectListItem>();

        vm.Disciplinas = await _db.Disciplinas.AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new SelectListItem { Text = d.Nombre, Value = d.IdDisciplina.ToString() })
            .ToListAsync();

        vm.Grupos = await _db.GruposMusculares.AsNoTracking()
            .OrderBy(g => g.Nombre)
            .Select(g => new SelectListItem { Text = g.Nombre, Value = g.IdGrupoMuscular.ToString() })
            .ToListAsync();
    }
}

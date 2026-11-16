using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

// ABM de clientes. El alta la hace cada cliente al registrarse; el administrador
// consulta, edita datos personales y da de baja o reactiva (baja lógica).
[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class ClientesController : Controller
{
    private const int TamanoPagina = 15;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClientesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Usuarios con el rol Cliente (los administradores no aparecen)
    private async Task<IQueryable<ApplicationUser>> ClientesAsync()
    {
        var idRol = await _db.Roles.Where(r => r.Name == Roles.Cliente).Select(r => r.Id).FirstAsync();
        return _db.Users.AsNoTracking()
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == idRol));
    }

    // ---------- Listado ----------

    public async Task<IActionResult> Index(ClienteListadoViewModel vm)
    {
        var query = await ClientesAsync();

        query = vm.Estado switch
        {
            "inactivos" => query.Where(u => !u.Activo),
            "todos" => query,
            _ => query.Where(u => u.Activo)
        };

        var texto = vm.Q?.Trim();
        if (!string.IsNullOrEmpty(texto))
            query = query.Where(u =>
                (u.Nombre + " " + u.Apellido).Contains(texto) ||
                (u.Email != null && u.Email.Contains(texto)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(texto)));

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)TamanoPagina));
        vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);

        vm.Clientes = await query
            .OrderBy(u => u.Apellido).ThenBy(u => u.Nombre)
            .Skip((vm.Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(u => new ClienteFilaViewModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Telefono = u.PhoneNumber,
                Activo = u.Activo,
                EmailConfirmado = u.EmailConfirmed,
                Pedidos = u.Pedidos.Count(p => p.Estado != EstadoPedido.Cancelado),
                TotalGastado = u.Pedidos.Where(p => p.Estado != EstadoPedido.Cancelado).Sum(p => (decimal?)p.Total) ?? 0
            })
            .ToListAsync();

        return View(vm);
    }

    // ---------- Detalle ----------

    public async Task<IActionResult> Detalle(string id)
    {
        var ahora = DateTimeOffset.UtcNow;
        var vm = await (await ClientesAsync())
            .Where(u => u.Id == id)
            .Select(u => new ClienteDetalleViewModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Telefono = u.PhoneNumber,
                Activo = u.Activo,
                EmailConfirmado = u.EmailConfirmed,
                Bloqueado = u.LockoutEnd != null && u.LockoutEnd > ahora,
                CantidadPedidos = u.Pedidos.Count(p => p.Estado != EstadoPedido.Cancelado),
                TotalGastado = u.Pedidos.Where(p => p.Estado != EstadoPedido.Cancelado).Sum(p => (decimal?)p.Total) ?? 0,
                CantidadResenas = u.ResenasProducto.Count + u.ResenasPlataforma.Count,
                Pedidos = u.Pedidos
                    .OrderByDescending(p => p.Fecha)
                    .Select(p => new PedidoFilaAdminViewModel
                    {
                        IdPedido = p.IdPedido,
                        Fecha = p.Fecha,
                        Estado = p.Estado,
                        Total = p.Total
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return vm is null ? NotFound() : View(vm);
    }

    // ---------- Edición ----------

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await ObtenerClienteAsync(id);
        if (user is null)
            return NotFound();

        return View(new ClienteEditViewModel
        {
            Id = user.Id,
            Email = user.Email,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Telefono = user.PhoneNumber
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClienteEditViewModel vm)
    {
        var user = await ObtenerClienteAsync(vm.Id);
        if (user is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            vm.Email = user.Email;
            return View(vm);
        }

        user.Nombre = vm.Nombre.Trim();
        user.Apellido = vm.Apellido.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(vm.Telefono) ? null : vm.Telefono.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            vm.Email = user.Email;
            return View(vm);
        }

        TempData["Exito"] = "Datos del cliente actualizados.";
        return RedirectToAction(nameof(Detalle), new { id = user.Id });
    }

    // ---------- Baja lógica / reactivación (AJAX) ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(string id)
    {
        var user = await ObtenerClienteAsync(id);
        if (user is null)
            return NotFound(new { ok = false, mensaje = "El cliente no existe." });

        user.Activo = !user.Activo;

        // Al dar de baja se cambia el "security stamp": la sesión que el cliente tenga abierta
        // deja de valer en la próxima verificación (ver SecurityStampValidatorOptions en Program.cs)
        var result = user.Activo
            ? await _userManager.UpdateAsync(user)
            : await _userManager.UpdateSecurityStampAsync(user);

        if (!result.Succeeded)
            return Json(new { ok = false, mensaje = "No se pudo actualizar el cliente. Intentá de nuevo." });

        return Json(new { ok = true, mensaje = user.Activo ? "Cliente reactivado." : "Cliente dado de baja." });
    }

    // Busca el usuario solo si tiene el rol Cliente (un administrador no se edita desde acá)
    private async Task<ApplicationUser?> ObtenerClienteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user is not null && await _userManager.IsInRoleAsync(user, Roles.Cliente) ? user : null;
    }
}

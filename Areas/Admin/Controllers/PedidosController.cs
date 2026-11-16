using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class PedidosController : Controller
{
    private const int TamanoPagina = 15;

    private readonly ApplicationDbContext _db;
    private readonly PedidoService _pedidos;
    private readonly NotificacionService _notificaciones;

    public PedidosController(ApplicationDbContext db, PedidoService pedidos, NotificacionService notificaciones)
    {
        _db = db;
        _pedidos = pedidos;
        _notificaciones = notificaciones;
    }

    public async Task<IActionResult> Index(PedidoListadoAdminViewModel vm)
    {
        var query = _db.Pedidos.AsNoTracking().AsQueryable();

        if (vm.Estado.HasValue)
            query = query.Where(p => p.Estado == vm.Estado.Value);

        var texto = vm.Q?.Trim().TrimStart('#');
        if (!string.IsNullOrEmpty(texto))
        {
            if (int.TryParse(texto, out var numero))
                query = query.Where(p => p.IdPedido == numero);
            else
                query = query.Where(p =>
                    (p.Usuario.Nombre + " " + p.Usuario.Apellido).Contains(texto) ||
                    (p.Usuario.Email != null && p.Usuario.Email.Contains(texto)));
        }

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)TamanoPagina));
        vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);

        vm.Pedidos = await query
            .OrderByDescending(p => p.Fecha).ThenByDescending(p => p.IdPedido)
            .Skip((vm.Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(p => new PedidoFilaAdminViewModel
            {
                IdPedido = p.IdPedido,
                Fecha = p.Fecha,
                Cliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                Email = p.Usuario.Email,
                Estado = p.Estado,
                Total = p.Total
            })
            .ToListAsync();

        return View(vm);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var vm = await _pedidos.ObtenerDetalleAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    // AJAX: cambia el estado respetando las transiciones permitidas
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoPedido estado)
    {
        var resultado = await _pedidos.CambiarEstadoAsync(id, estado);
        if (!resultado.Ok)
            return Json(new { ok = false, mensaje = resultado.Mensaje });
        
        var url = Url.Action("Detalle", "Pedidos", new { area = "", id }, Request.Scheme)!;
        var avisado = await _notificaciones.CambioEstadoAsync(id, url);
        var mensaje = avisado
            ? $"{resultado.Mensaje} Se avisó al cliente por email."
            : $"{resultado.Mensaje} No se pudo enviar el aviso por email.";
        return Json(new { ok = true, mensaje });
    }
}

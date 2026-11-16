using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Controllers;

// Pedidos del cliente: checkout, "Mis pedidos", detalle y cancelación.
[Authorize(Roles = Roles.Cliente)]
public class PedidosController : Controller
{
    private readonly PedidoService _pedidos;
    private readonly CarritoService _carrito;
    private readonly NotificacionService _notificaciones;
    private readonly UserManager<ApplicationUser> _userManager;

    public PedidosController(PedidoService pedidos, CarritoService carrito, NotificacionService notificaciones, UserManager<ApplicationUser> userManager)
    {
        _pedidos = pedidos;
        _carrito = carrito;
        _notificaciones = notificaciones;
        _userManager = userManager;
    }

    private string IdUsuario => _userManager.GetUserId(User)!;

    // ---------- Mis pedidos ----------

    public async Task<IActionResult> Index() => View(await _pedidos.ListarDelUsuarioAsync(IdUsuario));

    public async Task<IActionResult> Detalle(int id)
    {
        var vm = await _pedidos.ObtenerDetalleAsync(id, IdUsuario);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var resultado = await _pedidos.CancelarPorClienteAsync(id, IdUsuario);
        return Json(new { ok = resultado.Ok, mensaje = resultado.Ok ? "Pedido cancelado." : resultado.Mensaje });
    }

    // ---------- Checkout ----------

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var carrito = await _carrito.ObtenerAsync(IdUsuario);
        if (carrito.Items.Count == 0)
            return RedirectToAction("Index", "Carrito");

        if (!carrito.PuedeComprar)
        {
            TempData["Error"] = "Hay productos sin stock suficiente en tu carrito. Revisalos para continuar.";
            return RedirectToAction("Index", "Carrito");
        }

        var vm = await _pedidos.UltimaDireccionAsync(IdUsuario) ?? new CheckoutViewModel();
        vm.Carrito = carrito;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        if (!CheckoutViewModel.Provincias.Contains(vm.Provincia))
            ModelState.AddModelError(nameof(vm.Provincia), "Elegí una provincia de la lista.");

        if (!ModelState.IsValid)
        {
            vm.Carrito = await _carrito.ObtenerAsync(IdUsuario);
            return View(vm);
        }

        var resultado = await _pedidos.CrearAsync(IdUsuario, vm);
        if (!resultado.Ok)
        {
            TempData["Error"] = resultado.Mensaje;
            return RedirectToAction("Index", "Carrito");
        }

        var url = Url.Action(nameof(Detalle), "Pedidos", new { id = resultado.IdPedido }, Request.Scheme)!;
        await _notificaciones.PedidoRecibidoAsync(resultado.IdPedido!.Value, url);

        TempData["PedidoNuevo"] = true;
        return RedirectToAction(nameof(Detalle), new { id = resultado.IdPedido });
    }
}

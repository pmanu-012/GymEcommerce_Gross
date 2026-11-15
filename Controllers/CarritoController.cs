using GymEcommerce.Models;
using GymEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class CarritoController : Controller
{
    private readonly CarritoService _carrito;
    private readonly UserManager<ApplicationUser> _userManager;

    public CarritoController(CarritoService carrito, UserManager<ApplicationUser> userManager)
    {
        _carrito = carrito;
        _userManager = userManager;
    }

    private string IdUsuario => _userManager.GetUserId(User)!;

    public async Task<IActionResult> Index() => View(await _carrito.ObtenerAsync(IdUsuario));

    // AJAX: devuelve JSON con el resultado y la nueva cantidad para el contador
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int idProducto, int cantidad = 1)
    {
        var resultado = await _carrito.AgregarAsync(IdUsuario, idProducto, cantidad);
        var total = await _carrito.ContarAsync(IdUsuario);
        return Json(new { ok = resultado.Ok, mensaje = resultado.Mensaje, cantidad = total });
    }

    // AJAX: devuelve el HTML actualizado del carrito (o 409 con el motivo)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarCantidad(int idCarritoItem, int cantidad)
    {
        var resultado = await _carrito.ActualizarCantidadAsync(IdUsuario, idCarritoItem, cantidad);
        if (!resultado.Ok)
            return StatusCode(StatusCodes.Status409Conflict, new { ok = false, mensaje = resultado.Mensaje });

        return await ContenidoActualizadoAsync(resultado.Mensaje);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int idCarritoItem)
    {
        await _carrito.QuitarAsync(IdUsuario, idCarritoItem);
        return await ContenidoActualizadoAsync(null);
    }

    private async Task<IActionResult> ContenidoActualizadoAsync(string? aviso)
    {
        var vm = await _carrito.ObtenerAsync(IdUsuario);

        // Datos extra para el JavaScript: contador de la barra y aviso opcional
        Response.Headers["X-Carrito-Cantidad"] = vm.CantidadTotal.ToString();
        if (aviso is not null)
            Response.Headers["X-Carrito-Aviso"] = Uri.EscapeDataString(aviso);

        return PartialView("_CarritoContenido", vm);
    }
}

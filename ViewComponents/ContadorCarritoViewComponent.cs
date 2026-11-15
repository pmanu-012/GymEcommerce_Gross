using GymEcommerce.Models;
using GymEcommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.ViewComponents;

// Ícono del carrito con la cantidad de productos, en la barra de navegación.
public class ContadorCarritoViewComponent : ViewComponent
{
    private readonly CarritoService _carrito;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContadorCarritoViewComponent(CarritoService carrito, UserManager<ApplicationUser> userManager)
    {
        _carrito = carrito;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // El administrador no compra: no se le muestra el carrito
        if (UserClaimsPrincipal.IsInRole(Roles.Administrador))
            return Content(string.Empty);

        var cantidad = 0;
        var idUsuario = _userManager.GetUserId(UserClaimsPrincipal);
        if (idUsuario is not null)
            cantidad = await _carrito.ContarAsync(idUsuario);

        return View(cantidad);
    }
}

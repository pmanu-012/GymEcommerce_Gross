using GymEcommerce.Models;
using GymEcommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.ViewComponents;

// Sección de reseñas. Con idProducto: las del producto. Sin idProducto: las de la tienda.
// Se usa dentro de las páginas y también se pide sola por AJAX (ResenasController.Seccion) para refrescarla.
public class ResenasViewComponent : ViewComponent
{
    private readonly ResenaService _resenas;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResenasViewComponent(ResenaService resenas, UserManager<ApplicationUser> userManager)
    {
        _resenas = resenas;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? idProducto, int pagina = 1)
    {
        var idUsuario = _userManager.GetUserId(UserClaimsPrincipal);
        var esCliente = UserClaimsPrincipal.IsInRole(Roles.Cliente);
        return View(await _resenas.ObtenerSeccionAsync(idProducto, pagina, idUsuario, esCliente));
    }
}

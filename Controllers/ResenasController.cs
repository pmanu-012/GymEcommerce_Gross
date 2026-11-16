using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Controllers;

public class ResenasController : Controller
{
    private readonly ResenaService _resenas;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResenasController(ResenaService resenas, UserManager<ApplicationUser> userManager)
    {
        _resenas = resenas;
        _userManager = userManager;
    }

    // Página pública de opiniones sobre la tienda
    public IActionResult Index(int pagina = 1)
    {
        ViewData["Pagina"] = pagina;
        return View();
    }

    // Solo el HTML de la sección (para paginar y refrescar por AJAX)
    public IActionResult Seccion(int? idProducto, int pagina = 1) =>
        ViewComponent("Resenas", new { idProducto, pagina });

    [HttpPost, Authorize(Roles = Roles.Cliente), ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ResenaFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var error = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return Json(new { ok = false, mensaje = error });
        }

        var idUsuario = _userManager.GetUserId(User)!;
        var resultado = vm.IdProducto.HasValue
            ? await _resenas.GuardarDeProductoAsync(idUsuario, vm)
            : await _resenas.GuardarDePlataformaAsync(idUsuario, vm);

        return Json(new { ok = resultado.Ok, mensaje = resultado.Mensaje });
    }

    [HttpPost, Authorize(Roles = Roles.Cliente), ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int idResena, string tipo)
    {
        var resultado = await _resenas.EliminarAsync(tipo, idResena, _userManager.GetUserId(User)!);
        return Json(new { ok = resultado.Ok, mensaje = resultado.Mensaje });
    }
}

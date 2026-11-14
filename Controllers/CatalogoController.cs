using GymEcommerce.Services;
using GymEcommerce.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Controllers;

// Público: no requiere iniciar sesión.
public class CatalogoController : Controller
{
    private readonly CatalogoService _catalogo;

    public CatalogoController(CatalogoService catalogo) => _catalogo = catalogo;

    public async Task<IActionResult> Index([FromQuery] CatalogoFiltro filtro)
    {
        var vm = await _catalogo.ObtenerListadoAsync(filtro, nameof(Index));
        return View(vm);
    }

    public async Task<IActionResult> Ofertas([FromQuery] CatalogoFiltro filtro)
    {
        filtro.SoloOfertas = true;
        var vm = await _catalogo.ObtenerListadoAsync(filtro, nameof(Ofertas));
        return View("Index", vm); // misma vista, con el filtro de ofertas activado
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var vm = await _catalogo.ObtenerDetalleAsync(id);
        return vm is null ? NotFound() : View(vm);
    }
}

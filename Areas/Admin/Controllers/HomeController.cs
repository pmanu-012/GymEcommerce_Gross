using GymEcommerce.Models;
using GymEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Areas.Admin.Controllers;

// Página de inicio del panel: indicadores y estadísticas
[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class HomeController : Controller
{
    private readonly EstadisticasService _estadisticas;

    public HomeController(EstadisticasService estadisticas) => _estadisticas = estadisticas;

    public async Task<IActionResult> Index(int dias = 30) => View(await _estadisticas.ObtenerAsync(dias));
}

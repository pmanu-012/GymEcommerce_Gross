using GymEcommerce.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using GymEcommerce.Services;

namespace GymEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CatalogoService _catalogo;
        public HomeController(ILogger<HomeController> logger, CatalogoService catalogo)
        {
            _logger = logger;
            _catalogo = catalogo;
        }
        public async Task<IActionResult> Index()
        {
             return View(await _catalogo.ObtenerPortadaAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

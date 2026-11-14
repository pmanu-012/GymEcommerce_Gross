using GymEcommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class HomeController : Controller
{
    // Por ahora el panel arranca en Productos. Cuando hagamos las estadísticas, esta será su página.
    public IActionResult Index() => RedirectToAction("Index", "Productos");
}

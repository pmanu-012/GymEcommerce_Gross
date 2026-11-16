using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Controllers;

// Páginas de error propias. Program.cs redirige acá internamente:
// - UseStatusCodePagesWithReExecute: respuestas 404, 400, etc. que no tienen contenido
// - UseExceptionHandler (solo en producción): errores no controlados (500)
[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error/{codigo:int}")]
    public IActionResult Index(int codigo)
    {
        Response.StatusCode = codigo;

        if (codigo == 404)
            return View("NoEncontrado");

        ViewData["Codigo"] = codigo;
        return View("ErrorGeneral");
    }
}

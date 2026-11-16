using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Areas.Admin.Controllers;

// Página para verificar que el servidor puede enviar emails (sobre todo después de publicar el sitio).
// Se accede desde /Admin/EmailPrueba
[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class EmailPruebaController : Controller
{
    private readonly IEmailService _email;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmailPruebaController(IEmailService email, UserManager<ApplicationUser> userManager)
    {
        _email = email;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var admin = await _userManager.GetUserAsync(User);
        return View(new EmailPruebaViewModel { Destinatario = admin?.Email ?? string.Empty, Configurado = _email.Configurado });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(EmailPruebaViewModel vm)
    {
        vm.Configurado = _email.Configurado;
        if (!ModelState.IsValid)
            return View(vm);

        var resultado = await _email.EnviarAsync(vm.Destinatario, PlantillasEmail.Prueba());

        vm.Ok = resultado.Ok;
        vm.Resultado = resultado switch
        {
            { Ok: true, ArchivoLocal: not null } => $"El SMTP no está configurado: el email se guardó en {resultado.ArchivoLocal}",
            { Ok: true } => "Email enviado. Revisá la bandeja de entrada (y la carpeta de spam).",
            _ => $"No se pudo enviar: {resultado.Error}"
        };

        return View(vm);
    }
}

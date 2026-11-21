using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.Controllers;

// "Mi perfil": datos personales y cambio de contraseña (con la sesión iniciada).
[Authorize]
public class PerfilController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _email;

    public PerfilController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService email)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        return View(new PerfilPaginaViewModel
        {
            Email = user.Email ?? string.Empty,
            Datos = new PerfilViewModel { Nombre = user.Nombre, Apellido = user.Apellido, Telefono = user.PhoneNumber }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Datos([Bind(Prefix = "Datos")] PerfilViewModel datos)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();
        //si el usuario no está logueado, se redirige a la página de login (desafio de autenticación)

        if (!ModelState.IsValid)
            return View("Index", new PerfilPaginaViewModel { Email = user.Email ?? string.Empty, Datos = datos });

        user.Nombre = datos.Nombre.Trim(); //trim () elimina espacios en blanco al inicio y al final de la cadena
        user.Apellido = datos.Apellido.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim();

        var result = await _userManager.UpdateAsync(user);
        TempData[result.Succeeded ? "Exito" : "Error"] = result.Succeeded
            ? "Tus datos se actualizaron."
            : "No se pudieron guardar los cambios. Intentá de nuevo.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena([Bind(Prefix = "Contrasena")] CambiarContrasenaViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        if (ModelState.IsValid)
        {
            var result = await _userManager.ChangePasswordAsync(user, vm.PasswordActual, vm.PasswordNueva);
            if (result.Succeeded)
            {
                // Cambiar la contraseña invalida la cookie actual: se vuelve a emitir para no cerrar la sesión
                await _signInManager.RefreshSignInAsync(user);
                await _email.EnviarAsync(user.Email!, PlantillasEmail.ContrasenaCambiada(user.Nombre));

                TempData["Exito"] = "Tu contraseña se cambió.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                var campo = error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                    ? "Contrasena.PasswordActual"
                    : "Contrasena.PasswordNueva";
                var texto = error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                    ? "La contraseña actual es incorrecta."
                    : error.Description;
                ModelState.AddModelError(campo, texto);
            }
        }

        // Al volver a mostrar la página, las contraseñas no se reescriben en los campos
        return View("Index", new PerfilPaginaViewModel
        {
            Email = user.Email ?? string.Empty,
            Datos = new PerfilViewModel { Nombre = user.Nombre, Apellido = user.Apellido, Telefono = user.PhoneNumber }
        });
    }
}

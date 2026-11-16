using System.Text;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace GymEcommerce.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _email;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService email)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _email = email;
    }

    // ---------- Registro (siempre crea un Cliente) ----------

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            Nombre = model.Nombre.Trim(),
            Apellido = model.Apellido.Trim(),
            PhoneNumber = model.Telefono,
            Activo = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors.DistinctBy(e => e.Description))
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Cliente);

        // Ya no se inicia sesión: primero hay que confirmar el email
        var envio = await EnviarConfirmacionAsync(user);

        return View("Mensaje", new MensajeViewModel(
            Titulo: "Revisá tu correo",
            Texto: envio.Ok
                ? $"Te enviamos un email a {user.Email} con un enlace para confirmar tu cuenta. Si no lo ves, revisá la carpeta de spam."
                : "Tu cuenta se creó, pero no pudimos enviarte el email de confirmación. Pedí que te lo reenviemos.",
            TextoBoton: "Reenviar el email",
            UrlBoton: Url.Action(nameof(ReenviarConfirmacion), new { email = user.Email })));
    }

    // ---------- Confirmación de email ----------

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ConfirmarEmail(string? userId, string? token)
    {
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
        var tokenDecodificado = Decodificar(token);

        var confirmado = user is not null && (user.EmailConfirmed ||
            (tokenDecodificado is not null && (await _userManager.ConfirmEmailAsync(user, tokenDecodificado)).Succeeded));

        return View("Mensaje", confirmado
            ? new MensajeViewModel(
                Titulo: "¡Cuenta confirmada!",
                Texto: "Tu email quedó confirmado. Ya podés iniciar sesión y empezar a comprar.",
                Icono: "bi-patch-check",
                TextoBoton: "Iniciar sesión",
                UrlBoton: Url.Action(nameof(Login)))
            : new MensajeViewModel(
                Titulo: "El enlace no es válido",
                Texto: "El enlace venció o ya no es válido. Pedí uno nuevo y usalo dentro de las 24 horas.",
                Icono: "bi-exclamation-triangle",
                TextoBoton: "Pedir un enlace nuevo",
                UrlBoton: Url.Action(nameof(ReenviarConfirmacion))));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ReenviarConfirmacion(string? email) => View(new EmailViewModel { Email = email ?? string.Empty });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReenviarConfirmacion(EmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && !user.EmailConfirmed && user.Activo)
            await EnviarConfirmacionAsync(user);

        // Misma respuesta siempre: no se revela si el email está registrado
        return View("Mensaje", new MensajeViewModel(
            Titulo: "Revisá tu correo",
            Texto: "Si hay una cuenta sin confirmar con ese email, te enviamos un enlace nuevo. Revisá también la carpeta de spam."));
    }

    // ---------- Login ----------

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            if (!user.EmailConfirmed)
            {
                // Solo se avisa si la contraseña es correcta: así no se revela qué emails están registrados
                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    ViewData["EmailSinConfirmar"] = user.Email;
                    ModelState.AddModelError(string.Empty, "Todavía no confirmaste tu email. Revisá tu correo (también la carpeta de spam).");
                    return View(model);
                }
            }
            else
            {
                // Verifica la contraseña sin iniciar sesión todavía (cuenta intentos fallidos para el bloqueo)
                var check = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

                if (check.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Demasiados intentos fallidos. Probá de nuevo en unos minutos o restablecé tu contraseña.");
                    return View(model);
                }

                if (check.Succeeded)
                {
                    if (!user.Activo)
                    {
                        ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Contactá a un administrador.");
                        return View(model);
                    }

                    await _signInManager.SignInAsync(user, model.RememberMe);

                    if (string.IsNullOrEmpty(returnUrl) && await _userManager.IsInRoleAsync(user, Roles.Administrador))
                        return RedirectToAction("Index", "Home", new { area = "Admin" });

                    return RedirectToLocal(returnUrl);
                }
            }
        }

        // Mismo mensaje si el email no existe o la contraseña es incorrecta
        ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
        return View(model);
    }

    // ---------- Olvidé mi contraseña ----------

    [HttpGet, AllowAnonymous]
    public IActionResult OlvideContrasena() => View(new EmailViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> OlvideContrasena(EmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && user.EmailConfirmed && user.Activo)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action(nameof(RestablecerContrasena), "Account",
                new { userId = user.Id, token = Codificar(token) }, Request.Scheme)!;
            await _email.EnviarAsync(user.Email!, PlantillasEmail.RestablecerContrasena(user.Nombre, url));
        }

        // Misma respuesta siempre: no se revela si el email está registrado
        return View("Mensaje", new MensajeViewModel(
            Titulo: "Revisá tu correo",
            Texto: "Si existe una cuenta con ese email, te enviamos un enlace para crear una contraseña nueva. El enlace vence en 24 horas. Revisá también la carpeta de spam.",
            TextoBoton: "Volver a iniciar sesión",
            UrlBoton: Url.Action(nameof(Login))));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult RestablecerContrasena(string? userId, string? token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return View("Mensaje", EnlaceRestablecerInvalido());

        return View(new RestablecerContrasenaViewModel { UserId = userId, Token = token });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        var token = Decodificar(model.Token);
        if (user is null || token is null)
            return View("Mensaje", EnlaceRestablecerInvalido());

        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                return View("Mensaje", EnlaceRestablecerInvalido());

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // Si estaba bloqueada por intentos fallidos, se desbloquea
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        await _email.EnviarAsync(user.Email!, PlantillasEmail.ContrasenaCambiada(user.Nombre));

        TempData["Exito"] = "Tu contraseña se actualizó. Ya podés iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Logout (POST para que no se pueda disparar con un link) ----------

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    // ---------- Auxiliares ----------

    private async Task<ResultadoEmail> EnviarConfirmacionAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = Url.Action(nameof(ConfirmarEmail), "Account",
            new { userId = user.Id, token = Codificar(token) }, Request.Scheme)!;
        return await _email.EnviarAsync(user.Email!, PlantillasEmail.ConfirmarCuenta(user.Nombre, url));
    }

    private MensajeViewModel EnlaceRestablecerInvalido() => new(
        Titulo: "El enlace no es válido",
        Texto: "El enlace venció o ya se usó. Pedí uno nuevo para restablecer tu contraseña.",
        Icono: "bi-exclamation-triangle",
        TextoBoton: "Pedir un enlace nuevo",
        UrlBoton: Url.Action(nameof(OlvideContrasena)));

    // Los tokens de Identity tienen caracteres (+, /, =) que se rompen en una URL: se codifican en Base64Url
    private static string Codificar(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? Decodificar(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
}

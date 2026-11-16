using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels;

// Misma regla que las opciones de Identity en Program.cs. Si cambiás una, cambiá la otra.
public static class ReglasPassword
{
    public const string Patron = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$";
    public const string Mensaje = "Mínimo 8 caracteres, con al menos una mayúscula, una minúscula y un número.";
}

// Pantallas de aviso (registro pendiente, enlace enviado, email confirmado...)
public record MensajeViewModel(
    string Titulo,
    string Texto,
    string Icono = "bi-envelope-check",
    string? TextoBoton = null,
    string? UrlBoton = null,
    string? TextoSecundario = null,
    string? UrlSecundario = null);

// Formularios que solo piden un email (reenviar confirmación, olvidé mi contraseña)
public class EmailViewModel
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class RestablecerContrasenaViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [RegularExpression(ReglasPassword.Patron, ErrorMessage = ReglasPassword.Mensaje)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmá la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ---------- Perfil ----------

public class PerfilViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
    [StringLength(30, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Teléfono (opcional)")]
    public string? Telefono { get; set; }
}

public class CambiarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
    [RegularExpression(ReglasPassword.Patron, ErrorMessage = ReglasPassword.Mensaje)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string PasswordNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmá la nueva contraseña.")]
    [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

// La página de perfil tiene dos formularios; cada uno se envía por separado (prefijos "Datos." y "Contrasena.")
public class PerfilPaginaViewModel
{
    public string Email { get; set; } = string.Empty;
    public PerfilViewModel Datos { get; set; } = new();
    public CambiarContrasenaViewModel Contrasena { get; set; } = new();
}

using Microsoft.AspNetCore.Identity;

namespace GymEcommerce.Services;

// Traduce los mensajes de error de Identity (por defecto vienen en inglés).
public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError Err(string code, string description) =>
        new() { Code = code, Description = description };

    public override IdentityError DefaultError() =>
        Err(nameof(DefaultError), "Ocurrió un error inesperado.");

    // Como UserName = Email, Identity devuelve ambos errores; el controlador elimina el mensaje repetido.
    public override IdentityError DuplicateEmail(string email) =>
        Err(nameof(DuplicateEmail), "Ya existe una cuenta con ese email.");

    public override IdentityError DuplicateUserName(string userName) =>
        Err(nameof(DuplicateUserName), "Ya existe una cuenta con ese email.");

    public override IdentityError InvalidEmail(string? email) =>
        Err(nameof(InvalidEmail), "El email no es válido.");

    public override IdentityError InvalidUserName(string? userName) =>
        Err(nameof(InvalidUserName), "El email no es válido.");

    public override IdentityError PasswordTooShort(int length) =>
        Err(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");

    public override IdentityError PasswordRequiresDigit() =>
        Err(nameof(PasswordRequiresDigit), "La contraseña debe tener al menos un número.");

    public override IdentityError PasswordRequiresLower() =>
        Err(nameof(PasswordRequiresLower), "La contraseña debe tener al menos una minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Err(nameof(PasswordRequiresUpper), "La contraseña debe tener al menos una mayúscula.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Err(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe tener al menos un símbolo.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Err(nameof(PasswordRequiresUniqueChars), $"La contraseña debe tener al menos {uniqueChars} caracteres distintos.");

    public override IdentityError PasswordMismatch() =>
        Err(nameof(PasswordMismatch), "La contraseña es incorrecta.");
}

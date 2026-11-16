using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels.Admin;

public class EmailPruebaViewModel
{
    [Required(ErrorMessage = "Ingresá un destinatario.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [Display(Name = "Enviar a")]
    public string Destinatario { get; set; } = string.Empty;

    public bool Configurado { get; set; }
    public bool? Ok { get; set; }
    public string? Resultado { get; set; }
}

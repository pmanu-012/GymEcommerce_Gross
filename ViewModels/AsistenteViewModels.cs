using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels;

// Un mensaje previo de la conversación (lo guarda el navegador y lo reenvía en cada consulta)
public class MensajeAsistenteDto
{
    public string Rol { get; set; } = "usuario";   // usuario | asistente
    public string Texto { get; set; } = string.Empty;
}

public class PreguntaAsistenteDto
{
    [Required(ErrorMessage = "Escribí una consulta.")]
    [StringLength(500, ErrorMessage = "La consulta puede tener hasta {1} caracteres.")]
    public string Mensaje { get; set; } = string.Empty;

    public List<MensajeAsistenteDto> Historial { get; set; } = new();
}

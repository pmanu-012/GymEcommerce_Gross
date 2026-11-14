using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class ResenaPlataforma
{
    [Key]
    public int IdResena { get; set; }

    [Range(1, 5)]
    public int Calificacion { get; set; }

    [StringLength(1000)]
    public string? Comentario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string IdUsuario { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;
}

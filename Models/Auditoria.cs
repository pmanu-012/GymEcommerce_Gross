using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class Auditoria
{
    [Key]
    public int IdAuditoria { get; set; }

    // Nullable: hay operaciones sin usuario autenticado (seed, registro de un cliente nuevo).
    public string? IdUsuario { get; set; }
    public ApplicationUser? Usuario { get; set; }

    [Required, StringLength(20)]
    public string Accion { get; set; } = string.Empty;   // Crear / Editar / Eliminar

    [Required, StringLength(100)]
    public string Entidad { get; set; } = string.Empty;  // Nombre de la clase afectada

    [StringLength(50)]
    public string? IdEntidad { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string? ValorAnterior { get; set; }  // JSON
    public string? ValorNuevo { get; set; }     // JSON
}

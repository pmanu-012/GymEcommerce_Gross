using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

// Cabecera de una entrada de mercadería: quién la registró, cuándo y de qué proveedor.
public class IngresoMercaderia
{
    [Key]
    public int IdIngreso { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required, StringLength(150)]
    public string Proveedor { get; set; } = string.Empty;

    [StringLength(50)]
    public string? NumeroComprobante { get; set; }

    [StringLength(300)]
    public string? Observaciones { get; set; }

    // Un ingreso no se borra: se anula, y el stock que había sumado se descuenta.
    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public string IdUsuario { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    public ICollection<DetalleIngreso> Detalles { get; set; } = new List<DetalleIngreso>();
}

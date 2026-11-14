using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class Pedido
{
    [Key]
    public int IdPedido { get; set; }

    // Se guarda en UTC; se convierte a hora local al mostrarla.
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    // Foto del momento de la compra (suma de detalles + envío)
    public decimal Total { get; set; }

    // Dirección de envío (también foto del momento de la compra)
    [Required, StringLength(100)]
    public string Calle { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Numero { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Provincia { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string CodigoPostal { get; set; } = string.Empty;

    public decimal CostoEnvio { get; set; }

    public string IdUsuario { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
}

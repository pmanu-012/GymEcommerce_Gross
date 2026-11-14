using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class DetallePedido
{
    [Key]
    public int IdDetalle { get; set; }

    public int Cantidad { get; set; }

    // Precio final del producto al momento de la compra (ya con descuento aplicado)
    public decimal PrecioUnitario { get; set; }

    public int IdPedido { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;
}

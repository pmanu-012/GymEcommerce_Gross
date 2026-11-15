using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

// Un renglón del ingreso: qué producto entró, cuántas unidades y a qué costo.
public class DetalleIngreso
{
    [Key]
    public int IdDetalleIngreso { get; set; }

    public int Cantidad { get; set; }

    // Costo de compra por unidad (no el precio de venta)
    public decimal CostoUnitario { get; set; }

    public int IdIngreso { get; set; }
    public IngresoMercaderia Ingreso { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;
}

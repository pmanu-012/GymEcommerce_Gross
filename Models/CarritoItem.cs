using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class CarritoItem
{
    [Key]
    public int IdCarritoItem { get; set; }

    public int Cantidad { get; set; }

    public string IdUsuario { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;
}

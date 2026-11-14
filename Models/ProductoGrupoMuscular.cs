using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class ProductoGrupoMuscular
{
    [Key]
    public int IdProductoGrupoMuscular { get; set; }

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int IdGrupoMuscular { get; set; }
    public GrupoMuscular GrupoMuscular { get; set; } = null!;
}

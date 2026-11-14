using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class Subcategoria
{
    [Key]
    public int IdSubcategoria { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public int IdCategoria { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class GrupoMuscular
{
    [Key]
    public int IdGrupoMuscular { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public ICollection<ProductoGrupoMuscular> ProductoGruposMusculares { get; set; } = new List<ProductoGrupoMuscular>();
}

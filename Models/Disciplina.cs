using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class Disciplina
{
    [Key]
    public int IdDisciplina { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public ICollection<ProductoDisciplina> ProductoDisciplinas { get; set; } = new List<ProductoDisciplina>();
}

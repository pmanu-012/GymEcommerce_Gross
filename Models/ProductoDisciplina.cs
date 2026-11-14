using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.Models;

public class ProductoDisciplina
{
    [Key]
    public int IdProductoDisciplina { get; set; }

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int IdDisciplina { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
}

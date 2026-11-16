using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels;

// Promedio y cantidad de reseñas por estrella (Distribucion[0] = 1 estrella ... Distribucion[4] = 5 estrellas)
public class ResumenCalificacion
{
    public decimal Promedio { get; set; }
    public int Cantidad { get; set; }
    public int[] Distribucion { get; set; } = new int[5];
}

public class ResenaItemViewModel
{
    public int IdResena { get; set; }
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    // "Manuel P.": no se muestra el apellido completo
    public string Autor => string.IsNullOrWhiteSpace(Apellido) ? Nombre : $"{Nombre} {Apellido[0]}.";
}

public class ResenaFormViewModel
{
    // Con IdProducto: reseña del producto. Sin IdProducto: opinión sobre la tienda.
    public int? IdProducto { get; set; }

    [Required(ErrorMessage = "Elegí una calificación.")]
    [Range(1, 5, ErrorMessage = "Elegí entre 1 y 5 estrellas.")]
    [Display(Name = "Tu calificación")]
    public int? Calificacion { get; set; }

    [StringLength(1000, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Comentario (opcional)")]
    public string? Comentario { get; set; }

    // Solo para mostrar (no viene del formulario)
    public bool EsEdicion { get; set; }
}

// Sección completa de reseñas (la usan el detalle del producto y la página de opiniones)
public class SeccionResenasViewModel
{
    public int? IdProducto { get; set; }
    public ResumenCalificacion Resumen { get; set; } = new();
    public List<ResenaItemViewModel> Resenas { get; set; } = new();
    public ResenaItemViewModel? MiResena { get; set; }
    public ResenaFormViewModel Form { get; set; } = new();

    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;

    public bool Autenticado { get; set; }
    public bool EsCliente { get; set; }
    public bool PuedeResenar { get; set; }
}

public class UltimasOpinionesViewModel
{
    public ResumenCalificacion Resumen { get; set; } = new();
    public List<ResenaItemViewModel> Items { get; set; } = new();
}

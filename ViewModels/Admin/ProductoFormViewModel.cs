using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymEcommerce.ViewModels.Admin;

public class ProductoFormViewModel
{
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 100000000, ErrorMessage = "Ingresá un precio mayor a 0.")]
    [Display(Name = "Precio")]
    public decimal? Precio { get; set; }

    [Range(0, 100, ErrorMessage = "El descuento debe estar entre 0 y 100.")]
    [Display(Name = "Descuento (%)")]
    public decimal? PorcentajeDescuento { get; set; }

    // Solo se muestra: el stock se modifica con ingresos de mercadería y con ventas
    public int? Stock { get; set; }

    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Uso")]
    public string? Uso { get; set; }

    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Material")]
    public string? Material { get; set; }

    // IdCategoria no se guarda: solo sirve para filtrar la lista de subcategorías.
    [Required(ErrorMessage = "Elegí una categoría.")]
    [Display(Name = "Categoría")]
    public int? IdCategoria { get; set; }

    [Required(ErrorMessage = "Elegí una subcategoría.")]
    [Display(Name = "Subcategoría")]
    public int? IdSubcategoria { get; set; }

    [Display(Name = "Producto activo (visible en la tienda)")]
    public bool Activo { get; set; } = true;

    // Imagen
    public string? ImagenActual { get; set; }

    [Display(Name = "Imagen")]
    public IFormFile? Imagen { get; set; }

    [Display(Name = "Quitar la imagen actual")]
    public bool QuitarImagen { get; set; }

    // Relaciones N:M (los checkboxes marcados)
    public List<int> DisciplinasSeleccionadas { get; set; } = new();
    public List<int> GruposSeleccionados { get; set; } = new();

    // Control de concurrencia
    public byte[]? RowVersion { get; set; }

    // Opciones para los select y checkboxes (las carga el controlador, no vienen del formulario)
    public List<SelectListItem> Categorias { get; set; } = new();
    public List<SelectListItem> Subcategorias { get; set; } = new();
    public List<SelectListItem> Disciplinas { get; set; } = new();
    public List<SelectListItem> Grupos { get; set; } = new();
}

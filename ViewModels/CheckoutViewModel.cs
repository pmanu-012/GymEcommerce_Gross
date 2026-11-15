using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels;

public class CheckoutViewModel
{
    public static readonly string[] Provincias =
    {
        "Buenos Aires", "Catamarca", "Chaco", "Chubut", "Ciudad Autónoma de Buenos Aires", "Córdoba",
        "Corrientes", "Entre Ríos", "Formosa", "Jujuy", "La Pampa", "La Rioja", "Mendoza", "Misiones",
        "Neuquén", "Río Negro", "Salta", "San Juan", "San Luis", "Santa Cruz", "Santa Fe",
        "Santiago del Estero", "Tierra del Fuego", "Tucumán"
    };

    // Lo carga el controlador para mostrar el resumen (no viene del formulario)
    public CarritoViewModel Carrito { get; set; } = new();

    [Required(ErrorMessage = "La calle es obligatoria.")]
    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Calle")]
    public string Calle { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número es obligatorio.")]
    [StringLength(10, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Número")]
    public string Numero { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Ciudad")]
    public string Ciudad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí una provincia.")]
    [Display(Name = "Provincia")]
    public string Provincia { get; set; } = string.Empty;

    // Acepta el formato viejo (6300) y el CPA (C1425ABC)
    [Required(ErrorMessage = "El código postal es obligatorio.")]
    [RegularExpression(@"^(\d{4}|[A-Za-z]\d{4}[A-Za-z]{3})$", ErrorMessage = "Ingresá un código postal válido (ej.: 6300 o C1425ABC).")]
    [Display(Name = "Código postal")]
    public string CodigoPostal { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymEcommerce.ViewModels.Admin;

// Una fila de la tabla (sirve para categorías, subcategorías, disciplinas y grupos musculares).
public class CatalogoFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Detalle { get; set; }        // por ejemplo, la categoría de una subcategoría
    public int CantidadAsociados { get; set; }  // subcategorías o productos que lo usan
}

public class CatalogoListadoViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string TextoNuevo { get; set; } = string.Empty;
    public string TextoAsociados { get; set; } = string.Empty;
    public string? ColumnaDetalle { get; set; }
    public List<CatalogoFilaViewModel> Filas { get; set; } = new();
}

// Formulario que se muestra dentro del modal.
public class CatalogoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    // Solo para subcategorías
    [Display(Name = "Categoría")]
    public int? IdCategoria { get; set; }

    // Los completa el controlador base (no vienen del formulario)
    public bool PideCategoria { get; set; }
    public string TituloModal { get; set; } = string.Empty;
    public List<SelectListItem> Categorias { get; set; } = new();
}

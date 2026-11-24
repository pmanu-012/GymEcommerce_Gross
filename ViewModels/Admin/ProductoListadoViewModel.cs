using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymEcommerce.ViewModels.Admin;

public class ProductoFilaViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Subcategoria { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public int Stock { get; set; }
    public bool Activo { get; set; }
}

// Los filtros (Q, CategoriaId, Estado, Pagina) llegan por query string.
public class ProductoListadoViewModel
{
    //atributo para buscar productos
    public string? Q { get; set; }
    public int? CategoriaId { get; set; }
    public string Estado { get; set; } = "activos"; // activos | inactivos | todos
    public int Pagina { get; set; } = 1;

    public int Total { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public List<ProductoFilaViewModel> Productos { get; set; } = new();
    public List<SelectListItem> Categorias { get; set; } = new();
}

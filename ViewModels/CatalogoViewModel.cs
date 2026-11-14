namespace GymEcommerce.ViewModels;

public class SubcategoriaMenuItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CategoriaMenuItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<SubcategoriaMenuItem> Subcategorias { get; set; } = new();
}

public class CatalogoViewModel
{
    // Acción a la que apuntan los links y el formulario ("Index" u "Ofertas")
    public string Accion { get; set; } = "Index";

    public CatalogoFiltro Filtro { get; set; } = new();
    public List<ProductoCardViewModel> Productos { get; set; } = new();
    public List<CategoriaMenuItem> Categorias { get; set; } = new();

    public int TotalProductos { get; set; }
    public int PaginaActual { get; set; }
    public int TotalPaginas { get; set; }
}

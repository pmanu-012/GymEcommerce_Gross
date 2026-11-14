namespace GymEcommerce.ViewModels;

public enum OrdenCatalogo
{
    Nombre,
    PrecioMenor,
    PrecioMayor,
    Descuento
}

// Se llena desde la query string: /Catalogo?q=barra&categoriaId=1&orden=PrecioMenor&pagina=2
public class CatalogoFiltro
{
    public string? Q { get; set; }
    public int? CategoriaId { get; set; }
    public int? SubcategoriaId { get; set; }
    public OrdenCatalogo Orden { get; set; } = OrdenCatalogo.Nombre;
    public int Pagina { get; set; } = 1;
    public bool SoloOfertas { get; set; }
}

namespace GymEcommerce.ViewModels;

public class ProductoDetalleViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }

    public decimal Precio { get; set; }
    public decimal PrecioFinal { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public bool EnOferta { get; set; }
    public int Stock { get; set; }

    public string? Uso { get; set; }
    public string? Material { get; set; }

    public int IdCategoria { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public int IdSubcategoria { get; set; }
    public string Subcategoria { get; set; } = string.Empty;

    public List<string> Disciplinas { get; set; } = new();
    public List<string> GruposMusculares { get; set; } = new();
}

using GymEcommerce.Models;

namespace GymEcommerce.ViewModels;

public class ProductoCardViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public decimal Precio { get; set; }
    public decimal PrecioFinal { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public bool EnOferta { get; set; }
    public int Stock { get; set; }

    public static ProductoCardViewModel Desde(Producto p) => new()
    {
        IdProducto = p.IdProducto,
        Nombre = p.Nombre,
        ImagenUrl = p.ImagenUrl,
        Precio = p.Precio,
        PrecioFinal = p.PrecioFinal,
        PorcentajeDescuento = p.PorcentajeDescuento,
        EnOferta = p.EnOferta,
        Stock = p.Stock
    };
}

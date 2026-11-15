namespace GymEcommerce.ViewModels;

public class CarritoItemViewModel
{
    public int IdCarritoItem { get; set; }
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public decimal PrecioUnitario { get; set; }   // precio final (con descuento si tiene)
    public decimal PrecioOriginal { get; set; }
    public bool EnOferta { get; set; }
    public int Cantidad { get; set; }
    public int Stock { get; set; }
    public bool Activo { get; set; }

    public decimal Subtotal => PrecioUnitario * Cantidad;

    // Se puede comprar si el producto sigue activo y hay stock para la cantidad pedida
    public bool Disponible => Activo && Stock >= Cantidad;
}

public class CarritoViewModel
{
    public List<CarritoItemViewModel> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal CostoEnvio { get; set; }
    public decimal EnvioGratisDesde { get; set; }

    public decimal Total => Subtotal + CostoEnvio;
    public int CantidadTotal => Items.Sum(i => i.Cantidad);
    public bool PuedeComprar => Items.Count > 0 && Items.All(i => i.Disponible);

    public string TextoCostoEnvio => CostoEnvio == 0 ? "Gratis" : CostoEnvio.ToString("C");

    public string TextoEnvio
    {
        get
        {
            if (Items.Count == 0) return string.Empty;
            var falta = EnvioGratisDesde - Subtotal;
            return falta > 0 ? $"Te faltan {falta:C} para el envío gratis." : "¡Tenés envío gratis!";
        }
    }
}

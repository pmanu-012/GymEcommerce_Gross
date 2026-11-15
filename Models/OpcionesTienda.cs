namespace GymEcommerce.Models;

// Se carga desde la sección "Tienda" de appsettings.json.
public class OpcionesTienda
{
    public decimal CostoEnvio { get; set; } = 8000m;
    public decimal EnvioGratisDesde { get; set; } = 150000m;

    public decimal CalcularEnvio(decimal subtotal) =>
        subtotal <= 0 || subtotal >= EnvioGratisDesde ? 0m : CostoEnvio;
}

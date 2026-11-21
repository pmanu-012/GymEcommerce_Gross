namespace GymEcommerce.Models;

// Se carga desde la sección "Tienda" de appsettings.json.
public class OpcionesTienda
{
    public decimal CostoEnvio { get; set; } = 8000m;
    public decimal EnvioGratisDesde { get; set; } = 150000m;

    //método calculado: 
    //si el subtotal es menor o igual a 0 o mayor o igual a EnvioGratisDesde, el
    //costo de envío es 0, de lo contrario es CostoEnvio.
    public decimal CalcularEnvio(decimal subtotal) =>
        subtotal <= 0 || subtotal >= EnvioGratisDesde ? 0m : CostoEnvio;
}

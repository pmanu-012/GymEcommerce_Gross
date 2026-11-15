using GymEcommerce.Models;

namespace GymEcommerce.ViewModels;

public class PedidoResumenViewModel
{
    public int IdPedido { get; set; }
    public DateTime Fecha { get; set; }
    public EstadoPedido Estado { get; set; }
    public decimal Total { get; set; }
    public int CantidadProductos { get; set; }
}

public class PedidoDetalleItemViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
}

public class PedidoDetalleViewModel
{
    public int IdPedido { get; set; }
    public DateTime Fecha { get; set; }
    public EstadoPedido Estado { get; set; }
    public decimal CostoEnvio { get; set; }
    public decimal Total { get; set; }

    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;

    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }

    public List<PedidoDetalleItemViewModel> Items { get; set; } = new();
    public List<EstadoPedido> EstadosSiguientes { get; set; } = new();

    public decimal Subtotal => Items.Sum(i => i.Subtotal);
    public bool PuedeCancelarCliente => Estado == EstadoPedido.Pendiente;
}

using GymEcommerce.Models;

namespace GymEcommerce.ViewModels.Admin;

public class PedidoFilaAdminViewModel
{
    public int IdPedido { get; set; }
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string? Email { get; set; }
    public EstadoPedido Estado { get; set; }
    public decimal Total { get; set; }
}

// Los filtros (Q, Estado, Pagina) llegan por query string.
public class PedidoListadoAdminViewModel
{
    public string? Q { get; set; }
    public EstadoPedido? Estado { get; set; }
    public int Pagina { get; set; } = 1;

    public int Total { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public List<PedidoFilaAdminViewModel> Pedidos { get; set; } = new();
}

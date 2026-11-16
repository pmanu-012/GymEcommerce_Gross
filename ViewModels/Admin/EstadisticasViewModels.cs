using GymEcommerce.Models;

namespace GymEcommerce.ViewModels.Admin;

public class PuntoSerie
{
    public string Etiqueta { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public class RankingItem
{
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal Ingresos { get; set; }
}

public class StockBajoItem
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
}

public class EstadisticasViewModel
{
    public static readonly (int Dias, string Texto)[] Periodos =
    {
        (7, "Últimos 7 días"), (30, "Últimos 30 días"), (90, "Últimos 90 días"), (365, "Últimos 12 meses")
    };

    public int Dias { get; set; } = 30;
    public DateTime DesdeLocal { get; set; }
    public bool Mensual { get; set; }   // con 12 meses la serie se agrupa por mes

    // Indicadores (sin pedidos cancelados)
    public decimal Ventas { get; set; }
    public decimal? VariacionVentas { get; set; } // % contra el período anterior de igual duración
    public int Pedidos { get; set; }
    public decimal TicketPromedio => Pedidos == 0 ? 0 : Ventas / Pedidos;
    public int Unidades { get; set; }

    public List<PuntoSerie> Serie { get; set; } = new();
    public List<RankingItem> TopProductos { get; set; } = new();
    public List<RankingItem> VentasPorCategoria { get; set; } = new();
    public Dictionary<EstadoPedido, int> PedidosPorEstado { get; set; } = new();

    // Lo que requiere atención (no depende del período)
    public int PendientesTotal { get; set; }
    public List<PedidoFilaAdminViewModel> UltimosPendientes { get; set; } = new();
    public List<StockBajoItem> StockBajo { get; set; } = new();
}

namespace GymEcommerce.Models;

// Qué cambios de estado están permitidos para un pedido.
// Pendiente → Confirmado → Enviado → Entregado. Se puede cancelar mientras esté Pendiente o Confirmado.
public static class TransicionesPedido
{
    private static readonly Dictionary<EstadoPedido, EstadoPedido[]> Permitidas = new()
    {
        [EstadoPedido.Pendiente] = new[] { EstadoPedido.Confirmado, EstadoPedido.Cancelado },
        [EstadoPedido.Confirmado] = new[] { EstadoPedido.Enviado, EstadoPedido.Cancelado },
        [EstadoPedido.Enviado] = new[] { EstadoPedido.Entregado },
        [EstadoPedido.Entregado] = Array.Empty<EstadoPedido>(),
        [EstadoPedido.Cancelado] = Array.Empty<EstadoPedido>()
    };

    public static IReadOnlyList<EstadoPedido> Siguientes(EstadoPedido actual) => Permitidas[actual];

    public static bool EsValida(EstadoPedido actual, EstadoPedido nuevo) => Permitidas[actual].Contains(nuevo);
}

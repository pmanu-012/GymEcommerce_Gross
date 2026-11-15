using GymEcommerce.Models;

namespace GymEcommerce.Services;

// Textos y colores de cada estado del pedido, para usar en las vistas.
public static class EstadoPedidoUi
{
    public static string Texto(this EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => "Pendiente",
        EstadoPedido.Confirmado => "Confirmado",
        EstadoPedido.Enviado => "Enviado",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Cancelado => "Cancelado",
        _ => estado.ToString()
    };

    public static string ClaseBadge(this EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => "text-bg-warning",
        EstadoPedido.Confirmado => "text-bg-info",
        EstadoPedido.Enviado => "text-bg-dark",
        EstadoPedido.Entregado => "text-bg-success",
        _ => "text-bg-secondary"
    };

    // Texto del botón que lleva a ese estado (panel de administración)
    public static string TextoAccion(this EstadoPedido estado) => estado switch
    {
        EstadoPedido.Confirmado => "Confirmar pedido",
        EstadoPedido.Enviado => "Marcar como enviado",
        EstadoPedido.Entregado => "Marcar como entregado",
        EstadoPedido.Cancelado => "Cancelar pedido",
        _ => estado.ToString()
    };
}

namespace GymEcommerce.Models;

// Qué cambios de estado están permitidos para un pedido.
// Pendiente → Confirmado → Enviado → Entregado. Se puede cancelar mientras esté Pendiente o Confirmado.
public static class TransicionesPedido
{
    //diccionario par clave-valor, donde la clave es el estado actual y el valor una lista de estados permitidos para cada estado.
    //centraliza reglas y evita escribir muchos if o un switch.
    private static readonly Dictionary<EstadoPedido, EstadoPedido[]> Permitidas = new()
    {
        [EstadoPedido.Pendiente] = new[] { EstadoPedido.Confirmado, EstadoPedido.Cancelado },
        [EstadoPedido.Confirmado] = new[] { EstadoPedido.Enviado, EstadoPedido.Cancelado },
        [EstadoPedido.Enviado] = new[] { EstadoPedido.Entregado },
        [EstadoPedido.Entregado] = Array.Empty<EstadoPedido>(),
        [EstadoPedido.Cancelado] = Array.Empty<EstadoPedido>()
    };

    // devuelve los estados a los que puede pasar un pedido desde su estado actual
    public static IReadOnlyList<EstadoPedido> Siguientes(EstadoPedido actual) => Permitidas[actual];

    //devuelve true o false depende si el cambio de estado es válido o no.
    //Contains comprueba si el estado nuevo está dentro de los estados permitidos para el estado actual.
    public static bool EsValida(EstadoPedido actual, EstadoPedido nuevo) => Permitidas[actual].Contains(nuevo);
}

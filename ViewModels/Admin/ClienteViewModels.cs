using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels.Admin;

public class ClienteFilaViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public bool EmailConfirmado { get; set; }
    public int Pedidos { get; set; }
    public decimal TotalGastado { get; set; }
}

// Filtros por query string
public class ClienteListadoViewModel
{
    public string? Q { get; set; }
    public string Estado { get; set; } = "activos"; // activos | inactivos | todos
    public int Pagina { get; set; } = 1;

    public int Total { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public List<ClienteFilaViewModel> Clientes { get; set; } = new();
}

public class ClienteDetalleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public bool EmailConfirmado { get; set; }
    public bool Bloqueado { get; set; }

    public int CantidadPedidos { get; set; }
    public decimal TotalGastado { get; set; }
    public int CantidadResenas { get; set; }
    public List<PedidoFilaAdminViewModel> Pedidos { get; set; } = new();
}

public class ClienteEditViewModel
{
    public string Id { get; set; } = string.Empty;

    // Solo se muestra: cambiar el email requeriría volver a confirmarlo
    public string? Email { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
    [StringLength(30, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Teléfono (opcional)")]
    public string? Telefono { get; set; }
}

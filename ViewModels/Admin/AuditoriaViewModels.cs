namespace GymEcommerce.ViewModels.Admin;

public class CambioAuditoria
{
    public string Campo { get; set; } = string.Empty;
    public string? Anterior { get; set; }
    public string? Nuevo { get; set; }
}

public class AuditoriaFilaViewModel
{
    public int IdAuditoria { get; set; }
    public DateTime Fecha { get; set; }
    public string? Usuario { get; set; }   // null = Sistema
    public string? Email { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? IdEntidad { get; set; }
    public List<CambioAuditoria> Cambios { get; set; } = new();
}

// Filtros por query string (los usan el listado y la exportación a PDF)
public class AuditoriaFiltroViewModel
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Usuario { get; set; }
    public string? Entidad { get; set; }
    public string? Accion { get; set; }
    public int Pagina { get; set; } = 1;
}

public class AuditoriaListadoViewModel : AuditoriaFiltroViewModel
{
    public static readonly string[] Acciones = { "Crear", "Editar", "Eliminar", "Dar de baja", "Reactivar" };

    public int Total { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public List<AuditoriaFilaViewModel> Filas { get; set; } = new();
    public List<string> Entidades { get; set; } = new();
}

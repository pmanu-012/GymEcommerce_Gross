namespace GymEcommerce.ViewModels.Admin;

public class ResenaAdminFilaViewModel
{
    public int IdResena { get; set; }
    public DateTime Fecha { get; set; }
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? IdProducto { get; set; }
    public string? Producto { get; set; }
}

// Filtros por query string
public class ResenasAdminViewModel
{
    public string Tipo { get; set; } = "producto"; // producto | plataforma
    public int? Calificacion { get; set; }
    public int Pagina { get; set; } = 1;

    public int Total { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public List<ResenaAdminFilaViewModel> Resenas { get; set; } = new();
}

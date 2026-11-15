using System.ComponentModel.DataAnnotations;

namespace GymEcommerce.ViewModels.Admin;

// ---------- Listado ----------

public class IngresoFilaViewModel
{
    public int IdIngreso { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal Total { get; set; }
    public bool Anulado { get; set; }
}

// Los filtros llegan por query string.
public class IngresoListadoViewModel
{
    public string? Q { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string Estado { get; set; } = "todos"; // vigentes | anulados | todos
    public int Pagina { get; set; } = 1;

    public int Total { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public List<IngresoFilaViewModel> Ingresos { get; set; } = new();
}

// ---------- Formulario ----------

public class DetalleIngresoFormViewModel
{
    [Required(ErrorMessage = "Elegí un producto.")]
    public int? IdProducto { get; set; }

    [Required(ErrorMessage = "Ingresá la cantidad.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe estar entre 1 y 100.000.")]
    public int? Cantidad { get; set; }

    [Required(ErrorMessage = "Ingresá el costo.")]
    [Range(0, 100000000, ErrorMessage = "El costo debe ser 0 o más.")]
    public decimal? CostoUnitario { get; set; }
}

public class ProductoOpcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    public bool Activo { get; set; }
}

public class IngresoFormViewModel
{
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Proveedor")]
    public string Proveedor { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "N° de comprobante (opcional)")]
    public string? NumeroComprobante { get; set; }

    [StringLength(300, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Observaciones (opcional)")]
    public string? Observaciones { get; set; }

    public List<DetalleIngresoFormViewModel> Detalles { get; set; } = new();

    // Opciones del select (las carga el controlador, no vienen del formulario)
    public List<ProductoOpcion> Productos { get; set; } = new();
}

// Lo que necesita el parcial de una fila del formulario
public class FilaIngresoViewModel
{
    public string Indice { get; set; } = "0";   // "0", "1"... o "__i__" en la plantilla
    public int Numero { get; set; }
    public DetalleIngresoFormViewModel Fila { get; set; } = new();
    public List<ProductoOpcion> Productos { get; set; } = new();
}

// ---------- Detalle ----------

public class IngresoDetalleItemViewModel
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal => Cantidad * CostoUnitario;
}

public class IngresoDetalleViewModel
{
    public int IdIngreso { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }
    public string? Observaciones { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public List<IngresoDetalleItemViewModel> Items { get; set; } = new();

    public decimal Total => Items.Sum(i => i.Subtotal);
    public int Unidades => Items.Sum(i => i.Cantidad);
}

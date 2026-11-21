using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymEcommerce.Models;

public class Producto
{
    [Key]
    public int IdProducto { get; set; }

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    // Porcentaje (0 a 100) que carga el administrador. Null = sin oferta.
    public decimal? PorcentajeDescuento { get; set; }

    public int Stock { get; set; }

    [StringLength(300)]
    public string? ImagenUrl { get; set; }
    [StringLength(300)]
    public string? Descripcion { get; set; }

    // Atributos que no aplican a todos los productos (ropa, accesorios, etc.)
    [StringLength(100)]
    public string? Uso { get; set; }

    [StringLength(100)]
    public string? Material { get; set; }

    // Baja lógica
    public bool Activo { get; set; } = true;

    // Control de concurrencia (evita vender stock que ya no existe)
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public int IdSubcategoria { get; set; }
    //se consigue la categoria del producto a través de la subcategoria, que a su vez tiene la categoria 
    public Subcategoria Subcategoria { get; set; } = null!;

    public ICollection<ProductoDisciplina> ProductoDisciplinas { get; set; } = new List<ProductoDisciplina>();
    public ICollection<ProductoGrupoMuscular> ProductoGruposMusculares { get; set; } = new List<ProductoGrupoMuscular>();
    public ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();
    public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    public ICollection<ResenaProducto> Resenas { get; set; } = new List<ResenaProducto>();

    // Calculados en C#, no existen como columnas.
    // este bool es true si el producto tiene un descuento cargado y mayor a 0.
    [NotMapped]
    public bool EnOferta => PorcentajeDescuento.HasValue && PorcentajeDescuento.Value > 0;
    //calcula el precio final del producto, si tiene descuento aplica el porcentaje,
    //sino devuelve el precio original

    [NotMapped]
    public decimal PrecioFinal => EnOferta
        ? Math.Round(Precio * (1 - PorcentajeDescuento!.Value / 100m), 2)
        : Precio;
}

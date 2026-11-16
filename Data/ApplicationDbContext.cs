using GymEcommerce.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace GymEcommerce.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // El nombre de cada DbSet es el nombre de la tabla en la base.
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Subcategoria> Subcategorias { get; set; } = null!;
    public DbSet<Disciplina> Disciplinas { get; set; } = null!;
    public DbSet<GrupoMuscular> GruposMusculares { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<ProductoDisciplina> ProductoDisciplinas { get; set; } = null!;
    public DbSet<ProductoGrupoMuscular> ProductoGruposMusculares { get; set; } = null!;
    public DbSet<CarritoItem> CarritoItems { get; set; } = null!;
    public DbSet<Pedido> Pedidos { get; set; } = null!;
    public DbSet<DetallePedido> DetallesPedido { get; set; } = null!;
    public DbSet<ResenaProducto> ResenasProducto { get; set; } = null!;
    public DbSet<ResenaPlataforma> ResenasPlataforma { get; set; } = null!;
    public DbSet<Auditoria> Auditorias { get; set; } = null!;
    public DbSet<IngresoMercaderia> IngresosMercaderia { get; set; } = null!;
    public DbSet<DetalleIngreso> DetallesIngreso { get; set; } = null!;

    // Tabla técnica de ASP.NET (claves de Data Protection); no forma parte del DER
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // imprescindible: configura las tablas de Identity

        // ---------- Catálogo ----------
        b.Entity<Subcategoria>()
            .HasOne(s => s.Categoria).WithMany(c => c.Subcategorias)
            .HasForeignKey(s => s.IdCategoria)
            .OnDelete(DeleteBehavior.Restrict); // no se borra una categoría que tiene subcategorías

        b.Entity<Producto>(e =>
        {
            e.Property(p => p.Precio).HasPrecision(18, 2);
            e.Property(p => p.PorcentajeDescuento).HasPrecision(5, 2);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Productos_Stock", "[Stock] >= 0");
                t.HasCheckConstraint("CK_Productos_Descuento",
                    "[PorcentajeDescuento] IS NULL OR ([PorcentajeDescuento] >= 0 AND [PorcentajeDescuento] <= 100)");
            });

            e.HasOne(p => p.Subcategoria).WithMany(s => s.Productos)
                .HasForeignKey(p => p.IdSubcategoria)
                .OnDelete(DeleteBehavior.Restrict); // no se borra una subcategoría con productos
        });

        // ---------- Relaciones N:M (tablas intermedias) ----------
        b.Entity<ProductoDisciplina>(e =>
        {
            e.HasIndex(x => new { x.IdProducto, x.IdDisciplina }).IsUnique();
            e.HasOne(x => x.Producto).WithMany(p => p.ProductoDisciplinas)
                .HasForeignKey(x => x.IdProducto).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Disciplina).WithMany(d => d.ProductoDisciplinas)
                .HasForeignKey(x => x.IdDisciplina).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ProductoGrupoMuscular>(e =>
        {
            e.HasIndex(x => new { x.IdProducto, x.IdGrupoMuscular }).IsUnique();
            e.HasOne(x => x.Producto).WithMany(p => p.ProductoGruposMusculares)
                .HasForeignKey(x => x.IdProducto).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GrupoMuscular).WithMany(g => g.ProductoGruposMusculares)
                .HasForeignKey(x => x.IdGrupoMuscular).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Carrito ----------
        b.Entity<CarritoItem>(e =>
        {
            // Un producto aparece una sola vez por carrito: se incrementa Cantidad.
            e.HasIndex(c => new { c.IdUsuario, c.IdProducto }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_CarritoItems_Cantidad", "[Cantidad] > 0"));

            e.HasOne(c => c.Usuario).WithMany(u => u.CarritoItems)
                .HasForeignKey(c => c.IdUsuario).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Producto).WithMany(p => p.CarritoItems)
                .HasForeignKey(c => c.IdProducto).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Pedidos ----------
        b.Entity<Pedido>(e =>
        {
            e.Property(p => p.Total).HasPrecision(18, 2);
            e.Property(p => p.CostoEnvio).HasPrecision(18, 2);
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20); // se guarda como texto

            e.HasOne(p => p.Usuario).WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DetallePedido>(e =>
        {
            e.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            e.ToTable(t => t.HasCheckConstraint("CK_DetallesPedido_Cantidad", "[Cantidad] > 0"));

            e.HasOne(d => d.Pedido).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.IdPedido).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Producto).WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict); // el historial no se pierde
        });

        // ---------- Reseñas ----------
        b.Entity<ResenaProducto>(e =>
        {
            // Una reseña por usuario y producto (si no lo querés, borrá esta línea).
            e.HasIndex(r => new { r.IdUsuario, r.IdProducto }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_ResenasProducto_Calificacion", "[Calificacion] BETWEEN 1 AND 5"));

            e.HasOne(r => r.Usuario).WithMany(u => u.ResenasProducto)
                .HasForeignKey(r => r.IdUsuario).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Producto).WithMany(p => p.Resenas)
                .HasForeignKey(r => r.IdProducto).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ResenaPlataforma>(e =>
        {
            e.HasIndex(r => r.IdUsuario).IsUnique(); // una opinión por cliente (se puede editar)
            e.ToTable(t => t.HasCheckConstraint("CK_ResenasPlataforma_Calificacion", "[Calificacion] BETWEEN 1 AND 5"));

            e.HasOne(r => r.Usuario).WithMany(u => u.ResenasPlataforma)
                .HasForeignKey(r => r.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Ingresos de mercadería ----------
        b.Entity<IngresoMercaderia>(e =>
        {
            e.HasIndex(i => i.Fecha);
            e.HasOne(i => i.Usuario).WithMany()
                .HasForeignKey(i => i.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DetalleIngreso>(e =>
        {
            e.Property(d => d.CostoUnitario).HasPrecision(18, 2);
            e.HasIndex(d => new { d.IdIngreso, d.IdProducto }).IsUnique();
            e.ToTable(t =>
            {
                 t.HasCheckConstraint("CK_DetallesIngreso_Cantidad", "[Cantidad] > 0");
                 t.HasCheckConstraint("CK_DetallesIngreso_Costo", "[CostoUnitario] >= 0");
            });
            e.HasOne(d => d.Ingreso).WithMany(i => i.Detalles)
                .HasForeignKey(d => d.IdIngreso).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Producto).WithMany()
                .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Auditoría ----------
        b.Entity<Auditoria>(e =>
        {
            e.HasIndex(a => a.Fecha);
            e.HasOne(a => a.Usuario).WithMany(u => u.Auditorias)
                .HasForeignKey(a => a.IdUsuario).OnDelete(DeleteBehavior.Restrict); // la auditoría nunca se pierde
        });

        b.Entity<Categoria>().HasIndex(c => c.Nombre).IsUnique();
        b.Entity<Disciplina>().HasIndex(d => d.Nombre).IsUnique();
        b.Entity<GrupoMuscular>().HasIndex(g => g.Nombre).IsUnique();
        b.Entity<Subcategoria>().HasIndex(s => new { s.IdCategoria, s.Nombre }).IsUnique();
    }
}

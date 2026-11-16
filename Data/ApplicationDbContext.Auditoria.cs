using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GymEcommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GymEcommerce.Data;

// Auditoría automática: cada vez que se guarda algo, se registra quién cambió qué.
// Es la otra mitad (partial) de ApplicationDbContext; el constructor está en ApplicationDbContext.cs.
public partial class ApplicationDbContext
{
    // Para saber qué usuario hizo la operación. Es null cuando no hay petición web (por ejemplo, el seed).
    private readonly IHttpContextAccessor? _http;

    // Entidades que se auditan. El carrito, los detalles y las tablas técnicas quedan afuera (generarían ruido).
    private static readonly HashSet<Type> TiposAuditados = new()
    {
        typeof(Producto), typeof(Categoria), typeof(Subcategoria), typeof(Disciplina), typeof(GrupoMuscular),
        typeof(Pedido), typeof(IngresoMercaderia), typeof(ResenaProducto), typeof(ResenaPlataforma), typeof(ApplicationUser)
    };

    private static readonly Dictionary<Type, string> NombresEntidad = new()
    {
        [typeof(Producto)] = "Producto",
        [typeof(Categoria)] = "Categoría",
        [typeof(Subcategoria)] = "Subcategoría",
        [typeof(Disciplina)] = "Disciplina",
        [typeof(GrupoMuscular)] = "Grupo muscular",
        [typeof(Pedido)] = "Pedido",
        [typeof(IngresoMercaderia)] = "Ingreso de mercadería",
        [typeof(ResenaProducto)] = "Reseña de producto",
        [typeof(ResenaPlataforma)] = "Opinión de la tienda",
        [typeof(ApplicationUser)] = "Usuario"
    };

    // Datos técnicos que cambian solos (por ejemplo, al iniciar sesión): no se registran
    private static readonly HashSet<string> PropiedadesIgnoradas = new()
    {
        "RowVersion", "SecurityStamp", "ConcurrencyStamp", "AccessFailedCount", "LockoutEnd",
        "NormalizedEmail", "NormalizedUserName"
    };

    // Se registra que cambiaron, pero nunca su valor
    private static readonly HashSet<string> PropiedadesSensibles = new() { "PasswordHash" };

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // deja legibles las tildes y la ñ
        Converters = { new JsonStringEnumConverter() }         // "Enviado" en lugar de 2
    };

    private sealed record CambioPendiente(EntityEntry Entrada, Auditoria Registro);

    // SaveChangesAsync() sin parámetros también termina acá, así que cubre todos los guardados de la aplicación
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (!acceptAllChangesOnSuccess)
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        ChangeTracker.DetectChanges();
        var pendientes = PrepararAuditoria();
        if (pendientes.Count == 0)
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // Las altas recién tienen Id después de guardarse, así que se guarda en dos pasos
        // (datos y después auditoría) dentro de una misma transacción: o queda todo, o nada.
        var transaccion = Database.CurrentTransaction is null
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            foreach (var pendiente in pendientes)
            {
                if (pendiente.Registro.Accion == "Crear")
                {
                    pendiente.Registro.IdEntidad = Clave(pendiente.Entrada);
                    pendiente.Registro.ValorNuevo = Serializar(Valores(pendiente.Entrada.Properties, actuales: true));
                }
                Auditorias.Add(pendiente.Registro);
            }

            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (transaccion is not null)
                await transaccion.CommitAsync(cancellationToken);

            return filas;
        }
        catch
        {
            if (transaccion is not null)
                await transaccion.RollbackAsync(CancellationToken.None);
            throw; // el que llamó recibe la excepción original (por ejemplo, la de concurrencia)
        }
        finally
        {
            if (transaccion is not null)
                await transaccion.DisposeAsync();
        }
    }

    private List<CambioPendiente> PrepararAuditoria()
    {
        var idUsuario = _http?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ahora = DateTime.UtcNow;
        var pendientes = new List<CambioPendiente>();

        foreach (var entrada in ChangeTracker.Entries().ToList())
        {
            var tipo = entrada.Metadata.ClrType;
            if (!TiposAuditados.Contains(tipo))
                continue;

            var registro = entrada.State switch
            {
                EntityState.Added => new Auditoria { Accion = "Crear" },
                EntityState.Modified => RegistroDeEdicion(entrada),
                EntityState.Deleted => new Auditoria
                {
                    Accion = "Eliminar",
                    IdEntidad = Clave(entrada),
                    ValorAnterior = Serializar(Valores(entrada.Properties, actuales: false))
                },
                _ => null
            };

            if (registro is null)
                continue;

            registro.IdUsuario = idUsuario;
            registro.Fecha = ahora;
            registro.Entidad = NombresEntidad.GetValueOrDefault(tipo, tipo.Name);
            pendientes.Add(new CambioPendiente(entrada, registro));
        }

        return pendientes;
    }

    // En una edición solo se guardan los campos que cambiaron (antes y después)
    private static Auditoria? RegistroDeEdicion(EntityEntry entrada)
    {
        var modificadas = entrada.Properties
            .Where(p => p.IsModified
                        && !PropiedadesIgnoradas.Contains(p.Metadata.Name)
                        && !Equals(p.OriginalValue, p.CurrentValue))
            .ToList();

        if (modificadas.Count == 0)
            return null; // solo cambiaron datos técnicos

        // La baja lógica se registra con su propio nombre
        var accion = "Editar";
        if (modificadas.Count == 1 && modificadas[0].Metadata.Name == "Activo")
            accion = modificadas[0].CurrentValue is true ? "Reactivar" : "Dar de baja";

        return new Auditoria
        {
            Accion = accion,
            IdEntidad = Clave(entrada),
            ValorAnterior = Serializar(Valores(modificadas, actuales: false)),
            ValorNuevo = Serializar(Valores(modificadas, actuales: true))
        };
    }

    private static Dictionary<string, object?> Valores(IEnumerable<PropertyEntry> propiedades, bool actuales)
    {
        var resultado = new Dictionary<string, object?>();
        foreach (var propiedad in propiedades)
        {
            var nombre = propiedad.Metadata.Name;
            if (PropiedadesIgnoradas.Contains(nombre))
                continue;

            var valor = actuales ? propiedad.CurrentValue : propiedad.OriginalValue;
            resultado[nombre] = PropiedadesSensibles.Contains(nombre) && valor is not null ? "***" : valor;
        }
        return resultado;
    }

    private static string Clave(EntityEntry entrada) =>
        string.Join(",", entrada.Metadata.FindPrimaryKey()!.Properties
            .Select(p => entrada.Property(p.Name).CurrentValue));

    private static string? Serializar(Dictionary<string, object?> valores) =>
        valores.Count == 0 ? null : JsonSerializer.Serialize(valores, OpcionesJson);
}

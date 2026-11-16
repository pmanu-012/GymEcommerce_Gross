using System.Text.Json;
using GymEcommerce.ViewModels.Admin;

namespace GymEcommerce.Services;

// Convierte los JSON de ValorAnterior/ValorNuevo en una lista de cambios para mostrar
public static class AuditoriaFormato
{
    public static List<CambioAuditoria> Cambios(string? anterior, string? nuevo)
    {
        var antes = Leer(anterior);
        var despues = Leer(nuevo);

        return despues.Keys.Concat(antes.Keys).Distinct()
            .Select(campo => new CambioAuditoria
            {
                Campo = campo,
                Anterior = antes.GetValueOrDefault(campo),
                Nuevo = despues.GetValueOrDefault(campo)
            })
            .ToList();
    }

    // Una línea de texto por cambio (para el PDF)
    public static string Linea(CambioAuditoria cambio, string accion)
    {
        string Corto(string? valor) => valor is null ? "(vacío)" : valor.Length > 80 ? valor[..80] + "..." : valor;

        return accion switch
        {
            "Crear" => $"{cambio.Campo}: {Corto(cambio.Nuevo)}",
            "Eliminar" => $"{cambio.Campo}: {Corto(cambio.Anterior)}",
            _ => $"{cambio.Campo}: {Corto(cambio.Anterior)} -> {Corto(cambio.Nuevo)}"
        };
    }

    private static Dictionary<string, string?> Leer(string? json)
    {
        var resultado = new Dictionary<string, string?>();
        if (string.IsNullOrWhiteSpace(json))
            return resultado;

        try
        {
            using var documento = JsonDocument.Parse(json);
            foreach (var propiedad in documento.RootElement.EnumerateObject())
            {
                resultado[propiedad.Name] = propiedad.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => propiedad.Value.GetString(),
                    _ => propiedad.Value.GetRawText()
                };
            }
        }
        catch (JsonException)
        {
            resultado["(valor)"] = json;
        }

        return resultado;
    }
}

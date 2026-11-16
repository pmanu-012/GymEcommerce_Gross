using System.Globalization;
using GymEcommerce.Models;
using GymEcommerce.ViewModels;
using GymEcommerce.ViewModels.Admin;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymEcommerce.Services;

// Documentos PDF con QuestPDF: el diseño se arma con código C# (filas, columnas, tablas).
public static class GeneradorPdf
{
    private static readonly CultureInfo Ar = new("es-AR");

    private const string Carbon = "#15171A";
    private const string Acento = "#FF5B14";
    private const string Gris = "#6B7280";
    private const string Linea = "#E5E7EB";

    private static string Plata(decimal valor) => valor.ToString("C", Ar);

    // ---------- Comprobante de pedido ----------

    public static byte[] ComprobantePedido(PedidoDetalleViewModel p) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(1.5f, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontSize(10).FontColor(Carbon));

            pagina.Header().Element(c => Encabezado(c,
                $"Comprobante de pedido #{p.IdPedido}",
                $"Fecha: {Fechas.ALocal(p.Fecha):dd/MM/yyyy HH:mm}"));

            pagina.Content().PaddingVertical(15).Column(col =>
            {
                col.Spacing(14);

                // Cliente, envío y estado
                col.Item().Row(fila =>
                {
                    fila.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Cliente").FontSize(9).FontColor(Gris);
                        c.Item().Text(p.ClienteNombre).SemiBold();
                        if (!string.IsNullOrEmpty(p.ClienteEmail)) c.Item().Text(p.ClienteEmail);
                        if (!string.IsNullOrEmpty(p.ClienteTelefono)) c.Item().Text(p.ClienteTelefono);
                    });
                    fila.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Envío a").FontSize(9).FontColor(Gris);
                        c.Item().Text($"{p.Calle} {p.Numero}");
                        c.Item().Text($"{p.Ciudad}, {p.Provincia}");
                        c.Item().Text($"CP {p.CodigoPostal}");
                    });
                    fila.ConstantItem(110).Column(c =>
                    {
                        c.Item().AlignRight().Text("Estado").FontSize(9).FontColor(Gris);
                        c.Item().AlignRight().Text(p.Estado.Texto()).FontSize(13).Bold();
                    });
                });

                // Productos
                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(5);
                        c.ConstantColumn(50);
                        c.ConstantColumn(85);
                        c.ConstantColumn(85);
                    });

                    tabla.Header(h =>
                    {
                        h.Cell().Element(CeldaEncabezado).Text("Producto");
                        h.Cell().Element(CeldaEncabezado).AlignRight().Text("Cant.");
                        h.Cell().Element(CeldaEncabezado).AlignRight().Text("Precio unit.");
                        h.Cell().Element(CeldaEncabezado).AlignRight().Text("Subtotal");
                    });

                    foreach (var item in p.Items)
                    {
                        tabla.Cell().Element(Celda).Text(item.Nombre);
                        tabla.Cell().Element(Celda).AlignRight().Text(item.Cantidad.ToString());
                        tabla.Cell().Element(Celda).AlignRight().Text(Plata(item.PrecioUnitario));
                        tabla.Cell().Element(Celda).AlignRight().Text(Plata(item.Subtotal));
                    }
                });

                // Totales
                col.Item().AlignRight().Width(230).Column(t =>
                {
                    t.Spacing(3);
                    t.Item().Row(r => { r.RelativeItem().Text("Subtotal"); r.AutoItem().Text(Plata(p.Subtotal)); });
                    t.Item().Row(r => { r.RelativeItem().Text("Envío"); r.AutoItem().Text(p.CostoEnvio == 0 ? "Gratis" : Plata(p.CostoEnvio)); });
                    t.Item().LineHorizontal(1).LineColor(Linea);
                    t.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Total").Bold().FontSize(12);
                        r.AutoItem().Text(Plata(p.Total)).Bold().FontSize(12);
                    });
                });

                if (p.Estado == EstadoPedido.Cancelado)
                    col.Item().Background("#FCEBEB").Padding(8).Text("Este pedido fue cancelado.").FontColor("#A32D2D").SemiBold();

                col.Item().PaddingTop(10).Text("Documento no válido como factura. El pago de la tienda es simulado.")
                    .FontSize(8).FontColor(Gris);
            });

            pagina.Footer().Element(Pie);
        })).GeneratePdf();

    // ---------- Registro de auditoría ----------

    public static byte[] Auditoria(IReadOnlyList<AuditoriaFilaViewModel> filas, string filtros, string generadoPor, int total) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(1.2f, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontSize(8).FontColor(Carbon));

            pagina.Header().Element(c => Encabezado(c,
                "Registro de auditoría",
                $"Generado el {Fechas.ALocal(DateTime.UtcNow):dd/MM/yyyy HH:mm} por {generadoPor}"));

            pagina.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(8);
                col.Item().Text($"Filtros: {filtros}").FontSize(9);

                if (filas.Count < total)
                    col.Item().Text($"Se muestran los {filas.Count} registros más recientes de {total}. Ajustá los filtros para ver el resto.")
                        .FontSize(9).FontColor(Gris);

                if (filas.Count == 0)
                {
                    col.Item().Text("No hay registros con esos filtros.").FontSize(10);
                    return;
                }

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(70);   // Fecha
                        c.RelativeColumn(2);    // Usuario
                        c.ConstantColumn(58);   // Acción
                        c.RelativeColumn(1.6f); // Entidad
                        c.RelativeColumn(5);    // Cambios
                    });

                    tabla.Header(h =>
                    {
                        h.Cell().Element(CeldaEncabezado).Text("Fecha");
                        h.Cell().Element(CeldaEncabezado).Text("Usuario");
                        h.Cell().Element(CeldaEncabezado).Text("Acción");
                        h.Cell().Element(CeldaEncabezado).Text("Entidad");
                        h.Cell().Element(CeldaEncabezado).Text("Cambios");
                    });

                    foreach (var f in filas)
                    {
                        tabla.Cell().Element(Celda).Text(Fechas.ALocal(f.Fecha).ToString("dd/MM/yyyy HH:mm"));
                        tabla.Cell().Element(Celda).Text(f.Usuario ?? "Sistema");
                        tabla.Cell().Element(Celda).Text(f.Accion).SemiBold();
                        tabla.Cell().Element(Celda).Text($"{f.Entidad} #{f.IdEntidad}");
                        tabla.Cell().Element(Celda).Text(string.Join("\n", f.Cambios.Select(c => AuditoriaFormato.Linea(c, f.Accion))));
                    }
                });
            });

            pagina.Footer().Element(Pie);
        })).GeneratePdf();

    // ---------- Piezas comunes ----------

    private static void Encabezado(IContainer contenedor, string titulo, string subtitulo)
    {
        contenedor.Column(col =>
        {
            col.Item().Background(Carbon).PaddingVertical(10).PaddingHorizontal(14).Row(fila =>
            {
                fila.RelativeItem().AlignMiddle().Text(Marca.Nombre.ToUpperInvariant()).FontSize(18).Bold().FontColor(Colors.White);
                fila.RelativeItem().AlignMiddle().AlignRight().Text(Marca.Eslogan).FontSize(9).FontColor("#D1D5DB");
            });
            col.Item().Height(3).Background(Acento);
            col.Item().PaddingTop(12).Text(titulo).FontSize(16).Bold();
            col.Item().Text(subtitulo).FontSize(9).FontColor(Gris);
        });
    }

    private static void Pie(IContainer contenedor)
    {
        contenedor.AlignCenter().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Gris));
            t.Span($"{Marca.Nombre} · Página ");
            t.CurrentPageNumber();
            t.Span(" de ");
            t.TotalPages();
        });
    }

    private static IContainer CeldaEncabezado(IContainer c) =>
        c.Background(Carbon).PaddingVertical(5).PaddingHorizontal(6)
         .DefaultTextStyle(t => t.FontColor(Colors.White).SemiBold());

    private static IContainer Celda(IContainer c) =>
        c.BorderBottom(1).BorderColor(Linea).PaddingVertical(4).PaddingHorizontal(6);
}

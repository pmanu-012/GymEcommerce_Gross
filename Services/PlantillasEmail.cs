using System.Globalization;
using System.Net;
using System.Text;
using GymEcommerce.Models;

namespace GymEcommerce.Services;

// Textos y diseño de los emails. El HTML usa tablas y estilos en línea porque
// muchos clientes de correo (Gmail, Outlook) ignoran las hojas de estilo.
public static class PlantillasEmail
{
    private static readonly CultureInfo Ar = new("es-AR");

    private static string E(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);
    private static string Plata(decimal valor) => valor.ToString("C", Ar);

    // ---------- Cuenta ----------

    public static ContenidoEmail ConfirmarCuenta(string nombre, string url) => Armar(
        asunto: $"Confirmá tu cuenta en {Marca.Nombre}",
        titulo: $"¡Hola, {nombre}!",
        parrafos: new[]
        {
            $"Gracias por registrarte en {Marca.Nombre}.",
            "Para activar tu cuenta, confirmá tu email con el siguiente botón:"
        },
        textoBoton: "Confirmar mi email",
        url: url,
        nota: "El enlace vence en 24 horas. Si no creaste esta cuenta, ignorá este mensaje.");

    public static ContenidoEmail RestablecerContrasena(string nombre, string url) => Armar(
        asunto: "Restablecé tu contraseña",
        titulo: $"Hola, {nombre}",
        parrafos: new[]
        {
            "Recibimos un pedido para restablecer la contraseña de tu cuenta.",
            "Para elegir una nueva, usá el siguiente botón:"
        },
        textoBoton: "Crear nueva contraseña",
        url: url,
        nota: "El enlace vence en 24 horas y sirve una sola vez. Si no lo pediste, ignorá este mensaje: tu contraseña actual sigue funcionando.");

    public static ContenidoEmail ContrasenaCambiada(string nombre) => Armar(
        asunto: "Tu contraseña se cambió",
        titulo: $"Hola, {nombre}",
        parrafos: new[]
        {
            "Te avisamos que la contraseña de tu cuenta se cambió recién.",
            "Si fuiste vos, no tenés que hacer nada. Si no, restablecela desde \"¿Olvidaste tu contraseña?\" en la página de ingreso."
        });

    // ---------- Pedidos ----------

    public static ContenidoEmail PedidoRecibido(string nombre, int idPedido,
        IEnumerable<(string Nombre, int Cantidad, decimal Precio)> items, decimal costoEnvio, decimal total, string url)
    {
        var lista = items.ToList();

        var html = new StringBuilder();
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:8px 0 4px;font-size:14px;border-collapse:collapse;\">");
        foreach (var i in lista)
        {
            html.Append("<tr>")
                .Append($"<td style=\"padding:6px 0;border-bottom:1px solid #E5E7EB;\">{i.Cantidad} × {E(i.Nombre)}</td>")
                .Append($"<td align=\"right\" style=\"padding:6px 0;border-bottom:1px solid #E5E7EB;white-space:nowrap;\">{Plata(i.Precio * i.Cantidad)}</td>")
                .Append("</tr>");
        }
        html.Append($"<tr><td style=\"padding:6px 0;\">Envío</td><td align=\"right\" style=\"padding:6px 0;\">{(costoEnvio == 0 ? "Gratis" : Plata(costoEnvio))}</td></tr>");
        html.Append($"<tr><td style=\"padding:6px 0;font-weight:bold;\">Total</td><td align=\"right\" style=\"padding:6px 0;font-weight:bold;font-size:16px;\">{Plata(total)}</td></tr>");
        html.Append("</table>");

        var texto = new StringBuilder();
        foreach (var i in lista)
            texto.AppendLine($"{i.Cantidad} x {i.Nombre}: {Plata(i.Precio * i.Cantidad)}");
        texto.AppendLine($"Envío: {(costoEnvio == 0 ? "Gratis" : Plata(costoEnvio))}");
        texto.AppendLine($"Total: {Plata(total)}");

        return Armar(
            asunto: $"Recibimos tu pedido #{idPedido}",
            titulo: "¡Gracias por tu compra!",
            parrafos: new[]
            {
                $"Hola, {nombre}. Tu pedido #{idPedido} quedó registrado y está pendiente de confirmación.",
                "Te vamos a avisar por email cada vez que cambie de estado."
            },
            textoBoton: "Ver mi pedido",
            url: url,
            extraHtml: html.ToString(),
            extraTexto: texto.ToString());
    }

    public static ContenidoEmail CambioEstado(string nombre, int idPedido, EstadoPedido estado, string url)
    {
        var (asunto, frase) = estado switch
        {
            EstadoPedido.Confirmado => ($"Confirmamos tu pedido #{idPedido}", "Ya estamos preparando tu pedido."),
            EstadoPedido.Enviado => ($"Tu pedido #{idPedido} está en camino", "Lo despachamos a la dirección que indicaste."),
            EstadoPedido.Entregado => ($"Tu pedido #{idPedido} fue entregado", "¡Que lo disfrutes! Si querés, contanos qué te pareció dejando una reseña."),
            EstadoPedido.Cancelado => ($"Tu pedido #{idPedido} fue cancelado", "Si tenés dudas sobre la cancelación, respondé este email."),
            _ => ($"Actualización de tu pedido #{idPedido}", $"Tu pedido ahora está {estado.Texto().ToLower()}.")
        };

        return Armar(
            asunto: asunto,
            titulo: $"Hola, {nombre}",
            parrafos: new[] { $"Tu pedido #{idPedido} cambió de estado: {estado.Texto().ToLower()}.", frase },
            textoBoton: "Ver mi pedido",
            url: url);
    }

    // ---------- Prueba ----------

    public static ContenidoEmail Prueba() => Armar(
        asunto: $"Email de prueba de {Marca.Nombre}",
        titulo: "Prueba de envío",
        parrafos: new[] { "Si estás leyendo esto, el envío de emails funciona correctamente." });

    // ---------- Diseño común ----------

    private static ContenidoEmail Armar(string asunto, string titulo, IEnumerable<string> parrafos,
        string? textoBoton = null, string? url = null, string? extraHtml = null, string? extraTexto = null, string? nota = null)
    {
        var listaParrafos = parrafos.ToList();
        var html = new StringBuilder();

        html.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"></head>");
        html.Append("<body style=\"margin:0;padding:0;background:#F5F5F2;font-family:Arial,Helvetica,sans-serif;color:#15171A;\">");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F5F5F2;padding:24px 12px;\"><tr><td align=\"center\">");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;background:#ffffff;border-radius:8px;overflow:hidden;\">");

        // Encabezado con la marca
        html.Append("<tr><td style=\"background:#15171A;border-bottom:4px solid #FF5B14;padding:18px 28px;\">")
            .Append($"<span style=\"font-size:24px;font-weight:bold;letter-spacing:1px;text-transform:uppercase;color:#ffffff;\"><span style=\"color:#FF5B14;\">&#9889;</span> {E(Marca.Nombre)}</span>")
            .Append("</td></tr>");

        // Cuerpo
        html.Append("<tr><td style=\"padding:28px;\">");
        html.Append($"<h1 style=\"margin:0 0 16px;font-size:22px;\">{E(titulo)}</h1>");
        foreach (var p in listaParrafos)
            html.Append($"<p style=\"margin:0 0 14px;font-size:15px;line-height:1.6;\">{E(p)}</p>");

        if (extraHtml is not null)
            html.Append(extraHtml);

        if (textoBoton is not null && url is not null)
        {
            html.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:24px 0;\"><tr>")
                .Append("<td style=\"background:#FF5B14;border-radius:6px;\">")
                .Append($"<a href=\"{E(url)}\" style=\"display:inline-block;padding:12px 26px;color:#15171A;font-weight:bold;font-size:15px;text-decoration:none;text-transform:uppercase;letter-spacing:.5px;\">{E(textoBoton)}</a>")
                .Append("</td></tr></table>");
            html.Append("<p style=\"margin:0 0 14px;font-size:12px;line-height:1.5;color:#6B7280;\">Si el botón no funciona, copiá este enlace en el navegador:<br>")
                .Append($"<a href=\"{E(url)}\" style=\"color:#C2410C;word-break:break-all;\">{E(url)}</a></p>");
        }

        if (nota is not null)
            html.Append($"<p style=\"margin:16px 0 0;font-size:13px;line-height:1.5;color:#6B7280;\">{E(nota)}</p>");

        html.Append("</td></tr>");

        // Pie
        html.Append("<tr><td style=\"padding:16px 28px;background:#F5F5F2;font-size:12px;color:#6B7280;\">")
            .Append($"{E(Marca.Nombre)} · {E(Marca.Eslogan)}<br>Este es un email automático.")
            .Append("</td></tr>");

        html.Append("</table></td></tr></table></body></html>");

        // Versión en texto plano (para clientes que no muestran HTML)
        var texto = new StringBuilder();
        texto.AppendLine(titulo).AppendLine();
        foreach (var p in listaParrafos)
            texto.AppendLine(p);
        if (extraTexto is not null)
            texto.AppendLine().Append(extraTexto);
        if (url is not null)
            texto.AppendLine().AppendLine($"{textoBoton}: {url}");
        if (nota is not null)
            texto.AppendLine().AppendLine(nota);
        texto.AppendLine().AppendLine($"{Marca.Nombre} · {Marca.Eslogan}");

        return new ContenidoEmail(asunto, html.ToString(), texto.ToString());
    }
}

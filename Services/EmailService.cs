using GymEcommerce.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GymEcommerce.Services;

// Se carga desde la sección "Email" de la configuración. Usuario y Contrasena van en User Secrets (nunca en el repo).
public class OpcionesEmail
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Puerto { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string Remitente { get; set; } = string.Empty;   // si queda vacío se usa Usuario
    public string NombreRemitente { get; set; } = Marca.Nombre;
}

public record ContenidoEmail(string Asunto, string Html, string Texto);

// ArchivoLocal: ruta del archivo cuando el SMTP no está configurado y el email se guardó en disco
public record ResultadoEmail(bool Ok, string? Error = null, string? ArchivoLocal = null);

public interface IEmailService
{
    bool Configurado { get; }
    Task<ResultadoEmail> EnviarAsync(string destinatario, ContenidoEmail contenido, CancellationToken ct = default);
}

// Envía por SMTP con MailKit. Si cambiara el proveedor (por ejemplo, un servicio por API HTTP),
// solo se reemplaza esta clase: el resto del sistema usa IEmailService.
public class EmailService : IEmailService
{
    private readonly OpcionesEmail _opciones;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<OpcionesEmail> opciones, IWebHostEnvironment env, ILogger<EmailService> logger)
    {
        _opciones = opciones.Value;
        _env = env;
        _logger = logger;
    }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_opciones.Host) &&
        !string.IsNullOrWhiteSpace(_opciones.Usuario) &&
        !string.IsNullOrWhiteSpace(_opciones.Contrasena);

    public async Task<ResultadoEmail> EnviarAsync(string destinatario, ContenidoEmail contenido, CancellationToken ct = default)
    {
        if (!Configurado)
            return await GuardarEnArchivoAsync(destinatario, contenido);

        try
        {
            var remitente = string.IsNullOrWhiteSpace(_opciones.Remitente) ? _opciones.Usuario : _opciones.Remitente;

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_opciones.NombreRemitente, remitente));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = contenido.Asunto;
            mensaje.Body = new BodyBuilder { HtmlBody = contenido.Html, TextBody = contenido.Texto }.ToMessageBody();

            using var cliente = new SmtpClient { Timeout = 20000 };

            // 587 = STARTTLS (Gmail); 465 = SSL desde el inicio
            var seguridad = _opciones.Puerto == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await cliente.ConnectAsync(_opciones.Host, _opciones.Puerto, seguridad, ct);

            // Las contraseñas de aplicación de Google se muestran con espacios; se quitan por las dudas
            await cliente.AuthenticateAsync(_opciones.Usuario, _opciones.Contrasena.Replace(" ", ""), ct);
            await cliente.SendAsync(mensaje); // en MailKit 5 es un método de extensión del namespace MailKit
            await cliente.DisconnectAsync(true, ct);

            return new(true);
        }
        catch (Exception ex)
        {
            // El envío nunca rompe la operación que lo pidió: se registra el error y se informa
            _logger.LogError(ex, "No se pudo enviar el email \"{Asunto}\" a {Destinatario}", contenido.Asunto, destinatario);
            return new(false, ex.Message);
        }
    }

    // Sin SMTP configurado (por ejemplo, en desarrollo sin credenciales) el email se guarda como .html:
    // se abre con el navegador y los enlaces funcionan igual.
    private async Task<ResultadoEmail> GuardarEnArchivoAsync(string destinatario, ContenidoEmail contenido)
    {
        var carpeta = Path.Combine(_env.ContentRootPath, "emails-dev");
        Directory.CreateDirectory(carpeta);

        var asuntoSeguro = string.Concat(contenido.Asunto.Where(char.IsLetterOrDigit).Take(40));
        var ruta = Path.Combine(carpeta, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{asuntoSeguro}.html");

        await File.WriteAllTextAsync(ruta, $"<!-- Para: {destinatario} | Asunto: {contenido.Asunto} -->\n{contenido.Html}");
        _logger.LogWarning("SMTP sin configurar: el email para {Destinatario} se guardó en {Ruta}", destinatario, ruta);

        return new(true, null, ruta);
    }
}

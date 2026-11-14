namespace GymEcommerce.Services;

public record ResultadoImagen(string? Ruta, string? Error);

// Guarda y elimina imágenes de productos en wwwroot/uploads/productos.
public class ImagenService
{
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long TamanoMaximo = 2 * 1024 * 1024; // 2 MB
    private const string PrefijoRuta = "~/uploads/productos/";

    private readonly IWebHostEnvironment _env;

    public ImagenService(IWebHostEnvironment env) => _env = env;

    public async Task<ResultadoImagen> GuardarAsync(IFormFile archivo)
    {
        if (archivo.Length == 0)
            return new(null, "El archivo está vacío.");

        if (archivo.Length > TamanoMaximo)
            return new(null, "La imagen no puede superar los 2 MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            return new(null, "Formato no permitido. Usá JPG, PNG o WEBP.");

        // No alcanza con mirar la extensión: se verifican los primeros bytes del archivo.
        if (archivo.Length < 12)
            return new(null, "El archivo no es una imagen válida.");

        var cabecera = new byte[12];
        await using (var lectura = archivo.OpenReadStream())
        {
            await lectura.ReadExactlyAsync(cabecera, 0, cabecera.Length);
        }

        if (!EsImagen(cabecera))
            return new(null, "El archivo no es una imagen válida.");

        var carpeta = Path.Combine(_env.WebRootPath, "uploads", "productos");
        Directory.CreateDirectory(carpeta);

        // Nombre generado por nosotros: nunca se usa el nombre que manda el usuario.
        var nombre = $"{Guid.NewGuid():N}{extension}";
        await using (var destino = File.Create(Path.Combine(carpeta, nombre)))
        {
            await archivo.CopyToAsync(destino);
        }

        return new(PrefijoRuta + nombre, null);
    }

    public void Eliminar(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !ruta.StartsWith(PrefijoRuta))
            return;

        var nombre = Path.GetFileName(ruta); // evita rutas con ../
        var fisica = Path.Combine(_env.WebRootPath, "uploads", "productos", nombre);
        if (File.Exists(fisica))
            File.Delete(fisica);
    }

    private static bool EsImagen(byte[] b) =>
        (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) ||                                   // JPEG
        (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) ||                   // PNG
        (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&                    // WEBP: "RIFF"....
         b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50);                   //       "WEBP"
}

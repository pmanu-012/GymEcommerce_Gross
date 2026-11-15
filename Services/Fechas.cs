namespace GymEcommerce.Services;

// Las fechas se guardan en UTC; esto las convierte desde y hacia la hora de Argentina.
public static class Fechas
{
    private static readonly TimeZoneInfo ZonaArgentina = ObtenerZona();

    private static TimeZoneInfo ObtenerZona()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* se prueba el siguiente nombre */ }
        }
        return TimeZoneInfo.Utc;
    }

    // UTC (base de datos) → hora local (para mostrar)
    public static DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaArgentina);

    // Hora local (por ejemplo, un filtro "desde el 15/11") → UTC (para comparar en la base)
    public static DateTime AUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), ZonaArgentina);
}

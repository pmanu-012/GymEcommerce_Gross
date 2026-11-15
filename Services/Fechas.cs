namespace GymEcommerce.Services;

// Las fechas se guardan en UTC; esto las convierte a la hora de Argentina para mostrarlas.
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

    public static DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaArgentina);
}

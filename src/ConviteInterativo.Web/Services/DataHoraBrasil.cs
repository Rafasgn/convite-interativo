namespace ConviteInterativo.Web.Services;

// HU-07a — host de produção roda em UTC, mas o prazo de confirmação (HU-13) é
// derivado de Evento.DataHora, hora local de parede. Comparar contra UtcNow
// direto adianta o prazo em ~3h; este helper alinha os dois lados em
// horário de Brasília. Requer o pacote tzdata no container (ver Dockerfile).
public static class DataHoraBrasil
{
    private static readonly TimeZoneInfo FusoBrasil =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTime Agora =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasil);
}

namespace CarbonAwareComputing.ForecastUpdater.Function;

public class ApplicationSettings
{
    public string? ApiKeyPassword { get; init; }
    public string? MailFrom { get; init; }
    public string? ClientId { get; init; }
    public string? TenantId { get; init; }
    public string? ClientSecret { get; init; }
    public string? WriteHistoryFor { get; init; }

}
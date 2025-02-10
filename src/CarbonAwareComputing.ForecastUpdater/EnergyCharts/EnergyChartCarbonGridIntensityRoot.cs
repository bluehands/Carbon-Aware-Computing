using System.Text.Json.Serialization;

namespace CarbonAwareComputing.ForecastUpdater.EnergyCharts;

public class EnergyChartCarbonGridIntensityRoot
{
    [JsonPropertyName("unix_seconds")]
    public List<int> UnixSeconds { get; set; } = null!;

    [JsonPropertyName("co2eq")]
    public List<double?> Co2eq { get; set; } = null!;

    [JsonPropertyName("co2eq_forecast")]
    public List<double?> Co2eqForecast { get; set; } = null!;
}
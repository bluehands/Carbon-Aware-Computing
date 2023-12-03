using System.Text.Json.Serialization;

namespace CarbonAwareComputing.ForecastUpdater.EnergyCharts;

public record EnergyChartRoot(
    [property: JsonPropertyName("name")] Name Name,
    [property: JsonPropertyName("data")] IReadOnlyList<double?> Data,
    [property: JsonPropertyName("xAxisValues")] IReadOnlyList<long>? XAxisValues,
    [property: JsonPropertyName("xAxisFormat")] string XAxisFormat,
    [property: JsonPropertyName("date")] long? Date
);
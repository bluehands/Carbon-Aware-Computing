using System.Text.Json.Serialization;

namespace CarbonAwareComputing.ForecastUpdater
{
    public class EmissionsForecastJsonFile
    {
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;
        public List<EmissionsDataRaw> Emissions { get; set; } = new List<EmissionsDataRaw>();
    }
    public record EmissionsDataRaw
    {
        public DateTimeOffset Time { get; set; }
        public double Rating { get; set; }
        public TimeSpan Duration { get; set; }
    }
    public record Name(
        [property: JsonPropertyName("en")] string En,
        [property: JsonPropertyName("de")] string De,
        [property: JsonPropertyName("fr")] string Fr,
        [property: JsonPropertyName("it")] string It,
        [property: JsonPropertyName("es")] string Es
    );
    public record EnergyChartRoot(
        [property: JsonPropertyName("name")] Name Name,
        [property: JsonPropertyName("data")] IReadOnlyList<double?> Data,
        [property: JsonPropertyName("xAxisValues")] IReadOnlyList<long>? XAxisValues,
        [property: JsonPropertyName("xAxisFormat")] string XAxisFormat,
        [property: JsonPropertyName("date")] long? Date
    );


}
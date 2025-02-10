using System.Text.Json.Serialization;

namespace CarbonAwareComputing.ForecastUpdater.UKNationalGrid;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class UKNationalGridData
{
    [JsonPropertyName("from")]
    public DateTimeOffset From { get; set; }

    [JsonPropertyName("to")]
    public DateTimeOffset To { get; set; }

    [JsonPropertyName("intensity")]
    public Intensity Intensity { get; set; } = null!;

    //[JsonPropertyName("generationmix")]
    //public List<Generationmix> Generationmix { get; set; }
}

public class Generationmix
{
    [JsonPropertyName("fuel")]
    public string Fuel { get; set; } = null!;

    [JsonPropertyName("perc")]
    public double? Perc { get; set; }
}

public class Intensity
{
    [JsonPropertyName("forecast")]
    public int? Forecast { get; set; }

    [JsonPropertyName("index")]
    public string Index { get; set; } = null!;
}

public class UKNationalGridRoot
{
    [JsonPropertyName("data")]
    public UKNationalGridForecastData Data { get; set; } = null!;
}

public class UKNationalGridForecastData
{
    //[JsonPropertyName("regionid")]
    //public int? Regionid { get; set; }

    //[JsonPropertyName("dnoregion")]
    //public string Dnoregion { get; set; }

    //[JsonPropertyName("shortname")]
    //public string Shortname { get; set; }

    [JsonPropertyName("data")]
    public List<UKNationalGridData> Data { get; set; } = null!;
}


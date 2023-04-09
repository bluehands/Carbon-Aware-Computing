using System.Net.Http.Headers;
using CarbonAware.Model;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonAwareComputing.ExecutionForecast;

public class CarbonAwareDataProviderOpenData : CarbonAwareDataProviderCachedData
{
    protected override async Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        if (DateTimeOffset.Now < currentCachedData.LastUpdate.AddMinutes(5))
        {
            return currentCachedData;
        }

        var locationName = location.Name;
        var uri = new Uri($"https://carbonawarecomputing.blob.core.windows.net/forecasts/{locationName}.json");
        var httpClient = Services.GetService<HttpClient>()!;
        httpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
        var eTag = currentCachedData.Version;
        if (string.IsNullOrEmpty(eTag))
        {
            eTag = "\"*\"";
        }

        if (!eTag.StartsWith("\""))
        {
            eTag = "\"" + eTag + "\"";
        }
        httpClient.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(eTag));
        var response = await httpClient.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
        {
            return currentCachedData;
        }

        var eTagHeader = response.Headers.FirstOrDefault(h => h.Key.Equals("ETag", StringComparison.InvariantCultureIgnoreCase));
        eTag = eTagHeader.Value.FirstOrDefault();

        var json = await response.Content.ReadAsStringAsync();
        var jsonFile = System.Text.Json.JsonSerializer.Deserialize<EmissionsForecastJsonFile>(json)!;
        var emissionsData = jsonFile.Emissions.Select(e => new EmissionsData()
        {
            Duration = e.Duration,
            Rating = e.Rating,
            Location = locationName,
            Time = e.Time
        }).ToList();
        return new CachedData(emissionsData, DateTimeOffset.Now, eTag);
    }
}
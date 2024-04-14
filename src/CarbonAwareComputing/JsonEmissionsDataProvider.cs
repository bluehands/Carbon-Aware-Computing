using System.Collections.Concurrent;
using System.Net.Http.Headers;
using CarbonAware.DataSources.Memory;
using CarbonAware.Model;

namespace CarbonAwareComputing;

public interface ICachedEmissionsDataProvider : IEmissionsDataProvider
{
    Task<DataBoundary> GetDataBoundary(ComputingLocation computingLocation);
}

public abstract class JsonEmissionsDataProviderBase : ICachedEmissionsDataProvider
{
    private readonly ConcurrentDictionary<ComputingLocation, EmissionsForecastDataCache> m_EmissionsForecastData = new();

    public async Task<List<EmissionsData>> GetForecastData(Location location)
    {
        var computingLocation = new ComputingLocation(location.Name ?? "de");
        var emissionsForecastDataCache = GetEmissionsForecastDataCache(computingLocation);
        return await emissionsForecastDataCache.GetForecastData();
    }

    public async Task<DataBoundary> GetDataBoundary(ComputingLocation computingLocation)
    {
        var emissionsForecastDataCache = GetEmissionsForecastDataCache(computingLocation);
        return await emissionsForecastDataCache.GetDataBoundary();
    }
    private EmissionsForecastDataCache GetEmissionsForecastDataCache(ComputingLocation computingLocation)
    {
        if (m_EmissionsForecastData.ContainsKey(computingLocation))
        {
            return m_EmissionsForecastData[computingLocation];
        }
        var cache = new EmissionsForecastDataCache(async (c, l) => await FillEmissionsDataCache(l, c), computingLocation);
        m_EmissionsForecastData[computingLocation] = cache;
        return cache;
    }

    protected abstract Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData);
}
public class JsonEmissionsDataProvider : JsonEmissionsDataProviderBase, IDisposable
{
    private readonly HttpClient m_HttpClient;
    private readonly bool m_ShouldDisposeHtmlClient;

    public JsonEmissionsDataProvider()
    {
        m_HttpClient = new HttpClient();
        m_ShouldDisposeHtmlClient = true;
    }
    public JsonEmissionsDataProvider(HttpClient httpClient)
    {
        m_HttpClient = httpClient;
        m_ShouldDisposeHtmlClient = false;
    }
    public void Dispose()
    {
        if (m_ShouldDisposeHtmlClient)
        {
            m_HttpClient.Dispose();
        }
    }
    protected override async Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        if (DateTimeOffset.Now < currentCachedData.LastUpdate.AddMinutes(5))
        {
            return currentCachedData;
        }

        var locationName = location.Name;
        var uri = new Uri($"https://carbonawarecomputing.blob.core.windows.net/forecasts/{locationName}.json");
        m_HttpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
        var eTag = currentCachedData.Version;
        if (string.IsNullOrEmpty(eTag))
        {
            eTag = "\"*\"";
        }

        if (!eTag.StartsWith("\""))
        {
            eTag = "\"" + eTag + "\"";
        }
        m_HttpClient.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(eTag));
        var response = await m_HttpClient.GetAsync(uri);
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
public class OfflineJsonEmissionsDataProvider : JsonEmissionsDataProviderBase
{
    private readonly Func<ComputingLocation, Task<List<EmissionsData>>> m_GetEmissionData;

    public OfflineJsonEmissionsDataProvider()
    {
        m_GetEmissionData = l => Task.FromResult(new List<EmissionsData>());
    }
    public OfflineJsonEmissionsDataProvider(Func<ComputingLocation, Task<List<EmissionsData>>> getEmissionData)
    {
        m_GetEmissionData = getEmissionData;
    }
    protected override async Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        var emissionsData = await m_GetEmissionData.Invoke(location);
        return new CachedData(emissionsData, DateTimeOffset.Now, currentCachedData.Version);
    }
}

public class EmissionsForecastJsonFile
{
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;
    public List<EmissionsDataRaw> Emissions { get; set; } = new();
}
public record EmissionsDataRaw
{
    public DateTimeOffset Time { get; set; }
    public double Rating { get; set; }
    public TimeSpan Duration { get; set; }
}
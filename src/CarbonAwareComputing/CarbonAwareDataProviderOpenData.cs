using System.Collections.Concurrent;
using System.Net.Http.Headers;
using CarbonAware.DataSources.WattTime;
using CarbonAware.DataSources.WattTime.Client;
using CarbonAware.DataSources.WattTime.Configuration;
using CarbonAware.DataSources.WattTime.Model;
using CarbonAware.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// ReSharper disable InconsistentNaming

namespace CarbonAwareComputing;

public class CarbonAwareDataProviderOpenData : CarbonAwareDataProviderCachedData
{
    static readonly HttpClient httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private readonly string m_ForecastDataEndpointTemplate;

    public CarbonAwareDataProviderOpenData()
    {
        m_ForecastDataEndpointTemplate = "https://carbonawarecomputing.blob.core.windows.net/forecasts/{0}.json";
    }
    public CarbonAwareDataProviderOpenData(string forecastDataEndpointTemplate) : base()
    {
        m_ForecastDataEndpointTemplate = forecastDataEndpointTemplate ?? throw new ArgumentNullException(nameof(forecastDataEndpointTemplate));
    }
    protected override async Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        if (DateTimeOffset.Now < currentCachedData.LastUpdate.AddMinutes(5))
        {
            return currentCachedData;
        }

        var locationName = location.Name;
        var uri = new Uri(string.Format(m_ForecastDataEndpointTemplate, locationName));
        httpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
        var eTag = currentCachedData.Version;
        if (string.IsNullOrEmpty(eTag))
        {
            eTag = "\"*\"";
        }

        if (!eTag!.StartsWith("\""))
        {
            eTag = "\"" + eTag + "\"";
        }
        httpClient.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(eTag));
        var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return currentCachedData;
        }

        var eTagHeader = response.Headers.FirstOrDefault(h => h.Key.Equals("ETag", StringComparison.InvariantCultureIgnoreCase));
        eTag = eTagHeader.Value?.FirstOrDefault();

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

public class CarbonAwareDataProviderWattTime : CarbonAwareDataProviderCachedData
{
    private readonly IWattTimeClient client;

    private class DefaultHttpClientFactory : IHttpClientFactory
    {
        private static readonly ConcurrentDictionary<string, HttpClient> httpClients = new ConcurrentDictionary<string, HttpClient>();

        public HttpClient CreateClient(string name)
        {
            if (httpClients.TryGetValue(name, out var httpClient))
            {
                return httpClient;
            }
            httpClient = new HttpClient();
            httpClients[name] = httpClient;
            return httpClient;
        }
    }
    private class OptionsMonitor<T> : IOptionsMonitor<T>
        where T : class, new()
    {
        public OptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T Get(string name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            throw new NotImplementedException();
        }

        public T CurrentValue { get; }
    }

    public CarbonAwareDataProviderWattTime(string userName, string password)
    {
        var configurationMonitor = new OptionsMonitor<WattTimeClientConfiguration>(new WattTimeClientConfiguration()
        {
            Username = userName,
            Password = password
        });
        client = new WattTimeClient(
            new DefaultHttpClientFactory(),
            configurationMonitor,
            NullLogger<WattTimeClient>.Instance,
            new MemoryCache(Options.Create(new MemoryCacheOptions()
            {
                SizeLimit = 30
            })));
    }

    protected override async Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        if (DateTimeOffset.Now < currentCachedData.LastUpdate.AddHours(1))
        {
            return currentCachedData;
        }

        var ba = location.Name;
        var data = await client.GetCurrentForecastAsync(ba).ConfigureAwait(false);

        var forecastData = data.ForecastData.ToList();
        var defaultDuration = GetDurationFromGridEmissionDataPoints(data.ForecastData);

        var emissionsData = forecastData.Select(d =>
            new EmissionsData()
            {
                Duration = defaultDuration,
                Location = ba,
                Time = d.PointTime,
                Rating = ConvertMoerToGramsPerKilowattHour(d.Value)
            }
        ).ToList();

        return new CachedData(emissionsData, DateTimeOffset.Now, Guid.NewGuid().ToString());
    }
    internal double ConvertMoerToGramsPerKilowattHour(double value)
    {
        const double MWH_TO_KWH_CONVERSION_FACTOR = 1000.0;
        const double LBS_TO_GRAMS_CONVERSION_FACTOR = 453.59237;

        return value * LBS_TO_GRAMS_CONVERSION_FACTOR / MWH_TO_KWH_CONVERSION_FACTOR;
    }
    private TimeSpan GetDurationFromGridEmissionDataPoints(IEnumerable<GridEmissionDataPoint> gridEmissionDataPoints)
    {
        var firstPoint = gridEmissionDataPoints.FirstOrDefault();
        var secondPoint = gridEmissionDataPoints.Skip(1)?.FirstOrDefault();

        var first = firstPoint ?? throw new WattTimeClientException("Too few data points returned");
        var second = secondPoint ?? throw new WattTimeClientException("Too few data points returned");

        // Handle chronological and reverse-chronological data by using `.Duration()` to get
        // the absolute value of the TimeSpan between the two points.
        return first.PointTime.Subtract(second.PointTime).Duration();
    }
}
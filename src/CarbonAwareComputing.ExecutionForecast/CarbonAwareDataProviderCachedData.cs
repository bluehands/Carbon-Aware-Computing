using System.Collections.Concurrent;
using CarbonAware.DataSources.Memory;
using CarbonAware.Model;
using GSF.CarbonAware.Configuration;
using GSF.CarbonAware.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarbonAwareComputing.ExecutionForecast;

public abstract class CarbonAwareDataProviderCachedData : CarbonAwareDataProvider
{
    private class EmissionsDataProvider : IEmissionsDataProvider
    {
        private readonly Func<Location, Task<List<EmissionsData>>> m_GetForecastData;

        public EmissionsDataProvider(Func<Location, Task<List<EmissionsData>>> getForecastData)
        {
            m_GetForecastData = getForecastData;
        }
        public Task<List<EmissionsData>> GetForecastData(Location location)
        {
            return m_GetForecastData(location);
        }
    }

    protected readonly ServiceProvider Services;
    private readonly ConcurrentDictionary<ComputingLocation, EmissionsForecastDataCache> m_EmissionsForecastData = new();

    protected CarbonAwareDataProviderCachedData()
    {
        Services = InitializeSdk();
    }

    private ServiceProvider InitializeSdk()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"LocationDataSourcesConfiguration:LocationSourceFiles:DataFileLocation", ""},
            {"LocationDataSourcesConfiguration:LocationSourceFiles:Prefix", "az"},
            {"LocationDataSourcesConfiguration:LocationSourceFiles:Delimiter", "-"},
            {"DataSources:EmissionsDataSource", ""},
            {"DataSources:ForecastDataSource", "Memory"},
            {"DataSources:Configurations:Memory:Type", "MEMORY"},
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();


        var services = new ServiceCollection()
            .AddLogging(loggerBuilder =>
            {
                loggerBuilder.ClearProviders();
                loggerBuilder.AddConsole();
            })
            .AddSingleton<HttpClient>()
            .AddSingleton<IEmissionsDataProvider>(new EmissionsDataProvider(GetForecastData))
            .AddForecastServices(configuration)
            .BuildServiceProvider();
        return services;
    }

    public override async Task<ExecutionTime> CalculateBestExecutionTime(ComputingLocation location, DateTimeOffset earliestExecutionTime, DateTimeOffset latestExecutionTime, TimeSpan estimatedJobDuration)
    {
        var handler = Services.GetService<IForecastHandler>();
        if (handler == null)
        {
            return ExecutionTime.NoForecast;
        }

        var adjustedForecastBoundary = await TryAdjustForecastBoundary(location, earliestExecutionTime, latestExecutionTime - estimatedJobDuration);
        if (!adjustedForecastBoundary.ForecastIsAvailable)
        {
            return ExecutionTime.NoForecast;
        }

        var lastStartTime = adjustedForecastBoundary.LastStartTime;
        var earliestStartTime = adjustedForecastBoundary.EarliestStartTime;
        var forecast = await handler.GetCurrentForecastAsync(new[] { location.Name }, earliestStartTime, lastStartTime, Convert.ToInt32(estimatedJobDuration.TotalMinutes));
        var best = forecast.First().OptimalDataPoints.FirstOrDefault();
        if (best == null)
        {
            return ExecutionTime.NoForecast;
        }

        return ExecutionTime.BestExecutionTime(best.Time, best.Duration, best.Rating);
    }

    public Task<List<EmissionsData>> GetForecastData(ComputingLocation location)
    {
        return GetForecastData(new Location() { Name = location.Name });
    }

    private async Task<List<EmissionsData>> GetForecastData(Location location)
    {
        var computingLocation = new ComputingLocation(location.Name ?? "de");
        var emissionsForecastDataCache = GetEmissionsForecastDataCache(computingLocation);
        return await emissionsForecastDataCache.GetForecastData();
    }
    private async Task<(bool ForecastIsAvailable, DateTimeOffset EarliestStartTime, DateTimeOffset LastStartTime)> TryAdjustForecastBoundary(ComputingLocation location, DateTimeOffset earliestStartTime, DateTimeOffset lastStartTime)
    {
        var provider = Services.GetService<IEmissionsDataProvider>();
        if (provider == null)
        {
            return (false, DateTimeOffset.Now, DateTimeOffset.Now);
        }
        var boundary = await GetDataBoundary(location);
        if (lastStartTime > boundary.EndTime)
        {
            lastStartTime = boundary.EndTime;
        }

        if (earliestStartTime < boundary.StartTime)
        {
            earliestStartTime = boundary.StartTime;
        }

        if (earliestStartTime > lastStartTime)
        {
            return (false, earliestStartTime, lastStartTime);
        }
        return (true, earliestStartTime, lastStartTime);
    }
    private async Task<DataBoundary> GetDataBoundary(ComputingLocation computingLocation)
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
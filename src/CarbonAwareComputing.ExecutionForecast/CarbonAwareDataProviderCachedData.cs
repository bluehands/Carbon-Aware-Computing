using System.Collections.Concurrent;
using CarbonAware.DataSources.Memory;
using CarbonAware.Model;
using GSF.CarbonAware.Handlers;
using Microsoft.Extensions.Logging.Abstractions;

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

    protected IForecastHandler ForecastHandler;
    private readonly EmissionsDataProvider m_DataProvider;
    private readonly ConcurrentDictionary<ComputingLocation, EmissionsForecastDataCache> m_EmissionsForecastData = new();

    protected CarbonAwareDataProviderCachedData()
    {
        m_DataProvider = new EmissionsDataProvider(GetForecastData);
        var memoryDataSource = new MemoryDataSource(new NullLogger<MemoryDataSource>(), m_DataProvider);
        ForecastHandler = new ForecastHandler(new NullLogger<ForecastHandler>(), memoryDataSource);
    }

    public override async Task<ExecutionTime> CalculateBestExecutionTime(ComputingLocation location, DateTimeOffset earliestExecutionTime, DateTimeOffset latestExecutionTime, TimeSpan estimatedJobDuration)
    {
        var adjustedForecastBoundary = await TryAdjustForecastBoundary(location, earliestExecutionTime, latestExecutionTime - estimatedJobDuration);
        if (!adjustedForecastBoundary.ForecastIsAvailable)
        {
            return ExecutionTime.NoForecast;
        }

        var lastStartTime = adjustedForecastBoundary.LastStartTime;
        var earliestStartTime = adjustedForecastBoundary.EarliestStartTime;
        var forecast = await ForecastHandler.GetCurrentForecastAsync(new[] { location.Name }, earliestStartTime, lastStartTime, Convert.ToInt32(estimatedJobDuration.TotalMinutes));
        var best = forecast.First().OptimalDataPoints.FirstOrDefault();
        if (best == null)
        {
            return ExecutionTime.NoForecast;
        }

        return ExecutionTime.BestExecutionTime(best.Time, best.Duration, best.Rating);
    }

    public override async Task<GridCarbonIntensity> GetCarbonIntensity(ComputingLocation location, DateTimeOffset now)
    {
        var adjustedForecastBoundary = await TryAdjustForecastBoundary(location, now, now);
        if (!adjustedForecastBoundary.ForecastIsAvailable)
        {
            return GridCarbonIntensity.NoData();
        }

        var forecastData = await m_DataProvider.GetForecastData(new Location() { Name = location.Name });


        for (int i = forecastData.Count - 1; i >= 0; i--)
        {
            var f = forecastData[i];
            if (now >= f.Time)
            {
                return GridCarbonIntensity.Intensity(f.Rating);
            }
        }
        return GridCarbonIntensity.NoData();

        var emissionsData = forecastData.FirstOrDefault(e => e.Time >= now);
        if (emissionsData is null)
        {
            return GridCarbonIntensity.NoData();
        }
        return GridCarbonIntensity.Intensity(emissionsData.Rating);

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
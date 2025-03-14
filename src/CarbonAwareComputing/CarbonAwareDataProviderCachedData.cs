using System.Collections.Concurrent;
using CarbonAware.DataSources.Memory;
using CarbonAware.Model;
using GSF.CarbonAware.Handlers;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarbonAwareComputing;

public abstract class CarbonAwareDataProviderCachedData : CarbonAwareDataProvider
{
    private class EmissionsDataProvider(Func<Location, Task<List<EmissionsData>>> getForecastData)
        : IEmissionsDataProvider
    {
        public Task<List<EmissionsData>> GetForecastData(Location location) => getForecastData(location);
    }

    protected readonly IForecastHandler ForecastHandler;
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
        var adjustedForecastBoundary = await TryAdjustForecastBoundary(location, earliestExecutionTime, latestExecutionTime - estimatedJobDuration).ConfigureAwait(false);
        if (!adjustedForecastBoundary.ForecastIsAvailable)
        {
            return ExecutionTime.NoForecast;
        }

        var lastStartTime = adjustedForecastBoundary.LastStartTime;
        var earliestStartTime = adjustedForecastBoundary.EarliestStartTime;
        var forecast = await ForecastHandler.GetCurrentForecastAsync([location.Name], earliestStartTime, lastStartTime, Convert.ToInt32(estimatedJobDuration.TotalMinutes)).ConfigureAwait(false);
        var forecastForLocation = forecast[0];
        var best = forecastForLocation.OptimalDataPoints.FirstOrDefault();
        if (best == null)
        {
            return ExecutionTime.NoForecast;
        }

        var bestExecutionTime = best.Time;
        if (best.Time < earliestExecutionTime)
        {
            bestExecutionTime = earliestExecutionTime;
        }

        return ExecutionTime.BestExecutionTime(bestExecutionTime, best.Duration, best.Rating, forecastForLocation.EmissionsDataPoints.First().Rating);
    }

    public override async Task<GridCarbonIntensity> GetCarbonIntensity(ComputingLocation location, DateTimeOffset now)
    {
        var adjustedForecastBoundary = await TryAdjustForecastBoundary(location, now, now).ConfigureAwait(false);
        if (!adjustedForecastBoundary.ForecastIsAvailable)
        {
            return GridCarbonIntensity.NoData;
        }

        var forecastData = await m_DataProvider.GetForecastData(new Location() { Name = location.Name }).ConfigureAwait(false);
        for (int i = forecastData.Count - 1; i >= 0; i--)
        {
            var f = forecastData[i];
            if (now >= f.Time)
            {
                return GridCarbonIntensity.EmissionData(f.Location, f.Time, f.Rating);
            }
        }
        return GridCarbonIntensity.NoData;

    }

    public Task<List<EmissionsData>> GetForecastData(ComputingLocation location)
    {
        return GetForecastData(new Location() { Name = location.Name });
    }

    private async Task<List<EmissionsData>> GetForecastData(Location location)
    {
        var computingLocation = new ComputingLocation(location.Name ?? "de");
        var emissionsForecastDataCache = GetEmissionsForecastDataCache(computingLocation);
        return await emissionsForecastDataCache.GetForecastData().ConfigureAwait(false);
    }
    private async Task<(bool ForecastIsAvailable, DateTimeOffset EarliestStartTime, DateTimeOffset LastStartTime)> TryAdjustForecastBoundary(ComputingLocation location, DateTimeOffset earliestStartTime, DateTimeOffset lastStartTime)
    {
        var boundary = await GetDataBoundary(location).ConfigureAwait(false);
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
        return await emissionsForecastDataCache.GetDataBoundary().ConfigureAwait(false);
    }
    private EmissionsForecastDataCache GetEmissionsForecastDataCache(ComputingLocation computingLocation)
    {
        if (m_EmissionsForecastData.TryGetValue(computingLocation, out var dataCache))
        {
            return dataCache;
        }
        var cache = new EmissionsForecastDataCache(async (c, l) => await FillEmissionsDataCache(l, c).ConfigureAwait(false), computingLocation);
        m_EmissionsForecastData[computingLocation] = cache;
        return cache;
    }
    protected abstract Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData);
}
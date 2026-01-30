using CarbonAware.Interfaces;
using CarbonAware.Model;
using GSF.CarbonAware.Handlers;
using Microsoft.Extensions.Logging;

namespace CarbonAware.DataSources.Memory;

/// <summary>
/// Represents a JSON data source.
/// </summary>
internal class MemoryDataSource : IForecastDataSource
{
    public string Name => "MemoryDataSource";

    public string Description => "Plugin to read data from memory for Carbon Aware SDK";

    public string Author => "bluehands";

    public string Version => "0.0.1";

    public double MinSamplingWindow => 1440;  // 24 hrs


    private readonly ILogger<MemoryDataSource> _logger;
    private readonly IEmissionsDataProvider _emissionsDataProvider;

    /// <summary>
    /// Creates a new instance of the <see cref="MemoryDataSource"/> class.
    /// </summary>
    /// <param name="logger">The logger for the datasource</param>
    public MemoryDataSource(ILogger<MemoryDataSource> logger, IEmissionsDataProvider emissionsDataProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emissionsDataProvider = emissionsDataProvider;
    }

    public async Task<EmissionsForecast> GetCurrentCarbonIntensityForecastAsync(Location location)
    {
        return await GetCarbonIntensityForecastAsync(location, DateTimeOffset.Now).ConfigureAwait(false);
    }

    public async Task<EmissionsForecast> GetCarbonIntensityForecastAsync(Location location, DateTimeOffset requestedAt)
    {
        var emissionsData = await _emissionsDataProvider.GetForecastData(location).ConfigureAwait(false);
        if (!emissionsData.Any())
        {
            _logger.LogDebug("Emission data list is empty");
            return new EmissionsForecast();
        }
        _logger.LogDebug($"Total emission records retrieved {emissionsData.Count}");


        return new EmissionsForecast()
        {
            Location = location,
            GeneratedAt = DateTimeOffset.Now,
            ForecastData = emissionsData,
            OptimalDataPoints = emissionsData.GetOptimalEmissions()
        };
    }

}

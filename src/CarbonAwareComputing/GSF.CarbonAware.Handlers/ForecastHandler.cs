using CarbonAware;
using CarbonAware.Exceptions;
using CarbonAware.Extensions;
using CarbonAware.Interfaces;
using CarbonAware.Model;
using Microsoft.Extensions.Logging;

using EmissionsForecast = GSF.CarbonAware.Models.EmissionsForecast;

namespace GSF.CarbonAware.Handlers;

internal sealed class ForecastHandler : IForecastHandler
{
    private readonly IForecastDataSource _forecastDataSource;
    private readonly ILogger<ForecastHandler> _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="ForecastHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger for the handler</param>
    /// <param name="datasource">An <see cref="IForecastDataSource"> datasource.</param>
    public ForecastHandler(ILogger<ForecastHandler> logger, IForecastDataSource dataSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _forecastDataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmissionsForecast>> GetCurrentForecastAsync(string[] locations, DateTimeOffset? start = null, DateTimeOffset? end = null, int? duration = null)
    {
        try
        {

            var forecasts = new List<EmissionsForecast>();
            foreach (var location in locations)
            {
                var forecast = await _forecastDataSource.GetCurrentCarbonIntensityForecastAsync(new Location() { Name = location });
                var emissionsForecast = ProcessAndValidateForecast(forecast, duration.HasValue ? TimeSpan.FromMinutes(duration.Value) : TimeSpan.Zero, start, end);
                forecasts.Add(emissionsForecast);
            }
            return forecasts;
        }
        catch (CarbonAwareException ex)
        {
            throw new Exceptions.CarbonAwareException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<EmissionsForecast> GetForecastByDateAsync(string location, DateTimeOffset? start = null, DateTimeOffset? end = null, DateTimeOffset? requestedAt = null, int? duration = null)
    {
        if (!requestedAt.HasValue)
        {
            throw new ArgumentException("Argument is not set", nameof(requestedAt));
        }

        try
        {
            var forecast = await _forecastDataSource.GetCarbonIntensityForecastAsync(new Location() { Name = location }, requestedAt.Value);
            var emissionsForecast = ProcessAndValidateForecast(forecast, duration.HasValue ? TimeSpan.FromMinutes(duration.Value) : TimeSpan.Zero, start, end);
            return emissionsForecast;
        }
        catch (CarbonAwareException ex)
        {
            throw new Exceptions.CarbonAwareException(ex.Message, ex);
        }
    }

    private static EmissionsForecast ProcessAndValidateForecast(global::CarbonAware.Model.EmissionsForecast forecast, TimeSpan duration, DateTimeOffset? startAt, DateTimeOffset? endAt)
    {
        var windowSize = duration;
        var firstDataPoint = forecast.ForecastData.First();
        var lastDataPoint = forecast.ForecastData.Last();
        var dataStartAt = startAt ?? firstDataPoint.Time;
        var dataEndAt = endAt ?? lastDataPoint.Time + lastDataPoint.Duration;
        forecast.Validate(dataStartAt, dataEndAt);
        forecast.ForecastData = IntervalHelper.FilterByDuration(forecast.ForecastData, dataStartAt, dataEndAt);
        forecast.ForecastData = forecast.ForecastData.RollingAverage(windowSize, dataStartAt, dataEndAt);
        forecast.OptimalDataPoints = CarbonAwareOptimalEmission.GetOptimalEmissions(forecast.ForecastData.ToArray());
        return forecast;
    }
}
using CarbonAware.Model;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater.UKNationalGrid;

public static class UKNationalGridTransform
{
    public static Result<EmissionsForecast> ImportForecast(List<UKNationalGridData> roots, Option<UKRegions> region)
    {
        try
        {
            var location = region.IsNone() ? "uk" : region.GetValueOrThrow().ToString();
            var duration = CalculateDuration(roots);
            var emissionsForecast = new EmissionsForecast
            {
                GeneratedAt = DateTimeOffset.Now,
                Location = new Location { Name = location }
            };
            var forecastData = new List<EmissionsData>();
            foreach (var root in roots)
            {
                forecastData.Add(new EmissionsData
                {
                    Location = location,
                    Duration = duration,
                    Time = root.From,
                    Rating = Convert.ToInt32(root.Intensity.Forecast)
                });
            }
            emissionsForecast.ForecastData = forecastData;
            return emissionsForecast;
        }
        catch (Exception ex)
        {
            return Result.Error<EmissionsForecast>(ex.Message);
        }
    }

    private static TimeSpan CalculateDuration(List<UKNationalGridData> list)
    {
        if (list.Count == 0)
        {
            return TimeSpan.Zero;
        }
        var duration = list[0].To - list[0].From;
        return duration;
    }
}
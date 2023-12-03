using CarbonAware.Model;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater.EnergyCharts;

public static class Transform
{
    public static Result<string> Serialize(EmissionsForecast forecast)
    {
        try
        {
            var jsonFile = new EmissionsForecastJsonFile()
            {
                GeneratedAt = forecast.GeneratedAt,
                Emissions = forecast.ForecastData.Select(d => new EmissionsDataRaw { Time = d.Time, Rating = d.Rating, Duration = d.Duration }).ToList()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(jsonFile);
            return json;
        }
        catch (Exception ex)
        {
            return Result.Error<string>(ex.Message);
        }
    }
}
public static class EnergyChartsTransform
{
    public static Result<EmissionsForecast> ImportForecast(List<EnergyChartRoot> roots, string country)
    {
        try
        {
            var location = country;
            var generatedAt = DateTimeOffset.Now;
            var result = CreateTimeAxis(roots, country);
            return result.Bind<List<EmissionsData>>(
                forecast =>
                {
                    foreach (var root in roots)
                    {
                        if (root.Date.HasValue)
                        {
                            generatedAt = DateTimeOffset.FromUnixTimeMilliseconds(root.Date.Value);
                        }

                        var values = root.Data.ToArray();

                        for (int i = 0; i < values.Length; i++)
                        {
                            var value = values[i];
                            if (value != null)
                            {
                                forecast[i] = forecast[i] with
                                {
                                    Rating = 100 - value.Value
                                };
                            }
                        }
                    }
                    return forecast.Values.ToList();
                }).Bind<EmissionsForecast>(
                emissions => new EmissionsForecast
                {
                    GeneratedAt = generatedAt,
                    Location = new Location { Name = location },
                    ForecastData = emissions
                }
            );
        }
        catch (Exception ex)
        {
            return Result.Error<EmissionsForecast>(ex.Message);
        }
    }

    private static Result<Dictionary<int, EmissionsData>> CreateTimeAxis(List<EnergyChartRoot> roots, string country)
    {
        var timeAxis = new Dictionary<int, EmissionsData>();
        var root = roots.FirstOrDefault(r => r.XAxisValues != null);
        if (root == null)
        {
            return Result.Error<Dictionary<int, EmissionsData>>($"No time axis in forecast available for {country}");
        }
        var axis = root.XAxisValues!.Select(x => DateTimeOffset.FromUnixTimeMilliseconds(x)).ToArray();
        if (axis.Length <= 2)
        {
            return Result.Error<Dictionary<int, EmissionsData>>($"not enough data in forecast available for {country}");
        }
        var duration = axis[1] - axis[0];

        for (int i = 0; i < axis.Length; i++)
        {
            var time = axis[i];

            timeAxis[i] = new EmissionsData
            {
                Location = country,
                Time = time,
                Duration = duration,
                Rating = 0
            };
        }

        return timeAxis;
    }
}
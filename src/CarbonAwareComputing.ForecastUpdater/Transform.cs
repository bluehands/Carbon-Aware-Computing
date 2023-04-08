using System.Reflection.Metadata.Ecma335;
using CarbonAware.Model;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater;

public static class Transform
{
    public static Result<EmissionsForecast> ImportForecast(EnergyChartRoot root, string country)
    {
        try
        {
            var location = country;
            var values = root.Data.ToArray();
            var axis = root.XAxisValues.Select(x => DateTimeOffset.FromUnixTimeMilliseconds(x)).ToArray();
            if (axis.Length <= 2)
            {
                return Result.Error<EmissionsForecast>("not enough data in forecast available");
            }
            var duration = axis[1] - axis[0];
            var forecast = new List<EmissionsData>();
            for (int i = 0; i < axis.Length; i++)
            {
                var time = axis[i];
                var value = values[i];
                if (value != null)
                {
                    forecast.Add(new EmissionsData()
                    {
                        Location = country,
                        Time = time,
                        Duration = duration,
                        Rating = 100 - value.Value
                    });
                }
            }
            return new EmissionsForecast
            {
                GeneratedAt = root.Date.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(root.Date.Value) : DateTimeOffset.Now,
                Location = new Location { Name = location },
                ForecastData = forecast
            };
        }
        catch (Exception ex)
        {
            return Result.Error<EmissionsForecast>(ex.Message);
        }
    }

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


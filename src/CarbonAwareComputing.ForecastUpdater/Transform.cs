using System.ComponentModel.DataAnnotations;
using CarbonAware.Model;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater;

public static class Transform
{
    public static Result<(string sdk, string minimized)> Serialize(EmissionsForecast forecast)
    {
        try
        {
            var jsonSdk = SerializeCarbonAwareSdk(forecast);
            var jsonMinimized = SerializeMinimized(forecast);
            return (jsonSdk,jsonMinimized);
        }
        catch (Exception ex)
        {
            return Result.Error<(string sdk, string minimized)>(ex.Message);
        }
    }

    private static string SerializeMinimized(EmissionsForecast forecast)
    {
        var interval = forecast.ForecastData.FirstOrDefault()?.Duration ?? TimeSpan.FromMinutes(15);
        var now = DateTimeOffset.Now - interval;
        var now24 = now.AddDays(1) + interval;
        var emissionData = forecast.ForecastData.Where(d => d.Time >= now && d.Time <= now24 && d.Rating > 0).ToList();


        var first = emissionData.FirstOrDefault();
        var start = first != null ? first.Time.ToUnixTimeMilliseconds() : now.ToUnixTimeMilliseconds();
        var jsonFile = new EmissionsForecastMinimizedJsonFile()
        {
            Start = start,
            Interval = (int)interval.TotalMilliseconds,
            Ratings = emissionData.Select(d => d.Rating).ToList()
        };
        var json = System.Text.Json.JsonSerializer.Serialize(jsonFile);
        return json;
    }

    private static string SerializeCarbonAwareSdk(EmissionsForecast forecast)
    {
        var jsonFile = new EmissionsForecastJsonFile()
        {
            GeneratedAt = forecast.GeneratedAt,
            Emissions = forecast.ForecastData.Select(d => new EmissionsDataRaw { Time = d.Time, Rating = d.Rating, Duration = d.Duration }).ToList()
        };
        var json = System.Text.Json.JsonSerializer.Serialize(jsonFile);
        return json;
    }
}
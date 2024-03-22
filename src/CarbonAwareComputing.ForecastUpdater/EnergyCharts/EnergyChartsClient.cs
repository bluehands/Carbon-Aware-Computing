﻿using System.Text.Json;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater.EnergyCharts;

public class EnergyChartsClient
{
    private readonly Func<Uri, Task<Result<string>>> m_GetContent;
    private readonly string m_BaseUri = "https://api.energy-charts.info/co2eq?country={0}";
    public EnergyChartsClient(Func<Uri, Task<Result<string>>> getContent)
    {
        m_GetContent = getContent;
    }
    
    public async Task<Result<EnergyChartCarbonGridIntensityRoot>> GetCarbonGridIntensityForecastAsync(string country)
    {
        try
        {
            var uri = new Uri(string.Format(m_BaseUri, country));
            return await m_GetContent.Invoke(uri).Bind(
                json =>
                {
                    var roots = JsonSerializer.Deserialize<EnergyChartCarbonGridIntensityRoot>(json);
                    if (roots == null)
                    {
                        return Result.Error<EnergyChartCarbonGridIntensityRoot>("Invalid json format. Could not deserialize");
                    }
                    return roots;
                }
            );
        }
        catch (Exception ex)
        {
            return Result.Error<EnergyChartCarbonGridIntensityRoot>(ex.Message);
        }
    }
}

using System.Text.Json;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater;

public class EnergyChartsClient
{
    private readonly Func<Uri, Task<Result<string>>> m_GetContent;
    private readonly string m_BaseUri = "https://api.energy-charts.info/ren_share?country={0}";
    public EnergyChartsClient(Func<Uri, Task<Result<string>>> getContent)
    {
        m_GetContent = getContent;
    }

    public async Task<Result<List<EnergyChartRoot>>> GetForecastAsync(string country)
    {
        try
        {
            var uri = new Uri(string.Format(m_BaseUri, country));
            return await m_GetContent.Invoke(uri).Bind(
                json =>
                {
                    var roots = JsonSerializer.Deserialize<List<EnergyChartRoot>>(json);
                    if (roots == null)
                    {
                        return Result.Error<List<EnergyChartRoot>>("Invalid json format. Could not deserialize");
                    }

                    if (roots.Count == 0)
                    {
                        return Result.Error<List<EnergyChartRoot>>("No data in json available");
                    }
                    return roots;
                }
                );
        }
        catch (Exception ex)
        {
            return Result.Error<List<EnergyChartRoot>>(ex.Message);
        }
    }
}
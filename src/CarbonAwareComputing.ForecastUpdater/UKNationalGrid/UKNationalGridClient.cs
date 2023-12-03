using System.Text.Json;
using CarbonAwareComputing.ForecastUpdater.EnergyCharts;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater.UKNationalGrid;

public class UKNationalGridClient
{
    private readonly Func<Uri, Task<Result<string>>> m_GetContent;
    private readonly string m_UKBaseUri = "https://api.carbonintensity.org.uk/intensity/{0}/fw48h";
    private readonly string m_RegionBaseUri = "https://api.carbonintensity.org.uk/regional/intensity/{0}/fw48h/regionid/{1}";
    public UKNationalGridClient(Func<Uri, Task<Result<string>>> getContent)
    {
        m_GetContent = getContent;
    }

    public async Task<Result<List<UKNationalGridData>>> GetForecastAsync()
    {
        try
        {
            var uri = new Uri(string.Format(m_UKBaseUri, DateTimeOffset.UtcNow.AddHours(-1).ToString("O")));
            return await m_GetContent.Invoke(uri).Bind(
                json =>
                {
                    var roots = JsonSerializer.Deserialize<UKNationalGridForecastData>(json);
                    if (roots == null)
                    {
                        return Result.Error<List<UKNationalGridData>>("Invalid json format. Could not deserialize");
                    }

                    if (roots.Data.Count == 0)
                    {
                        return Result.Error<List<UKNationalGridData>>("No data in json available");
                    }
                    return roots.Data;
                }
            );
        }
        catch (Exception ex)
        {
            return Result.Error<List<UKNationalGridData>>(ex.Message);
        }
    }
    public async Task<Result<List<UKNationalGridData>>> GetForecastAsync(Option<UKRegions> region)
    {
        if (region.IsNone())
        {
            return await GetForecastAsync();
        }
        try
        {
            var uri = new Uri(string.Format(m_RegionBaseUri, DateTimeOffset.UtcNow.AddHours(-1).ToString("O"), (int)region.GetValueOrThrow()));
            return await m_GetContent.Invoke(uri).Bind(
                json =>
                {
                    var roots = JsonSerializer.Deserialize<UKNationalGridRoot>(json);
                    if (roots == null)
                    {
                        return Result.Error<List<UKNationalGridData>>("Invalid json format. Could not deserialize");
                    }

                    if (roots.Data.Data.Count == 0)
                    {
                        return Result.Error<List<UKNationalGridData>>("No data in json available");
                    }
                    return roots.Data.Data;
                }
            );
        }
        catch (Exception ex)
        {
            return Result.Error<List<UKNationalGridData>>(ex.Message);
        }
    }
}

public enum UKRegions
{
    NorthScotland = 1,
    SouthScotland = 2,
    NorthWestEngland = 3,
    NorthEastEngland = 4,
    Yorkshire = 5,
    NorthWales = 6,
    SouthWales = 7,
    WestMidlands = 8,
    EastMidlands = 9,
    EastEngland = 10,
    SouthWestEngland = 11,
    SouthEngland = 12,
    London = 13,
    SouthEastEngland = 14,
    England = 15,
    Scotland = 16,
    Wales = 17
}
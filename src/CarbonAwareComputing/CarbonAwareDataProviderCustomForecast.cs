namespace CarbonAwareComputing;

public class CarbonAwareDataProviderWithCustomForecast : CarbonAwareDataProviderCachedData
{
    private readonly Func<ComputingLocation, CachedData, Task<CachedData>> m_GetEmissionData;

    public CarbonAwareDataProviderWithCustomForecast(Func<ComputingLocation, CachedData, Task<CachedData>> getEmissionData)
    {
        m_GetEmissionData = getEmissionData;
    }

    protected override Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        return m_GetEmissionData(location, currentCachedData);
    }
}
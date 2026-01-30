using EmissionsData = CarbonAware.Model.EmissionsData;

namespace CarbonAwareComputing;

internal abstract class EmissionsForecastDataCacheBase
{
    private readonly ComputingLocation m_Location;
    private CachedData m_CachedData = new(new List<EmissionsData>(), DateTimeOffset.MinValue, string.Empty);
    protected EmissionsForecastDataCacheBase(ComputingLocation location)
    {
        m_Location = location;
    }
    public async Task<List<EmissionsData>> GetForecastData()
    {
        m_CachedData = await FillEmissionsDataCache(m_Location, m_CachedData).ConfigureAwait(false);
        return m_CachedData.EmissionsData;
    }
    public async Task<DataBoundary> GetDataBoundary()
    {
        m_CachedData = await FillEmissionsDataCache(m_Location, m_CachedData).ConfigureAwait(false);
        if (m_CachedData.EmissionsData.Count == 0)
        {
            return new DataBoundary(DateTimeOffset.MaxValue, DateTimeOffset.MinValue);
        }
        return new DataBoundary(m_CachedData.EmissionsData[0].Time, m_CachedData.EmissionsData[^1].Time);
    }
    protected abstract Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData);
}
internal class EmissionsForecastDataCache : EmissionsForecastDataCacheBase
{
    private readonly Func<CachedData, ComputingLocation, Task<CachedData>> m_FillEmissionDataCache;

    public EmissionsForecastDataCache(Func<CachedData, ComputingLocation, Task<CachedData>> fillEmissionDataCache, ComputingLocation location) : base(location)
    {
        m_FillEmissionDataCache = fillEmissionDataCache;
    }

    protected override async Task<CachedData> FillEmissionsDataCache(ComputingLocation location, CachedData currentCachedData)
    {
        return await m_FillEmissionDataCache.Invoke(currentCachedData, location).ConfigureAwait(false);
    }
}

public record CachedData(List<EmissionsData> EmissionsData, DateTimeOffset LastUpdate, string? Version);
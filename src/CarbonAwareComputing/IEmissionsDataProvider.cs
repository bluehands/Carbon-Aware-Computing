using CarbonAware.Model;

namespace CarbonAware.DataSources.Memory;

public interface IEmissionsDataProvider
{
    public Task<List<EmissionsData>> GetForecastData(Location location);
}
using System.Net.Http;

namespace CarbonAwareComputing.ExecutionForecast;

public class CarbonAwareDataProviderBuildIn : CarbonAwareDataProviderOnlineForecastFile
{
    private readonly ComputingLocation m_Location;
    public CarbonAwareDataProviderBuildIn(ComputingLocation location)
    {
        m_Location = location;
    }

    public async Task<ExecutionTime> CalculateBestExecutionTime(DateTimeOffset earliestExecutionTime, DateTimeOffset latestExecutionTime, TimeSpan estimatedJobDuration)
    {
        return await base.CalculateBestExecutionTime(m_Location, earliestExecutionTime, latestExecutionTime, estimatedJobDuration);
    }

}
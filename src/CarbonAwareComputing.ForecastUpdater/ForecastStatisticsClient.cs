using Azure.Data.Tables;
using Azure.Identity;
using CarbonAware.Model;
using CarbonAwareComputing.ExecutionForecast;
using FunicularSwitch;

namespace CarbonAwareComputing.ForecastUpdater;

public class ForecastStatisticsClient
{
    private readonly string m_TableName;
    private readonly Uri m_BaseUri;

    public ForecastStatisticsClient(string storageAccountName, string tableName)
    {
        m_TableName = tableName;
        m_BaseUri = new Uri($"https://{storageAccountName}.table.core.windows.net");
    }
    public async Task<Result<EmissionsForecast>> UpdateForecastData(ComputingLocation location, EmissionsForecast emissionsForecast)
    {
        try
        {
            var credentials = new DefaultAzureCredential();
            var tableClient = new TableClient(m_BaseUri, m_TableName, credentials);

            var first = emissionsForecast.ForecastData.First();
            var last = emissionsForecast.ForecastData.Last();
            var tableEntity = new TableEntity("statistics", location.Name)
            {
                { "GeneratedAt", emissionsForecast.GeneratedAt },
                { "UploadedAt", DateTimeOffset.Now },
                { "ForecastDurationInHours", (last.Time-first.Time).TotalHours },
                { "LastForecast", last.Time }
            };
            await tableClient.AddEntityAsync(tableEntity);
            return emissionsForecast;
        }
        catch (Exception ex)
        {
            return Result.Error<EmissionsForecast>(ex.Message);
        }
    }
}
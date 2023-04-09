using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using CarbonAware.Model;
using CarbonAwareComputing.ExecutionForecast;
using FunicularSwitch;
using static CarbonAwareComputing.ForecastUpdater.ForecastStatisticsClient;

namespace CarbonAwareComputing.ForecastUpdater;

public class ForecastStatisticsClient
{
    private readonly string m_TableName;
    private readonly Uri m_BaseUri;
    private const string PartitionKey = "statistics";

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
            var tableEntity = new TableEntity(PartitionKey, location.Name)
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
    public async Task<Result<List<Statistic>>> GetForecastData()
    {
        try
        {
            var credentials = new DefaultAzureCredential();
            var tableClient = new TableClient(m_BaseUri, m_TableName, credentials);

            var statistics = tableClient.QueryAsync<Statistic>(x => x.PartitionKey.Equals(PartitionKey));
            var l = new List<Statistic>();
            await foreach (var page in statistics.AsPages())
            {
                foreach (var statistic in page.Values)
                {
                    l.Add(statistic);
                }
            }
            return l;
        }
        catch (Exception ex)
        {
            return Result.Error<List<Statistic>>(ex.Message);
        }
    }
}
public record Statistic : ITableEntity
{
    public string RowKey { get; set; } = default!;
    public string PartitionKey { get; set; } = default!;
    public DateTimeOffset? GeneratedAt { get; init; }
    public DateTimeOffset? UploadedAt { get; init; }
    public double? ForecastDurationInHours { get; init; }
    public DateTimeOffset? LastForecast { get; init; }
    public ETag ETag { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using FunicularSwitch;
using Azure.Identity;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using CarbonAwareComputing.ForecastUpdater.EnergyCharts;
using CarbonAwareComputing.ForecastUpdater.UKNationalGrid;

// ReSharper disable StringLiteralTypo
// ReSharper disable ConvertClosureToMethodGroup

namespace CarbonAwareComputing.ForecastUpdater.Function
{
    public class ForecastUpdateFunction
    {
        private readonly IOptions<ApplicationSettings> m_ApplicationSettings;
        private readonly HttpClient m_Http;

        public ForecastUpdateFunction(IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> applicationSettings)
        {
            m_ApplicationSettings = applicationSettings;
            m_Http = httpClientFactory.CreateClient();
        }


        [FunctionName("ScheduledUpdateForecast")]
        public async Task ScheduledUpdateForecast([TimerTrigger("0 20 8,12,16,18,19,20 * * *")] TimerInfo myTimer, ILogger log)
        {
            await UpdateForecast(log);
        }

        [FunctionName("ScheduledReportForecast")]
        public async Task ScheduledReportForecast([TimerTrigger("0 20 8 * * *")] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            await ReportForecast(log, context.FunctionAppDirectory);
        }
        [FunctionName("ManualUpdateForecast")]
        public async Task<IActionResult> ManualUpdateForecast(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            await UpdateForecast(log);
            return new OkObjectResult("Updated");
        }

        [FunctionName("ManualReportForecast")]
        public async Task<IActionResult> ManualReportForecast(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            await ReportForecast(log, context.FunctionAppDirectory);
            return new OkObjectResult("Reported");
        }

        private async Task UpdateForecast(ILogger log)
        {
            await UpdateEnergyChartsForecast(log);
            await UpdateUKNationalGridForecast(log);
        }

        private async Task UpdateEnergyChartsForecast(ILogger log)
        {
            var energyChartsClient = GetEnergyChartsClient();
            var cachedForecastClient = new CachedForecastClient("carbonawarecomputing", "forecasts");
            var forecastStatisticsClient = new ForecastStatisticsClient("carbonawarecomputing", "forecasts");
            foreach (var computingLocation in ComputingLocations.All.Where(l => l.IsActive && l.ForecastProvider == ForecastProvider.EnergyCharts).DistinctBy(l => l.Name))
            {
                await energyChartsClient.GetCarbonGridIntensityForecastAsync(computingLocation.Name).Bind(
                    energyChartsRoot => EnergyChartsTransform.ImportForecast(energyChartsRoot, computingLocation.Name)).Bind(
                    emissionsForecast => forecastStatisticsClient.UpdateForecastData(computingLocation, emissionsForecast)).Bind(
                    emissionsForecast => Transform.Serialize(emissionsForecast)).Bind(
                    json => cachedForecastClient.UpdateForecastData(computingLocation, json)
                ).Match(
                    o => No.Thing,
                    e =>
                    {
                        log.LogError(e);
                        return No.Thing;
                    }
                );
            }
        }
        private async Task UpdateUKNationalGridForecast(ILogger log)
        {
            var ukNationalGridClient = GetUKNationalGridClient();
            var cachedForecastClient = new CachedForecastClient("carbonawarecomputing", "forecasts");
            var forecastStatisticsClient = new ForecastStatisticsClient("carbonawarecomputing", "forecasts");
            foreach (var computingLocation in ComputingLocations.All.Where(l => l.IsActive && l.ForecastProvider == ForecastProvider.UKNationalGrid).DistinctBy(l => l.Name))
            {
                var ukNationalGridRegion = ConvertToUKNationalGridRegion(computingLocation.Name);
                await ukNationalGridClient.GetForecastAsync(ukNationalGridRegion).Bind(
                    ukNationalGridRoot => UKNationalGridTransform.ImportForecast(ukNationalGridRoot, ukNationalGridRegion)).Bind(
                    emissionsForecast => forecastStatisticsClient.UpdateForecastData(computingLocation, emissionsForecast)).Bind(
                    emissionsForecast => Transform.Serialize(emissionsForecast)).Bind(
                    json => cachedForecastClient.UpdateForecastData(computingLocation, json)
                ).Match(
                    o => No.Thing,
                    e =>
                    {
                        log.LogError(e);
                        return No.Thing;
                    }
                );
            }
        }

        private async Task ReportForecast(ILogger log, string currentDirectory)
        {
            var forecastStatisticsClient = new ForecastStatisticsClient("carbonawarecomputing", "forecasts");
            var statistics = await forecastStatisticsClient.GetForecastData();
            string messageText = statistics.Match(
                s =>
                {
                    var sb = new StringBuilder();
                    sb.
                        Append("Location").Append("\t").
                        Append("GeneratedAt").Append("\t").
                        Append("UploadedAt").Append("\t").
                        Append("ForecastDurationInHours").Append("\t").
                        Append("LastForecast").AppendLine();
                    foreach (var row in s)
                    {
                        sb.
                            Append(row.RowKey).Append("\t").
                            Append(row.GeneratedAt).Append("\t").
                            Append(row.UploadedAt).Append("\t").
                            Append(row.ForecastDurationInHours).Append("\t").
                            Append(row.LastForecast).AppendLine();
                    }
                    return sb.ToString();
                },
                e => $"Could not get forecast: {e}");

            var template = await System.IO.File.ReadAllTextAsync(Path.Combine(currentDirectory, "mail_template.txt"));
            await SendStatisticsAsync(log, m_ApplicationSettings.Value.MailFrom, messageText, template);
        }
        private EnergyChartsClient GetEnergyChartsClient()
        {
            var client = new EnergyChartsClient(async u =>
            {
                try
                {
                    return await m_Http.GetStringAsync(u);
                }
                catch (Exception ex)
                {
                    return Result.Error<string>(ex.Message);
                }
            });
            return client;
        }
        private UKNationalGridClient GetUKNationalGridClient()
        {
            var client = new UKNationalGridClient(async u =>
            {
                try
                {
                    return await m_Http.GetStringAsync(u);
                }
                catch (Exception ex)
                {
                    return Result.Error<string>(ex.Message);
                }
            });
            return client;
        }
        private async Task<bool> SendStatisticsAsync(ILogger log, string mailAddress, string statistics, string template)
        {
            var tenantId = m_ApplicationSettings.Value.TenantId;
            var clientId = m_ApplicationSettings.Value.ClientId;
            var clientSecret = m_ApplicationSettings.Value.ClientSecret;

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var graphClient = new GraphServiceClient(credential);

            Message message = new()
            {
                Subject = "Statistics for Carbon Aware Computing Execution Forecast ",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = template.Replace("{{STATISTICS}}", statistics)
                },
                ToRecipients = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = mailAddress
                        }
                    }
                }
            };

            bool saveToSentItems = true;

            try
            {
                await graphClient.Users[m_ApplicationSettings.Value.MailFrom]
                    .SendMail(message, saveToSentItems)
                    .Request()
                    .PostAsync();
                return true;
            }
            catch (Exception ex)
            {
                log.LogError($"Could not send mail to {mailAddress}. Error: {ex.Message}");
                return false;
            }
        }
        private Option<UKRegions> ConvertToUKNationalGridRegion(string computingLocationName)
        {
            if (ComputingLocations.TryParse(computingLocationName, out var computingLocation) && computingLocation!.ForecastProvider == ForecastProvider.UKNationalGrid)
            {
                var name = computingLocation.Name.ToLowerInvariant();
                if (name == "uk")
                {
                    return Option<UKRegions>.None;
                }

                if (Enum.TryParse<UKRegions>(name, true, out var region))
                {
                    return region;
                }
            }
            return Option<UKRegions>.None;
        }
    }
}

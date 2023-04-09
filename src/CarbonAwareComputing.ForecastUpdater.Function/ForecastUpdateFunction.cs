using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CarbonAwareComputing.ExecutionForecast;
using System.Net.Http;
using System.Runtime.CompilerServices;
using FunicularSwitch;
using static System.Net.WebRequestMethods;
// ReSharper disable StringLiteralTypo
// ReSharper disable ConvertClosureToMethodGroup

namespace CarbonAwareComputing.ForecastUpdater.Function
{
    public class ForecastUpdateFunction
    {
        private readonly HttpClient m_Http;

        public ForecastUpdateFunction(IHttpClientFactory httpClientFactory)
        {
            m_Http = httpClientFactory.CreateClient();
        }


        [FunctionName("ScheduledUpdateForecast")]
        public async Task ScheduledUpdateForecast([TimerTrigger("0 20 8,12,16,18,19,20 * * *")] TimerInfo myTimer, ILogger log)
        {
            await Update(log);
        }

        [FunctionName("ManualUpdateForecast")]
        public async Task<IActionResult> ManualUpdateForecast(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            await Update(log);
            return new OkObjectResult("Updated");
        }

        private async Task Update(ILogger log)
        {
            var energyChartsClient = GetEnergyChartsClient();
            var cachedForecastClient = new CachedForecastClient("carbonawarecomputing", "forecasts");
            var forecastStatisticsClient = new ForecastStatisticsClient("carbonawarecomputing", "forecasts");
            foreach (var computingLocation in ComputingLocations.All.Where(l => l.IsActive))
            {
                await energyChartsClient.GetForecastAsync(computingLocation.Name).Bind(
                    energyChartsRoot => Transform.ImportForecast(energyChartsRoot, computingLocation.Name)).Bind(
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
    }
}

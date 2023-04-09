// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;
using System.Xml.Linq;
using System.Net.Mail;
using System.Web.Http;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph;


namespace CarbonAwareComputing.ExecutionForecast.Function
{
    public class EmissionForecastFunction
    {
        private readonly IOptions<ApplicationSettings> m_ApplicationSettings;
        private readonly CarbonAwareDataProviderOpenData m_Provider;

        public EmissionForecastFunction(
            IOptions<ApplicationSettings> applicationSettings,
            CarbonAwareDataProviderOpenData provider
            )
        {
            m_ApplicationSettings = applicationSettings;
            m_Provider = provider;
        }

        [OpenApiOperation(operationId: "Register", tags: new[] { "forecast" }, Summary = "Register yourself to this API. A API-Key is send to your mail address. The address is only used to inform you about incompatible changes to this service.", Description = "Register yourself to this API.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody("application/json", typeof(RegistrationData), Required = true, Description = "The mail address API-Key ist send")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "API-Key sent. Operation succeeded", Description = "API-Key sent. Operation succeeded")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ProblemDetails), Summary = "failed operation", Description = "failed operation")]
        [FunctionName("Register")]
        public async Task<IActionResult> Register(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "register")]
            HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            try
            {
                using var streamReader = new StreamReader(req.Body);
                var requestBody = await streamReader.ReadToEndAsync();
                var registration = JsonConvert.DeserializeObject<RegistrationData>(requestBody);
                var mailAddress = registration?.MailAddress;
                if (string.IsNullOrWhiteSpace(mailAddress))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Invalid or empty mail address",
                        Status = 400
                    });
                }

                var apiKey = await StringCipher.EncryptAsync(mailAddress, m_ApplicationSettings.Value.ApiKeyPassword!);

                var template = await System.IO.File.ReadAllTextAsync(Path.Combine(context.FunctionAppDirectory, "mail_template.txt"));
                if (!await SendApiKeyAsync(log, mailAddress, apiKey, template))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Could not send the API-Key",
                        Status = 400
                    });
                }

                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.LogError($"Unexpected Error. {ex}");
                return new InternalServerErrorResult();
            }
        }

        [OpenApiOperation(operationId: "GetBestExecutionTime", tags: new[] { "forecast" }, Summary = "Get the best execution time with minimal grid carbon intensity", Description = "Get the best execution time with minimal grid carbon intensity. A time intervall of the given duration within the earliest and latest execution time with the most renewable energy in the power grid of the location", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "location", In = ParameterLocation.Query, Required = true, Type = typeof(string[]), Description = "String array of named locations")]
        [OpenApiParameter(name: "dataStartAt", In = ParameterLocation.Query, Required = false, Type = typeof(DateTimeOffset), Description = "Start time boundary of forecasted data points. Ignores current forecast data points before this time. Defaults to the earliest time in the forecast data.")]
        [OpenApiParameter(name: "dataEndAt", In = ParameterLocation.Query, Required = false, Type = typeof(DateTimeOffset), Description = "End time boundary of forecasted data points. Ignores current forecast data points after this time. Defaults to the latest time in the forecast data.")]
        [OpenApiParameter(name: "windowSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "The estimated duration (in minutes) of the workload. Defaults to 5 Minutes (This is different from GSF SDK which default to the duration of a single forecast data point).")]
        [OpenApiSecurity("apikey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EmissionsForecast), Summary = "Forecast available. Operation succeeded", Description = "Forecast data is available and the best execution time is provided. Tis is a subset of the GSF SDK data. No infoormation on the underlying forecast data ist provided. E.g. no forecast boundaries, no forcast data, no forecast generation date")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ProblemDetails), Summary = "Forecast is not available for the location or time window. Operation failed", Description = "Forecast is not available for the location or time window. Operation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ProblemDetails), Summary = "failed operation", Description = "failed operation")]
        [FunctionName("GetBestExecutionTime")]
        public async Task<IActionResult> GetBestExecutionTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "forecasts/current")]
            HttpRequest req,
            ILogger log)
        {
            try
            {
                var apiKeyHeader = req.Headers.FirstOrDefault(h => h.Key.Equals("x-api-key", StringComparison.InvariantCultureIgnoreCase));
                var apiKey = apiKeyHeader.Value.ToArray().FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return new StatusCodeResult(403);
                }

                var mailAddress = await StringCipher.DecryptAsync(apiKey, m_ApplicationSettings.Value.ApiKeyPassword!);
                if (string.IsNullOrEmpty(apiKey))
                {
                    return new StatusCodeResult(403);
                }
                var location = req.Query["location"];
                if (location.Count == 0)
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Requiered parameter 'location' not provided",
                        Status = Convert.ToInt32(HttpStatusCode.NotFound)
                    });
                }

                if (!TryGetDateTimeOffset(req.Query["dataStartAt"], DateTimeOffset.MinValue, out var dataStartAt))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Optional parameter 'dataStartAt' is bad formated",
                        Status = Convert.ToInt32(HttpStatusCode.BadRequest)
                    });
                }

                if (!TryGetDateTimeOffset(req.Query["dataEndAt"], DateTimeOffset.MaxValue, out var dataEndAt))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Optional parameter 'dataEndAt' is bad formated",
                        Status = Convert.ToInt32(HttpStatusCode.BadRequest)
                    });
                }

                if (!TryGetInt(req.Query["windowSize"], 5, out var windowSize))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Optional parameter 'windowSize' is bad formated",
                        Status = Convert.ToInt32(HttpStatusCode.BadRequest)
                    });
                }

                var forecasts = new List<EmissionsForecast>();
                foreach (var l in location.First().Split(','))
                {
                    var computingLocation = new ComputingLocation(l.Trim());
                    var best = await m_Provider.CalculateBestExecutionTime(computingLocation, dataStartAt, dataEndAt, TimeSpan.FromMinutes(windowSize));
                    if (best is ExecutionTime.BestExecutionTime_ bestExecutionTime)
                    {
                        forecasts.Add(new EmissionsForecast
                        {
                            Location = computingLocation.Name,
                            WindowSize = windowSize,
                            OptimalDataPoints = new List<EmissionsData>
                        {
                            new()
                            {
                                Timestamp = bestExecutionTime.ExecutionTime,
                                Value = bestExecutionTime.Rating
                            }
                        }
                        });
                    }
                }


                if (forecasts.Count > 0)
                {
                    return new OkObjectResult(forecasts);
                }

                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                log.LogError($"Unexpected Error. {ex}");
                return new InternalServerErrorResult();
            }
        }

        [OpenApiOperation(operationId: "GetLocations", tags: new[] { "forecast" }, Summary = "Get a list of available locations. Not all locations are active, to avoid unnecessary computing. Send a message to 'a.mirmohammadi@bluehands.de' to activate a location.", Description = "Get a list of available locations. Not all locations are active, to avoid unnecessary computing. Send a message to 'a.mirmohammadi@bluehands.de' to activate a location.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AvailableLocation[]), Summary = "Operation succeeded", Description = "Operation succeeded")]
        [FunctionName("GetLocations")]
        public IActionResult GetLocations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "locations")]
            HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(ComputingLocations.All.Select(c => new AvailableLocation(c.Name, c.IsActive)));
        }
        private async Task<bool> SendApiKeyAsync(ILogger log, string mailAddress, string apiKey, string template)
        {
            var tenantId = m_ApplicationSettings.Value.TenantId;
            var clientId = m_ApplicationSettings.Value.ClientId;
            var clientSecret = m_ApplicationSettings.Value.ClientSecret;

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var graphClient = new GraphServiceClient(credential);

            Message message = new()
            {
                Subject = "Your API-Key for Carbon Aware Computing Execution Forecast ",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = template.Replace("{{APIKEY}}", apiKey)
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
        private static bool TryGetDateTimeOffset(StringValues values, DateTimeOffset defaultDate, out DateTimeOffset date)
        {
            var d = values.FirstOrDefault();
            if (d != null)
            {
                return DateTimeOffset.TryParse(d, out date);
            }

            date = defaultDate;
            return true;
        }
        private static bool TryGetInt(StringValues values, int defaultData, out int data)
        {
            var d = values.FirstOrDefault();
            if (d != null)
            {
                return Int32.TryParse(d, out data);
            }

            data = defaultData;
            return true;
        }
    }

    public record AvailableLocation(string LocationName, bool Active);

    public record RegistrationData(string MailAddress);

    [Serializable]
    public record EmissionsForecast
    {
        /// <summary>The location of the forecast</summary>
        /// <example>de</example>
        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// The estimated duration (in minutes) of the workload.
        /// Defaults to the duration of a single forecast data point.
        /// </summary>
        /// <example>30</example>
        [JsonPropertyName("windowSize")]
        public int WindowSize { get; set; }

        /// <summary>
        /// The optimal forecasted data point within the 'forecastData' array.
        /// Null if 'forecastData' array is empty.
        /// </summary>
        /// <example>
        /// {
        ///   "location": "de",
        ///   "timestamp": "2022-06-01T14:45:00Z",
        ///   "duration": 30,
        ///   "value": 359.23
        /// }
        /// </example>
        [JsonPropertyName("optimalDataPoints")]
        public IEnumerable<EmissionsData>? OptimalDataPoints { get; set; }

        public static EmissionsForecast FromEmissionsForecast(GSF.CarbonAware.Models.EmissionsForecast emissionsForecast, int? windowSize)
        {
            return new EmissionsForecast
            {
                Location = emissionsForecast.Location,
                OptimalDataPoints = emissionsForecast.OptimalDataPoints.Select(EmissionsData.FromEmissionsData)!,
                WindowSize = windowSize ?? 0
            };
        }
    }

    [Serializable]
    public record EmissionsData
    {
        /// <example>2022-06-01T14:45:00Z</example>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        /// <example>359.23</example>
        [JsonPropertyName("value")]
        public double Value { get; set; }

        public static EmissionsData FromEmissionsData(GSF.CarbonAware.Models.EmissionsData emissionsData)
        {
            if (emissionsData == null)
            {
                return null;
            }

            return new EmissionsData
            {
                Timestamp = emissionsData.Time,
                Value = emissionsData.Rating
            };
        }
    }
}

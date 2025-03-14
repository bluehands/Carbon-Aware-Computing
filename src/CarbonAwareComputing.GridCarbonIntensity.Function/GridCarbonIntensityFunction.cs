using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using CarbonAwareComputing.ExecutionForecast.Function;
using CarbonAwareComputing.Functions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace CarbonAwareComputing.GridCarbonIntensity.Function
{
    public class GridCarbonIntensityFunction
    {
        private readonly IOptions<ApplicationSettings> m_ApplicationSettings;
        private readonly CarbonAwareDataProvider m_Provider;

        public GridCarbonIntensityFunction(
            IOptions<ApplicationSettings> applicationSettings,
            CarbonAwareDataProvider provider
            )
        {
            m_ApplicationSettings = applicationSettings;
            m_Provider = provider;
        }

        [OpenApiOperation(operationId: "Register", tags: new[] { "Usage" }, Summary = "Register yourself to the Carbon Aware Computing API. A API-Key is send to your mail address. The address is only used to inform you about incompatible changes to this service.", Description = "Register yourself to this API.", Visibility = OpenApiVisibilityType.Important)]
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
                var template = await File.ReadAllTextAsync(Path.Combine(context.FunctionAppDirectory, "mail_template.txt"));
                var apiKeyPassword = m_ApplicationSettings.Value.ApiKeyPassword!;
                var mailFrom = m_ApplicationSettings.Value.MailFrom;
                var tenantId = m_ApplicationSettings.Value.TenantId;
                var clientId = m_ApplicationSettings.Value.ClientId;
                var clientSecret = m_ApplicationSettings.Value.ClientSecret;
                return await ApiRegistration.Register(req.Body, apiKeyPassword!, template, mailFrom, tenantId, clientId, clientSecret, log);
            }
            catch (Exception ex)
            {
                log.LogError($"Unexpected Error. {ex}");
                return new InternalServerErrorResult();
            }
        }

        [OpenApiOperation(operationId: "GetCurrentGridCarbonIntensity", tags: new[] { "Carbon Intensity" }, Summary = "Get the current grid carbon intensity for the given location", Description = "Get the current grid carbon intensity for a given location. The carbon intensity in gCO2e/kWh is returned", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "location", In = ParameterLocation.Query, Required = true, Type = typeof(string), Example = typeof(LocationExample), Description = "A named locations like 'de' or a cloud region like 'germanywestcentral'. Use the 'locations' endpoint to get the list of supported and available locations")]
        [OpenApiSecurity("apikey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EmissionData), Summary = "Carbon intensity available. Operation succeeded", Description = "Grid carbon intensity is available for this location and provided.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ProblemDetails), Summary = "Carbon intensity is not available for the location or time window. Operation failed", Description = "Forecast is not available for the location. Operation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ProblemDetails), Summary = "failed operation", Description = "failed operation")]
        [FunctionName("GetCurrentGridCarbonIntensity")]
        public async Task<IActionResult> GetCurrentGridCarbonIntensity(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "emissions/current")]
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
                if (string.IsNullOrEmpty(mailAddress))
                {
                    return new StatusCodeResult(403);
                }
                var locations = req.Query["location"];
                var location = locations.FirstOrDefault();
                if (string.IsNullOrEmpty(location))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = "Required parameter 'location' not provided",
                        Status = Convert.ToInt32(HttpStatusCode.NotFound)
                    });
                }
                if (!ComputingLocations.TryParse(location.Trim(), out var computingLocation))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Detail = $"Unknown or not provided location '{location}' not provided",
                        Status = Convert.ToInt32(HttpStatusCode.NotFound)
                    });
                }

                var intensity = await m_Provider.GetCarbonIntensity(computingLocation, DateTimeOffset.Now);
                return intensity.Match<IActionResult>(
                    emissionData: e => new OkObjectResult(new EmissionData(e.Location, e.Time, e.Value)),
                    noData: _ => new NotFoundResult()

                );
            }
            catch (Exception ex)
            {
                log.LogError($"Unexpected Error. {ex}");
                return new InternalServerErrorResult();
            }
        }

        [OpenApiOperation(operationId: "GetLocations", tags: new[] { "Usage" }, Summary = "Get a list of available locations. Not all locations are active, to avoid unnecessary computing. Send a message to 'a.mirmohammadi@bluehands.de' to activate a location.", Description = "Get a list of available locations. Not all locations are active, to avoid unnecessary computing. Send a message to 'a.mirmohammadi@bluehands.de' to activate a location.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AvailableLocation[]), Summary = "Operation succeeded", Description = "Operation succeeded")]
        [FunctionName("GetLocations")]
        public IActionResult GetLocations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "locations")]
            HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(ComputingLocations.All.Select(c => new AvailableLocation(c)));
        }
    }
}

using CarbonAwareComputing.ExecutionForecast.Function;
using CarbonAwareComputing.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using EmissionsData = CarbonAware.Model.EmissionsData;

namespace CarbonAwareComputing.ExecutionForecast.Test;

[TestClass]
public class EmissionForecastFunctionSpecs
{
    private static readonly DateTimeOffset DataStart = new(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);

    [DataTestMethod]
    [DataRow(-60, -30, 3)]
    [DataRow(60, 120, 3)]
    [DataRow(60, 1500, 30)]
    [DataRow(0, 30, 60)]
    public async Task UnavailablePeriodReturnsNotFoundWithProblemDetails(int startMinutes, int endMinutes, int windowSize)
    {
        var start = DataStart.AddMinutes(startMinutes);
        var end = DataStart.AddMinutes(endMinutes);
        var request = await CreateRequest($"?location=de&dataStartAt={Uri.EscapeDataString(start.ToString("O"))}"
            + $"&dataEndAt={Uri.EscapeDataString(end.ToString("O"))}&windowSize={windowSize}");

        var result = await CreateFunction().GetBestExecutionTime(request);

        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var response = (ObjectResult)result;
        Assert.AreEqual(404, response.StatusCode);
        CollectionAssert.Contains(response.ContentTypes.ToArray(), "application/problem+json");
        Assert.IsInstanceOfType(response.Value, typeof(ProblemDetails));
        var problem = (ProblemDetails)response.Value!;
        Assert.AreEqual(404, problem.Status);
        Assert.AreEqual("Forecast data unavailable", problem.Title);
        Assert.AreEqual("https://www.rfc-editor.org/rfc/rfc9110.html#section-15.5.5", problem.Type);
        StringAssert.Contains(problem.Detail, "'de'");
        StringAssert.Contains(problem.Detail, start.ToString("O"));
        StringAssert.Contains(problem.Detail, end.ToString("O"));
        StringAssert.Contains(problem.Detail, $"{windowSize} minutes");
        StringAssert.Contains(problem.Detail, "outdated");
        StringAssert.Contains(problem.Detail, "retry after forecast data has been refreshed");

        using var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        request.HttpContext.RequestServices = services;
        using var body = new MemoryStream();
        request.HttpContext.Response.Body = body;
        await response.ExecuteResultAsync(new ActionContext
        {
            HttpContext = request.HttpContext,
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
        });
        Assert.AreEqual(404, request.HttpContext.Response.StatusCode);
        StringAssert.StartsWith(request.HttpContext.Response.ContentType, "application/problem+json");
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        Assert.AreEqual(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.AreEqual(problem.Detail, json.RootElement.GetProperty("detail").GetString());
    }

    [DataTestMethod]
    [DataRow("?location=de")]
    [DataRow("?location=de&dataStartAt=2026-10-03T00:00:00Z&dataEndAt=2026-10-03T00:30:00Z&windowSize=3")]
    public async Task AvailablePeriodStillReturnsForecast(string query)
    {
        var result = await CreateFunction().GetBestExecutionTime(await CreateRequest(query));

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var forecasts = (IEnumerable<EmissionsForecast>)((OkObjectResult)result).Value!;
        var forecast = forecasts.Single();
        Assert.AreEqual("de", forecast.Location);
        Assert.AreEqual(DataStart, forecast.OptimalDataPoints!.Single().Timestamp);
    }

    [TestMethod]
    public void OpenApiDocumentsNotFoundProblemResponse()
    {
        var responses = typeof(EmissionForecastFunction)
            .GetMethod(nameof(EmissionForecastFunction.GetBestExecutionTime))!
            .GetCustomAttributes(typeof(OpenApiResponseWithBodyAttribute), false)
            .Cast<OpenApiResponseWithBodyAttribute>().ToArray();

        var notFound = responses.Single(response => response.StatusCode == HttpStatusCode.NotFound);
        Assert.AreEqual(typeof(ProblemDetails), notFound.BodyType);
        Assert.AreEqual("application/problem+json", notFound.ContentType);
        Assert.IsFalse(responses.Any(response => response.StatusCode == HttpStatusCode.Gone));
    }

    private static EmissionForecastFunction CreateFunction()
    {
        var data = Enumerable.Range(0, 6).Select(i => new EmissionsData
        {
            Time = DataStart.AddMinutes(i * 5),
            Duration = TimeSpan.FromMinutes(5),
            Location = "de",
            Rating = 10 + i
        }).ToList();
        var provider = new CarbonAwareDataProviderWithCustomForecast((_, _) =>
            Task.FromResult(new CachedData(data, DateTimeOffset.UtcNow, "test-forecast")));

        return new EmissionForecastFunction(
            Options.Create(new ApplicationSettings { ApiKeyPassword = nameof(EmissionForecastFunctionSpecs) }),
            provider,
            NullLogger<EmissionForecastFunction>.Instance);
    }

    private static async Task<HttpRequest> CreateRequest(string query)
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString(query);
        request.Headers["x-api-key"] = await StringCipher.EncryptAsync(
            "forecast-test@example.invalid", nameof(EmissionForecastFunctionSpecs));
        return request;
    }
}

using System.Reflection;
using CarbonAwareComputing.Functions;
using CarbonAwareComputing.GridCarbonIntensity;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarbonAwareComputing.GridCarbonIntensity;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);
        builder.ConfigureFunctionsWebApplication();
        builder.Configuration
            .AddJsonFile("local.settings.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables();
        builder.Services.AddSingleton<CarbonAwareDataProvider, CarbonAwareDataProviderOpenData>();
        builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));
        builder.Services.AddSingleton<IOpenApiConfigurationOptions, OpenApiConfigurationOptions>();
        builder.Build().Run();
    }
}

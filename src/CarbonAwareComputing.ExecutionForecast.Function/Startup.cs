using System;
using System.Reflection;
using CarbonAwareComputing.ExecutionForecast.Function;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CarbonAwareComputing.ExecutionForecast.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<CarbonAwareDataProviderOnlineForecastFile>();
            builder.Services.AddOptions<ApplicationSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("ApplicationSettings").Bind(settings);
                });
        }
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            // local dev no Key Vault
            builder.ConfigurationBuilder
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddEnvironmentVariables()
                .Build();

        }
    }

    public class ApplicationSettings
    {
        public string? ApiKeyPassword { get; init; }
        public string? MailFrom { get; init; }
        public string? ClientId { get; init; }
        public string? TenantId { get; init; }
        public string? ClientSecret { get; init; }

    }
}

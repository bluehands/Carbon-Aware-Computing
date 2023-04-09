using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;

[assembly: FunctionsStartup(typeof(CarbonAwareComputing.ForecastUpdater.Function.Startup))]
namespace CarbonAwareComputing.ForecastUpdater.Function;
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Note: Only register dependencies, do not depend or request those in Configure().
        // Dependencies are only usable during function execution, not before (like here).

        builder.Services.AddHttpClient();
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
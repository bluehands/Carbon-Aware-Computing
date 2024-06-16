﻿using System;
using System.Reflection;
using CarbonAwareComputing.ExecutionForecast.Function;
using CarbonAwareComputing.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CarbonAwareComputing.ExecutionForecast.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<CarbonAwareDataProvider, CarbonAwareDataProviderOpenData>(p => new CarbonAwareDataProviderOpenData());
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
}

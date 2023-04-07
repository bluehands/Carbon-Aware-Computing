using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace CarbonAwareComputing.ExecutionForecast.Function;

internal class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    public override OpenApiInfo Info { get; set; } = new OpenApiInfo
    {
        Version = "1.0.0",
        Title = "Carbon Aware Computing - Execution Forecast",
        Description = "Get the best execution time with minimal grid carbon intensity. A compatible subset of the Green Software Foundation SDK with open data. The data is licensed under the CC0 license (https://creativecommons.org/publicdomain/zero/1.0/)",
        License = new OpenApiLicense
        {
            Name = "Software is licensed under MIT license.",
            Url = new Uri("http://opensource.org/licenses/MIT"),
        }
    };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace CarbonAwareComputing.GridCarbonIntensity.Function;

internal class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    public override OpenApiInfo Info { get; set; } = new OpenApiInfo
    {
        Version = "1.0.0",
        Title = "Carbon Aware Computing - Grid Carbon Intensity",
        Description = "Get the grid carbon intensity of a given computing location. Useful for calculating the carbon emissions of digital services or as a signal to start or stop computing.The data is licensed under the CC0 license (https://creativecommons.org/publicdomain/zero/1.0/)",
        License = new OpenApiLicense
        {
            Name = "Software is licensed under MIT license.",
            Url = new Uri("http://opensource.org/licenses/MIT"),
        }
    };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
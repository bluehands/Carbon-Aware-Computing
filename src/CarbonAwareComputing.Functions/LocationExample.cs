using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json.Serialization;

namespace CarbonAwareComputing.Functions;

public class LocationsExample : OpenApiExample<string>
{
    public override IOpenApiExample<string> Build(NamingStrategy? namingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Germany", "de", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("Austria and France", "at,fr", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("Azure francecentral", "francecentral", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("AWS eu-west-3", "eu-west-3", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("GCP europe-west9", "europe-west9", namingStrategy));
        return this;
    }
}

public class LocationExample : OpenApiExample<string>
{
    public override IOpenApiExample<string> Build(NamingStrategy? namingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Germany", "de", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("Austria", "at", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("Azure francecentral", "francecentral", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("AWS eu-west-3", "eu-west-3", namingStrategy));
        Examples.Add(OpenApiExampleResolver.Resolve("GCP europe-west9", "europe-west9", namingStrategy));
        return this;
    }
}
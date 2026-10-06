# Carbon Aware Computing

**Execute computing tasks when the grid is powered by renewable energy**

The goal of this project is to provide developers with hassle free, easy to use, ready to run tools for carbon aware computing. All libraries and data are open source and open data with unrestricted usage. Within private, open source and commercial software.

## time-shifting, demand-shifting, grid carbon intensity

The basic idea behind the time-shifting approach is to move the computing load in to a point in time, when the power grid has a maximum of renewable energy. This will result in a lower emission of CO2 of your computing task.

This project will deliver a set of libraries, services and data. There are mostly extensions to other projects and all credits belong to them. To forecast the best execution time the [Carbon Aware SDK](https://github.com/Green-Software-Foundation/carbon-aware-sdk) from the [Green Software Foundation](https://greensoftware.foundation/) is used. The Forecast and actual data is from [Energy-Charts](https://www.energy-charts.info/) provided by [Fraunhofer ISE](https://www.ise.fraunhofer.de/). For UK the data is provided by [UK National Grid ESO](https://carbonintensity.org.uk/).

## Get the best execution time as library

Use the NuGet-package for your .NET project to get the best execution time for a task, in a given Execution-Window for a estimated duration. The lib will calculate the optimal execution time within the provided forecast data.

## Get the Grid Carbon Intensity as library

Use the NuGet-package for your .NET project to get the current Grid Carbon Intensity of a given region.

### Installation

Just add the package to your project.

``` powershell
Install-Package CarbonAwareComputing 
```

### Usage

Instantiate a *CarbonAwareDataProvider* and call *CalculateBestExecutionTime*

``` csharp
// Hold the provider as singleton. The forecast data is cached
var provider = new CarbonAwareDataProviderOpenData();
var forecast = provider.CalculateBestExecutionTime(
    ComputingLocations.Germany,
    DateTimeOffset.Now,
    DateTimeOffset.Now + TimeSpan.FromHours(8),
    TimeSpan.FromMinutes(20)
);
var executionTime = forecast.Match(
    noForecast =>
    {
        Console.WriteLine("No forecast available. Use fallback");
        return DateTimeOffset.Now;
    },
    bestExecutionTime =>
    {
        Console.WriteLine($"Forecast available for a task of {bestExecutionTime.Duration} length");
        return bestExecutionTime.ExecutionTime;
    });
```

In the above example we will get a optimal execution time for the German Power Grid from now to 8 hours for a task with an estimated duration of 20 minutes. Please have in mind that this a best effort approach. Based on the data or your boundaries a forecast is not available.  

``` csharp
// Hold the provider as singleton. The forecast data is cached
var provider = new CarbonAwareDataProviderOpenData();
var intensity = provider.GetCarbonIntensity(
    ComputingLocations.Germany,
    DateTimeOffset.Now);
intensity.Match(
    emissionData =>
    {
        Console.WriteLine($"Current grid carbon intensity: {emissionData.Value}");
    },
    _ =>
    {
        Console.WriteLine($"Data not available);
    });
```

In the above example we will get the grid carbon intensity for the German Power Grid for the actual time. Please have in mind that the time range for the intensity is limited. It's mostly for today.  

The *CarbonAwareDataProviderOpenData* has a cache of all forecasts. To improve performance use it as a singleton, to avoid multiple downloads. For a list of locations see below.

## Hangfire Extension

Hangfire is one of the most used tools for background processing in .NET. Use the *[Hangfire.Community.CarbonAwareExecution](https://github.com/bluehands/Hangfire.Community.CarbonAwareExecution)* extension to enqueue and schedule your jobs.

### Installation

Hangfire.Community.CarbonAwareExecution is available as a NuGet package. You can install it using the NuGet-Package Console window:

``` powershell
Install-Package Hangfire.Community.CarbonAwareExecution
```

After installation add the extension to the Hangfire configuration.

``` csharp
builder.Services.AddHangfire(configuration => configuration
    .UseCarbonAwareExecution(new CarbonAwareDataProviderOpenData(), ComputingLocations.Germany)
);
```

### Usage

Every backgroud job with a `CarbonAwareExectution` parameter will now be scheduled with optimized carbon footprint within the interval you specified. For more details check the [GitHub Repository](https://github.com/bluehands/Hangfire.Community.CarbonAwareExecution).

## Web API

### Azure Functions CI/CD

The GitHub Actions workflow `.github/workflows/azure-functions.yml` runs on pushes to every branch except `main`. It restores, builds and tests the solution in Release mode, then publishes one ZIP artifact per Function App:

| Project | Azure Function App |
| --- | --- |
| `CarbonAwareComputing.ExecutionForecast.Function` | `CarbonAwareComputingExecutionForecast` |
| `CarbonAwareComputing.ForecastUpdater.Function` | `CarbonAwareComputingForecastUpdaterFunction` |
| `CarbonAwareComputing.GridCarbonIntensity.Function` | `CarbonAwareComputingGridCarbonIntensity` |

All apps use resource group `Carbon-Aware-Computing`, Linux plan `CarbonAwareComputingAppServicePlan` and an existing slot named `Staging`. The workflow deploys all three packages to those slots. After a successful manual check and approval of the GitHub environment `production`, it swaps the tested slots into production without rebuilding.

Before enabling deployments:

- Create GitHub environments named `staging` and `production`. Configure the existing reviewers as **required reviewers on `production`**, with administrator bypass disabled where available. Merely assigning PR reviewers does not protect production deployments. Allow the intended non-`main` branches in both environments' deployment rules.
- Provide `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` as GitHub secrets or variables, either repository-wide or in both environments. Secrets take precedence over variables. Use the target subscription's UUID for `AZURE_SUBSCRIPTION_ID`.
- Configure Azure OIDC federated credentials for both environment subjects: `repo:bluehands/Carbon-Aware-Computing:environment:staging` and `repo:bluehands/Carbon-Aware-Computing:environment:production`. Existing branch-based OIDC credentials alone do not cover these jobs.
- Grant the deployment identity permission to deploy ZIP packages, read slot details, read/update slot resource tags and swap slots on the three apps. Ensure the slots, deployment endpoint access and matching runtime configuration exist. Disable remote build in the slots' deployment configuration; the workflow deploys already-published packages.
- Separate Staging storage, queues and other data sources from production, and mark environment-specific settings as deployment-slot settings so they stay with their slot during swaps. Disable side-effecting background triggers in Staging where appropriate; check their behavior during swap warm-up as well.
- The projects currently target .NET 6 in-process / Azure Functions v4. The workflow installs .NET 8 for the C# 12 compiler features used by the shared library, and .NET 6 to run the existing tests; this does not change the deployment target. .NET 6 is out of support; confirm Azure runtime compatibility before deployment and plan a separate runtime migration.

The Staging job summary contains the deployed commit and links for manual inspection. Review all three apps, including the ForecastUpdater's background processing, before approving production.

A workflow-wide concurrency group serializes releases across branches, including the approval wait, to prevent another run from replacing the shared Staging slots during review. Reject or cancel an unwanted release to unblock the next one. GitHub may replace an older pending run with a newer pending run; not every push is guaranteed to deploy. Do not deploy to or swap these slots outside this workflow while a release is awaiting approval.

Each Staging slot has a `cac-release` resource tag identifying the workflow run, attempt and commit. Production checks all three tags against the approved release and consumes all markers before starting any swap. This blocks stale production-job reruns and prevents a retry from swapping the previous production version back. Do not edit these tags manually.

The three swaps are sequential, not atomic. If a swap fails, inspect the job summary and Azure slot state before recovering the remaining apps. Once swap markers are consumed, automatic production-job retries deliberately fail closed, even if no swap completed. Start a fresh full deployment and review, or have an operator recover the slots after checking their state. After a successful swap, Staging contains the previous production version until the next deployment.

We provide a live and ready to use subset of the Carbon Aware SDK. The API is available from this location: [https://forecast.carbon-aware-computing.com/](https://forecast.carbon-aware-computing.com/). Use the Swagger UI [https://forecast.carbon-aware-computing.com/swagger/UI](https://forecast.carbon-aware-computing.com/swagger/UI) to play around with the API.

We also provide an endpoint to get the actual grid carbon intensity. The API is available from this location: [https://intensity.carbon-aware-computing.com/](https://intensity.carbon-aware-computing.com/). Use the Swagger UI [https://intensity.carbon-aware-computing.com/swagger/UI](https://intensity.carbon-aware-computing.com/swagger/UI) to play around with the API.

### Registration

To use the API, you must register to the service submitting a valid eMail-Address. Please check the *register* endpoint in the Swagger UI. The API-Key is send to this email. We will use this address only to inform you about important changes to the service. The registration is suitable for both API (Execution Forecast & Grid Carbon Intensity)

``` powershell
curl -X POST "https://forecast.carbon-aware-computing.com/register" -H  "accept: */*" -H  "Content-Type: application/json" -d "{\"mailAddress\":\"someone@example.com\"}"
```

### Subset of endpoints & data

We want to support the time-shifting functionality of the SDK and provide only the forecast endpoint for given locations. There are no historically data and the forecast data has only the *optimalDataPoints* collection set. The *emissionsDataPoints* with all forecast data is not set due to data efficiency. If you need the forecast data download it directly.

## Carbon Aware SDK as NuGet-Package

We have fork the Carbon Aware SDK [https://github.com/bluehands/carbon-aware-sdk](https://github.com/bluehands/carbon-aware-sdk) and provide the SDK as a NuGet-Package. The fork has also some modifications for cached data provider. You may use this package for your extensions.

### Installation

The unofficial Carbon Aware SDK is available form [nuget.org](https://www.nuget.org/packages/GSF.CarbonAware.Unofficial). Install it using the Package Manager Console window:

``` powershell
Install-Package GSF.CarbonAware.Unofficial
```

## PowerShell Cmdlets

There is a PowerShell Cmdlets to forecast the best execution time. You may use this Cmdlets in automation scripts to execute tasks with carbon awareness. See the [GitHub Repository](https://github.com/bluehands/Carbon-Aware-Computing-Cmdlets) for more details.

### Installation

``` powershell
Install-Module -Name CarbonAwareComputing
```
### Usage

``` powershell
$now=get-date
Get-CarbonAwareExecutionTime -Location at -EarliestExecutionTime $now -LatestExecutionTime ($now).AddHours(10) -EstimatedExecutionDuration "00:10:00"
```

Set the *FallbackExecutionTime* Parameter to set the execution time when no forecast is available. This command is designed to be used in scripts, therefor no errors are thrown.

## Prometheus metrics for grid carbon intensity

There is a Kubernetes Prometheus metrics exporter to report the actual grid carbon intensity. Use this exporter to calculate the carbon emission based on electricity power. This exporter may be combined with data from [kepler](https://sustainable-computing.io/) or [Scaphandre](https://github.com/hubblo-org/scaphandre).  

See the [GitHub repository](https://github.com/bluehands/kubernetes-grid-carbon-intensity-metrics-exporter) for usage and installation.

## Kubernetes grid carbon exporter

To support the [Carbon Aware KEDA Operator](https://github.com/Azure/carbon-aware-keda-operator) a compatible grid carbon exporter is provided. The data is exported in to a k8s configmap to adjust the number of scaled nodes. See the [GitHub repository](https://github.com/bluehands/kubernetes-grid-carbon-intensity-configmap-exporter) for usage and installation.

## Data

The forecast data for Europe (without UK) is gathered from [Energy Charts](https://www.energy-charts.info/) provided by [Frauenhofer ISE](https://www.ise.fraunhofer.de/). It is licensed as CC 0 <https://creativecommons.org/publicdomain/zero/1.0/>. You may use it for any purpose without any credits.

The forecast data for United Kingdom is gathered from [UK National Grid ESO](https://carbonintensity.org.uk/). It is licensed as [CC BY 4.0 DEED](https://creativecommons.org/licenses/by/4.0/). See [terms of usage](https://github.com/carbon-intensity/terms/).

### Download

The forecast data is available as json formatted files for every location. The files are directly consumable by the Carbon Aware SDK. Download is publicly available from a Azure Blob Storage.

``` powershell
curl -X GET "https://carbonawarecomputing.blob.core.windows.net/forecasts/{LOCATION}.json" -H  "accept: application/json"
```

Replace the {LOCATION} with one of the supported locations two letter country code (e.g. "de").

### Available and supported locations

We support the most countries in Europe, but not all are active. For computing efficiency we start with a Germany, France, Austria, Switzerland and the United Kingdom. If you have a need for some other countries please contact us. We will activate that country. To get a list of all locations see the *locations* endpoint of the API. Every location has a IsActive-Flag. 

``` powershell
curl -X GET "https://forecast.carbon-aware-computing.com/locations" -H  "accept: application/json"
```

### Methodology

The forecast data for Europe (without UK) is based on reported energy production (current) and forecast production for Wind (on-shore & off-shore) and Solar. This information's are send to the ENTSO-E Transparency Platform by the power grid Transmission System Operators (TSO). For the additional renewable energy sources like running water, bio mass the forecast is calculated as an interpolation of the last hours. After that the carbon intensity is calculated by the emission factor for every energy source. This forecast is very accurate because it is used by the TSO to manage the power grid. The data is recalculated every hour by *Energy Charts*. The forecast for next day is available at 19:00+01.

### Fallback

For regions we not support (e.g. US, Asia), a **WattTime**-DataProvider is implemented. For that you must provide your WattTime Credentials.

## Contribution

Every contribution is warmly welcome. You may contribute to forecast data for other regions than Europe or help to integrate time-shifting in popular processing systems and libraries. Migration to other programming languages and runtime systems is very efficacious as well.

### Contact

Please drop a message to

Aydin Mir Mohammadi  
[am@bluehands.de](mailto:am@bluehands.de?subject=[GitHub]%20Carbon%20Aware%20Computing)

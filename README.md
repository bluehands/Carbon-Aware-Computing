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

### Azure Functions infrastructure deployment

Each Function project has its own Azure Bicep deployment and variable file:

| Project | Deployment | Variables |
| --- | --- | --- |
| `CarbonAwareComputing.ExecutionForecast.Function` | `deploy/cac-execution-forecast.bicep` | `deploy/cac-execution-forecast.bicepparam` |
| `CarbonAwareComputing.GridCarbonIntensity.Function` | `deploy/cac-grid-carbon-intensity.bicep` | `deploy/cac-grid-carbon-intensity.bicepparam` |
| `CarbonAwareComputing.ForecastUpdater.Function` | `deploy/cac-forecast-updater.bicep` | `deploy/cac-forecast-updater.bicepparam` |

Each deployment creates a **Windows Consumption plan (Y1/Dynamic)**, a .NET 10
isolated / Functions v4 app with a system-assigned managed identity, a private
host Storage Account including an Azure Files content share, and workspace-based
Application Insights. Each app also gets a deployment slot named `Staging` on the
same plan, with its own system-assigned identity, host Storage Account and content
share. Both slots use the same .NET runtime and Application Insights configuration.
Host storage connections and Staging-specific settings stay with their slot during
swaps. Azure manages the content share setting's swap behavior automatically.
The shared module keeps the runtime configuration consistent.
These templates provision infrastructure and settings, not application code.
They are intended for new apps, not an in-place conversion of
existing Linux apps/plans. Classic Linux Consumption does not support .NET 10.

Use Azure CLI 2.76+ with a current Bicep CLI (`az bicep upgrade`). Edit each
`.bicepparam` file to set globally unique Function App names (2-50 characters,
letters, digits and hyphens), location, tags and, for the updater, the existing
forecast storage resource group. The deployment subscription must contain the
`carbonawarecomputing` Storage Account, its `forecasts` Blob container and its
`forecasts` table. Their names are fixed in the updater code. The updater template
grants both its production and Staging managed identities Storage Blob Data Contributor on that container and
Storage Table Data Contributor on that table; it does not create or alter the
forecast data store or its public read configuration. The deploying identity needs
resource deployment permissions and permission to create those role assignments
in the existing storage resource group. RBAC propagation can take several minutes.

Supply `CAC_MAIL_FROM`, `CAC_TENANT_ID`, `CAC_CLIENT_ID` and `CAC_CLIENT_SECRET`
through your shell environment or CI secret store. These configure the existing
Microsoft Graph mail application, which needs the application permission
`Mail.Send`, administrator consent and access to the sender mailbox. Also supply
`CAC_API_KEY_PASSWORD` for the two APIs; use the **same password** for both so
registered keys remain interchangeable. Do not put secrets in variable files.
The secret parameters are marked secure; compiled parameter JSON still contains
resolved secrets and must not be committed or retained in artifacts.

From the repository root, after configuring the variables and logging in:

``` bash
az login
az account set --subscription "<subscription-id>"
az group create --name Carbon-Aware-Computing --location westeurope

for deployment in cac-execution-forecast cac-grid-carbon-intensity cac-forecast-updater; do
  az deployment group what-if \
    --resource-group Carbon-Aware-Computing \
    --parameters "deploy/$deployment.bicepparam"
done
```

Review the proposed changes, then provision each app:

``` bash
for deployment in cac-execution-forecast cac-grid-carbon-intensity cac-forecast-updater; do
  az deployment group create \
    --name "$deployment" \
    --resource-group Carbon-Aware-Computing \
    --parameters "deploy/$deployment.bicepparam"
done
```

Publish and deploy each project's code separately, using the app name from its
variable file (or the deployment's `functionAppName` output). For example:

``` bash
dotnet publish src/CarbonAwareComputing.ExecutionForecast.Function \
  --configuration Release --output /tmp/cac-execution-forecast
(cd /tmp/cac-execution-forecast && zip -qr /tmp/cac-execution-forecast.zip .)
az functionapp deployment source config-zip \
  --resource-group Carbon-Aware-Computing \
  --name "<execution-forecast-app-name>" \
  --src /tmp/cac-execution-forecast.zip --build-remote false
```

Repeat for GridCarbonIntensity and ForecastUpdater with separate output directories
and ZIP files. ZIP roots must contain `host.json` and the published worker files.
The apps run prebuilt packages (`WEBSITE_RUN_FROM_PACKAGE=1`).
To deploy a package to Staging instead of production, add `--slot Staging` to the
`az functionapp deployment source config-zip` command. Each template also exposes
`stagingSlotName` and `stagingSlotUrl` outputs. No automatic slot swap is configured.
The updater's timers are active after code deployment and write to the existing
shared forecast store; schedules use UTC. Set `writeHistoryFor` in its variable
file to a comma-separated list of location names to retain history.
In Staging, `ScheduledUpdateForecast` and `ScheduledReportForecast` are disabled
using slot-specific settings to prevent duplicate updates and reports. Manual
updater HTTP triggers still access the shared forecast store.
### Manual infrastructure and Functions deployment

The separate workflow `.github/workflows/azure-functions-infrastructure.yml`
is triggered only via `workflow_dispatch`. It builds and tests the solution using
.NET 10 (and .NET 6 for the existing tests), packages all three Functions, validates
all three Bicep parameter files, then provisions and deploys each app. App names
are read from the Bicep deployment outputs, not duplicated in the workflow.
The existing push/Staging workflow is unchanged.

The deployment uses the existing GitHub environment `dev`, resource group
`Carbon-Aware-Computing` and the names/location/tags in `deploy/*.bicepparam`.
Adjust those non-secret values before running if necessary. The resource group
is created in `westeurope` if absent. This deploys directly to the new Windows
Consumption apps' production slots, not to their newly provisioned Staging slots
or the existing Linux apps.

Reuse the existing authentication names `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`
and `AZURE_SUBSCRIPTION_ID` as repository or `dev` environment secrets/variables
(secrets take precedence). These identify the Azure deployment identity, not
the Microsoft Graph mail application. Configure an OIDC federated credential
with subject `repo:bluehands/Carbon-Aware-Computing:environment:dev`.
The identity needs resource group creation/deployment, ZIP deployment and role
assignment permissions on the existing forecast storage resource group.

Create the following GitHub Actions secrets in the repository or `dev`
environment if they are not already present:

| Secret | Value |
| --- | --- |
| `CAC_MAIL_FROM` | Sender mailbox for registration/notification mail |
| `CAC_TENANT_ID` | Microsoft Graph mail application's tenant ID |
| `CAC_CLIENT_ID` | Microsoft Graph mail application's client ID |
| `CAC_CLIENT_SECRET` | Microsoft Graph mail application's client secret |
| `CAC_API_KEY_PASSWORD` | Shared API-key password for both APIs |

The workflow passes these secrets as environment variables to the existing
`readEnvironmentVariable(...)` calls in the parameter files. It fails explicitly
on missing configuration and does not upload resolved parameter files or print
deployment settings. The secret inventory could not be verified from the coding
session (Actions secrets API returned HTTP 403), so this is a required-secret
list, not a confirmed list of missing secrets.

After merging the workflow into the default branch, open **Actions > Deploy Azure
Functions and infrastructure > Run workflow** and select the branch containing
the .NET 10 projects and Bicep files. Ensure `dev` deployment branch rules permit
that branch; configure environment approval rules if desired. Starting the
workflow provisions Azure resources and deploys code; ForecastUpdater timers
become active and write to the existing shared forecast store. The run summary
contains the deployed commit and app URLs. No Azure deployment is performed
merely by adding this workflow.

### Azure Functions CI/CD

The GitHub Actions workflow `.github/workflows/azure-functions.yml` runs on pushes to every branch except `main`. It restores, builds and tests the solution in Release mode, then publishes one ZIP artifact per Function App:

| Project | Azure Function App |
| --- | --- |
| `CarbonAwareComputing.ExecutionForecast.Function` | `CarbonAwareComputingExecutionForecast` |
| `CarbonAwareComputing.ForecastUpdater.Function` | `CarbonAwareComputingForecastUpdaterFunction` |
| `CarbonAwareComputing.GridCarbonIntensity.Function` | `CarbonAwareComputingGridCarbonIntensity` |

All apps use resource group `Carbon-Aware-Computing`, Linux plan `CarbonAwareComputingAppServicePlan` and an existing slot named `Staging`. The workflow deploys all three packages to those slots only. It does not deploy to production or swap slots.

Before enabling deployments:

- Create a GitHub environment named `staging`. Allow the intended non-`main` branches in its deployment rules.
- Provide `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` as GitHub secrets or variables, either repository-wide or in the `staging` environment. Secrets take precedence over variables. Use the target subscription's UUID for `AZURE_SUBSCRIPTION_ID`.
- Configure an Azure OIDC federated credential for the `staging` environment subject presented by GitHub. Existing branch-based OIDC credentials alone do not cover this job.
- Grant the deployment identity permission to deploy ZIP packages and read slot details on the three apps. Ensure the slots, deployment endpoint access and matching runtime configuration exist. Disable remote build in the slots' deployment configuration; the workflow deploys already-published packages.
- Separate Staging storage, queues and other data sources from production, and mark environment-specific settings as deployment-slot settings. Disable side-effecting background triggers in Staging where appropriate.
- The projects currently target .NET 6 in-process / Azure Functions v4. The workflow installs .NET 8 for the C# 12 compiler features used by the shared library, and .NET 6 to run the existing tests; this does not change the deployment target. .NET 6 is out of support; confirm Azure runtime compatibility before deployment and plan a separate runtime migration.

The Staging job summary contains the deployed commit and links for manual inspection. Review all three apps, including the ForecastUpdater's background processing.

A workflow-wide concurrency group serializes deployments across branches to prevent simultaneous updates to the shared Staging slots. GitHub may replace an older pending run with a newer pending run; not every push is guaranteed to deploy. A subsequent deployment can replace the version under manual inspection.

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

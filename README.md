# Carbon Aware Computing

**Execute computing tasks when the grid is powered by renewable energy**

The goal of this project is to provide developers with hassle free, easy to use, ready to run tools for carbon aware computing. All libraries and data are open source and open data with unrestricted usage. Within private, open source and commercial software.

## time-shifting, demand-shifting

The basic idea behind the time-shifting approach is to move the computing load in to a point in time, when the power grid has a maximum of renewable energy. This will result in a lower emission of CO2 of your computing task.

This project will deliver a set of libraries, services and data. There are mostly extensions to other projects and all credits belong to them. To forecast the best execution time the [Carbon Aware SDK](https://github.com/Green-Software-Foundation/carbon-aware-sdk) from the [Green Software Foundation](https://greensoftware.foundation/) is used. The Forecast data is from [Energy Charts](https://www.energy-charts.info/) provided by [Frauenhofer ISE](https://www.ise.fraunhofer.de/).

## Get the best execution time as library

Use the nuget-package for your .NET project to get the best execution time for a task, in a given Execution-Window for a estimated duration. The lib will calculate the optimal execution time within the provided forecast data.

### Installation

Just add the package to your project.

``` powershell
Install-Package CarbonAwareComputing.ExecutionForecast 
```

### Usage

Instantiate a *CarbonAwareDataProvider* and call *CalculateBestExecutionTime*

``` csharp
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

The *CarbonAwareDataProviderOnlineForecastFile* has a cache of all forecasts. To improve performance use it as a singleton, to avoid multiple downloads. For a list of locations see below.

## Hangfire Extension

Hangfire is one of the most used tools for background processing in .NET. Use the *[Hangfire.CarbonAwareExecution](https://github.com/bluehands/Hangfire.CarbonAwareExecution)* Extension to enqueue and schedule your jobs.

### Installation

HangfireCarbonAwareExecution is available as a NuGet package. You can install it using the NuGet Package Console window:

``` powershell
Install-Package Hangfire.CarbonAwareExecution
```

After installation add the extension to the Hangfire configuration.

``` csharp
builder.Services.AddHangfire(configuration => configuration
    .UseCarbonAwareExecution(new CarbonAwareDataProviderBuildIn(ComputingLocations.Germany))
);
```

### Usage

There are extension to **Enqueue** and **Schedule** with *WithCarbonAwarenessAsync*. For more details check the [GitHub Repository](https://github.com/bluehands/Hangfire.CarbonAwareExecution).

## Web API

We provide a live and ready to use subset of the Carbon Aware SDK. The API is available from this location: [https://forecast.carbon-aware-computing.com/](https://forecast.carbon-aware-computing.com/). Use the Swagger UI [https://forecast.carbon-aware-computing.com/swagger/UI](https://forecast.carbon-aware-computing.com/swagger/UI) to play around with the API.

### Registration

To use the API, you have to register to the service submitting a valid eMail-Address. Please check the *register* endpoint in the Swagger UI. The API-Key is send to this email. We will use this address only to inform you about important changes to the service.

``` powershell
curl -X POST "https://forecast.carbon-aware-computing.com/register" -H  "accept: */*" -H  "Content-Type: application/json" -d "{\"mailAddress\":\"someone@example.com\"}"
```

### Subset of endpoints & data

We want to support the time-shifting functionality of the SDK and provide only the forecast endpoint for given locations. There are no historically data and the forecast data has only the *optimalDataPoints* collection set. The *emissionsDataPoints* with all forecast data is not set due to data efficiency. If you need the forecast data download it directly.

## Carbon Aware SDK as Library

We have fork the Carbon Aware SDK [https://github.com/bluehands/carbon-aware-sdk](https://github.com/bluehands/carbon-aware-sdk) and provide the SKD as a nuget-Package. The fork has also some modifications for cached data provider. You may use this package for your extensions.

### Installation

The unofficial Carbon Aware SDK is available as a NuGet package. You can install it using the NuGet Package Console window:

``` powershell
Install-Package GSF.CarbonAware.Unofficial
```

## Data

The forecast data is gathered from [Energy Charts](https://www.energy-charts.info/) provided by [Frauenhofer ISE](https://www.ise.fraunhofer.de/). It is licensed as CC 0 <https://creativecommons.org/publicdomain/zero/1.0/>. You may use it for any purpose without any credits.

### Download

The forecast data is available as json formatted files for every location. The files are directly consumable by the Carbon Aware SDK. Download is publicly available from a Azure Blob Storage.

``` powershell
curl -X GET "https://carbonawarecomputing.blob.core.windows.net/forecasts/{LOCATION}.json" -H  "accept: application/json"
```

Replace the {LOCATION} with one of the supported locations.

### Available and supported locations

We support the most countries in Europe, but not all are active. For computing efficiency we start with a Germany, France, Austria and Switzerland. If you have a need for some other countries please contact us. We will activate that country. To get a list of all locations see the *locations* endpoint of the API. Every location has a IsActive-Flag. 

``` powershell
curl -X GET "https://forecast.carbon-aware-computing.com/locations" -H  "accept: application/json"
```

### Methodology

The forecast data is based on reported energy production (current) and forecast production for Wind (on-shore & off-shore) and Solar. This information's are send to the ENTSO-E Transparency Platform by the power grid Transmission System Operators (TSO). For the additional renewable energy sources like running water, bio mass the forecast is calculated as an interpolation of the last hours. After that the share of renewable energy is calculated as the quotient of generated renewable energy to all generated energy. This forecast is very accurate because it is used by the TSO to manage the power grid. The data is recalculated every hour by *Energy Charts*. The forecast for next day is available at 19:00+02.

In future *Energy Charts* will provide mid term forecast based on weather forecast and on calculations as well.

## Contribution

Every contribution is warmly welcome. You may contribute to forecast data for other regions than Europe or help to integrate time-shifting in other processing systems and libraries.

### Contact

Please drop a message to

Aydin Mir Mohammadi  
[am@bluehands.de](mailto:am@bluehands.de?subject=[GitHub]%20Carbon%20Aware%20Computing)
using FunicularSwitch.Generators;

// ReSharper disable InconsistentNaming
namespace CarbonAwareComputing;
public abstract class CarbonAwareDataProvider
{
    public abstract Task<ExecutionTime> CalculateBestExecutionTime(ComputingLocation location, DateTimeOffset earliestExecutionTime, DateTimeOffset jobShouldBeFinishedAt, TimeSpan estimatedJobDuration);
    public abstract Task<GridCarbonIntensity> GetCarbonIntensity(ComputingLocation location, DateTimeOffset now);
}

[UnionType(CaseOrder = CaseOrder.Explicit)]
public abstract partial record ExecutionTime
{
    public static readonly ExecutionTime NoForecast = new NoForecast_();

    [UnionCase(1)]
    public record NoForecast_() : ExecutionTime;

    [UnionCase(2)]
    public record BestExecutionTime_(
        DateTimeOffset ExecutionTime,
        TimeSpan Duration,
        double CarbonIntensity,
        double CarbonIntensityAtEarliestExecutionTime)
        : ExecutionTime;
}

[UnionType(CaseOrder = CaseOrder.Explicit)]
public abstract partial record GridCarbonIntensity
{
    public static readonly GridCarbonIntensity NoData = new NoData_();

    [UnionCase(1)]
    public record NoData_ : GridCarbonIntensity;

    [UnionCase(2)]
    public record EmissionData_(string Location, DateTimeOffset Time, double Value) : GridCarbonIntensity;

    public EmissionData_? GetValueOrDefault() => this.Match<EmissionData_?>(noData: _ => null, emissionData: e => e);
}
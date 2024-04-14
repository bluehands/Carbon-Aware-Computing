using FunicularSwitch.Generators;

// ReSharper disable InconsistentNaming
namespace CarbonAwareComputing;
public abstract class CarbonAwareDataProvider
{
    public abstract Task<ExecutionTime> CalculateBestExecutionTime(ComputingLocation location, DateTimeOffset earliestExecutionTime, DateTimeOffset latestExecutionTime, TimeSpan estimatedJobDuration);
    public abstract Task<GridCarbonIntensity> GetCarbonIntensity(ComputingLocation location, DateTimeOffset now);
}

public abstract class ExecutionTime
{
    public static readonly ExecutionTime NoForecast = new NoForecast_();
    public static ExecutionTime BestExecutionTime(DateTimeOffset bestExecutionTime, TimeSpan duration, double rating) => new BestExecutionTime_(bestExecutionTime, duration, rating);
    public class NoForecast_ : ExecutionTime
    {
        public NoForecast_() : base(UnionCases.NoForecast)
        {
        }
    }

    public class BestExecutionTime_ : ExecutionTime
    {
        public DateTimeOffset ExecutionTime { get; }
        public TimeSpan Duration { get; }
        public double Rating { get; }

        public BestExecutionTime_(DateTimeOffset bestExecutionTime, TimeSpan duration, double rating) : base(UnionCases.BestExecutionTime)
        {
            ExecutionTime = bestExecutionTime;
            Duration = duration;
            Rating = rating;
        }
    }

    internal enum UnionCases
    {
        NoForecast,
        BestExecutionTime
    }

    internal UnionCases UnionCase { get; }

    ExecutionTime(UnionCases unionCase) => UnionCase = unionCase;
    public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
    bool Equals(ExecutionTime other) => UnionCase == other.UnionCase;
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((ExecutionTime)obj);
    }

    public override int GetHashCode() => (int)UnionCase;
}
public static class ExecutionTimeExtension
{
    public static T Match<T>(this ExecutionTime executionTime, Func<ExecutionTime.NoForecast_, T> noForecast, Func<ExecutionTime.BestExecutionTime_, T> bestExecutionTime)
    {
        switch (executionTime.UnionCase)
        {
            case ExecutionTime.UnionCases.NoForecast:
                return noForecast((ExecutionTime.NoForecast_)executionTime);
            case ExecutionTime.UnionCases.BestExecutionTime:
                return bestExecutionTime((ExecutionTime.BestExecutionTime_)executionTime);
            default:
                throw new ArgumentException($"Unknown type derived from ExecutionTime: {executionTime.GetType().Name}");
        }
    }

    public static async Task<T> Match<T>(this ExecutionTime executionTime, Func<ExecutionTime.NoForecast_, Task<T>> noForecast, Func<ExecutionTime.BestExecutionTime_, Task<T>> bestExecutionTime)
    {
        switch (executionTime.UnionCase)
        {
            case ExecutionTime.UnionCases.NoForecast:
                return await noForecast((ExecutionTime.NoForecast_)executionTime).ConfigureAwait(false);
            case ExecutionTime.UnionCases.BestExecutionTime:
                return await bestExecutionTime((ExecutionTime.BestExecutionTime_)executionTime).ConfigureAwait(false);
            default:
                throw new ArgumentException($"Unknown type derived from ExecutionTime: {executionTime.GetType().Name}");
        }
    }

    public static async Task<T> Match<T>(this Task<ExecutionTime> executionTime, Func<ExecutionTime.NoForecast_, T> noForecast, Func<ExecutionTime.BestExecutionTime_, T> bestExecutionTime) => (await executionTime.ConfigureAwait(false)).Match(noForecast, bestExecutionTime);
    public static async Task<T> Match<T>(this Task<ExecutionTime> executionTime, Func<ExecutionTime.NoForecast_, Task<T>> noForecast, Func<ExecutionTime.BestExecutionTime_, Task<T>> bestExecutionTime) => await (await executionTime.ConfigureAwait(false)).Match(noForecast, bestExecutionTime).ConfigureAwait(false);
}

[UnionType(StaticFactoryMethods = true)]
public abstract partial record GridCarbonIntensity;
public record NoData : GridCarbonIntensity;
public record EmissionData(string Location, DateTimeOffset Time, double Value) : GridCarbonIntensity;




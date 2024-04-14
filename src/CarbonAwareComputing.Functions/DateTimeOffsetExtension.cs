namespace CarbonAwareComputing.ExecutionForecast.Function;

public static class DateTimeOffsetExtension
{
    public static DateTimeOffset PadSeconds(this DateTimeOffset date)
    {
        return new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second,date.Offset);
    }
}
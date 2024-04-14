using Microsoft.Extensions.Primitives;

namespace CarbonAwareComputing.Functions;

public static class StringValuesExtension
{
    public static bool TryGetDateTimeOffset(this StringValues values, DateTimeOffset defaultDate, out DateTimeOffset date)
    {
        var d = values.FirstOrDefault();
        if (d != null)
        {
            return DateTimeOffset.TryParse(d, out date);
        }

        date = defaultDate;
        return true;
    }
    public static bool TryGetInt(this StringValues values, int defaultData, out int data)
    {
        var d = values.FirstOrDefault();
        if (d != null)
        {
            return Int32.TryParse(d, out data);
        }

        data = defaultData;
        return true;
    }
}
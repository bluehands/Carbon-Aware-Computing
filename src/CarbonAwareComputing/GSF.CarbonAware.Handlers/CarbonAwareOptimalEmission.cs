namespace GSF.CarbonAware.Handlers;

internal static class CarbonAwareOptimalEmission
{
    public static IEnumerable<global::CarbonAware.Model.EmissionsData> GetOptimalEmissions(IEnumerable<global::CarbonAware.Model.EmissionsData> emissionsData)
    {
        if (!emissionsData.Any())
        {
            return Array.Empty<global::CarbonAware.Model.EmissionsData>();
        }

        var bestResult = emissionsData.MinBy(x => x.Rating);

        IEnumerable<global::CarbonAware.Model.EmissionsData> results = Array.Empty<global::CarbonAware.Model.EmissionsData>();

        if (bestResult != null)
        {
            results = emissionsData.Where(x => x.Rating == bestResult.Rating);
        }

        return results;
    }
}
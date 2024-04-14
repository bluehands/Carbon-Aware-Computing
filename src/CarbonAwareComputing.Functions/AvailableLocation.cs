namespace CarbonAwareComputing.ExecutionForecast.Function;

public class AvailableLocation
{
    public string Name { get; }
    public bool IsActive { get; }
    public AvailableLocation(ComputingLocation location)
    {
        if (location is CloudRegion cloudRegion)
        {
            Name = cloudRegion.Region;
            IsActive = cloudRegion.IsActive;
        }
        else
        {
            Name = location.Name;
            IsActive = location.IsActive;
        }
    }
}
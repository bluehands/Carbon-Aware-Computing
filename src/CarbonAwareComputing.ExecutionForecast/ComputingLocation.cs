namespace CarbonAwareComputing.ExecutionForecast;

public static class ComputingLocations
{
    public static ComputingLocation Germany { get; } = new("de");
    public static ComputingLocation Switzerland { get; } = new("ch");
    public static ComputingLocation France { get; } = new("fr");
    public static ComputingLocation Austria { get; } = new("at");

    public static List<ComputingLocation> All { get; } = new()
    {
        Germany,
        France,
        Austria,
        Switzerland,
        new InactiveComputingLocation("az"),
        new InactiveComputingLocation("ba"),
        new InactiveComputingLocation("be"),
        new InactiveComputingLocation("by"),
        new InactiveComputingLocation("cy"),
        new InactiveComputingLocation("cz"),
        new InactiveComputingLocation("dk"),
        new InactiveComputingLocation("ee"),
        new InactiveComputingLocation("es"),
        new InactiveComputingLocation("fi"),
        new InactiveComputingLocation("ge"),
        new InactiveComputingLocation("gr"),
        new InactiveComputingLocation("hr"),
        new InactiveComputingLocation("hu"),
        new InactiveComputingLocation("ie"),
        new InactiveComputingLocation("lt"),
        new InactiveComputingLocation("lu"),
        new InactiveComputingLocation("lv"),
        new InactiveComputingLocation("md"),
        new InactiveComputingLocation("me"),
        new InactiveComputingLocation("mk"),
        new InactiveComputingLocation("mt"),
        new InactiveComputingLocation("nie"),
        new InactiveComputingLocation("nl"),
        new InactiveComputingLocation("no"),
        new InactiveComputingLocation("pl"),
        new InactiveComputingLocation("pt"),
        new InactiveComputingLocation("ro"),
        new InactiveComputingLocation("rs"),
        new InactiveComputingLocation("ru"),
        new InactiveComputingLocation("se"),
        new InactiveComputingLocation("sl"),
        new InactiveComputingLocation("sk"),
        new InactiveComputingLocation("tr"),
        new InactiveComputingLocation("ua"),
        new InactiveComputingLocation("uk"),
        new InactiveComputingLocation("xk"),


    };
}
public record ComputingLocation
{
    public string Name { get; }
    public bool IsActive { get; }

    public ComputingLocation(string namedLocation) : this(namedLocation, true)
    {
    }
    protected ComputingLocation(string namedLocation, bool isActive)
    {
        Name = namedLocation.ToLowerInvariant();
        IsActive = isActive;
    }
}
public record InactiveComputingLocation : ComputingLocation
{
    public InactiveComputingLocation(string namedLocation) : base(namedLocation, false)
    {
    }
}
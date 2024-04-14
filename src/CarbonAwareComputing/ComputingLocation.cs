// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

using System.Diagnostics.CodeAnalysis;

namespace CarbonAwareComputing;

public static class ComputingLocations
{
    public static ComputingLocation Germany { get; } = new("de");
    public static ComputingLocation UnitedKingdom { get; } = new("uk", true, ForecastProvider.UKNationalGrid);
    public static ComputingLocation UnitedKingdomLondon { get; } = new("london", true, ForecastProvider.UKNationalGrid);
    public static ComputingLocation UnitedKingdomSouthWales { get; } = new("southwales", true, ForecastProvider.UKNationalGrid);
    public static CloudRegion Azure_Germany_WestCentral { get; } = new("germanywestcentral", "de");
    public static CloudRegion Azure_UK_South { get; } = new("uksouth", "london", ForecastProvider.UKNationalGrid);
    public static CloudRegion Azure_UK_West { get; } = new("ukwest", "southwales", ForecastProvider.UKNationalGrid);
    public static CloudRegion AWS_EU_Central1 { get; } = new("eu-central-1", "de");
    public static CloudRegion GCP_Europe_West3 { get; } = new("europe-west3", "de");
    public static ComputingLocation Switzerland { get; } = new("ch");
    public static CloudRegion Azure_Switzerland_North { get; } = new("switzerlandnorth", "ch");
    public static CloudRegion AWS_EU_Central2 { get; } = new("eu-central-2", "ch");
    public static CloudRegion GCP_Europe_West6 { get; } = new("europe-west6", "ch");
    public static ComputingLocation France { get; } = new("fr");
    public static CloudRegion Azure_France_Central { get; } = new("francecentral", "fr");
    public static CloudRegion GCP_Europe_West9 { get; } = new("europe-west9", "fr");
    public static CloudRegion AWS_EU_West3 { get; } = new("eu-west-3", "fr");
    public static ComputingLocation Austria { get; } = new("at");

    private static Dictionary<string, ComputingLocation> Active { get; } = new(StringComparer.InvariantCultureIgnoreCase)
    {
        {"Germany",Germany},
        {"France",France},
        {"Austria",Austria},
        {"Switzerland",Switzerland},
        {"UnitedKingdom",UnitedKingdom},
        {"London",UnitedKingdomLondon},
        {"SouthWales",UnitedKingdomSouthWales},
        {"GermanyWestCentral",Azure_Germany_WestCentral},
        {"SwitzerlandNorth",Azure_Switzerland_North},
        {"FranceCentral",Azure_France_Central},
        {"UKSouth",Azure_UK_South},
        {"UKWest",Azure_UK_West},
        {"eu-central-1",AWS_EU_Central1},
        {"eu-central-2",AWS_EU_Central2},
        {"eu-west-3",AWS_EU_West3},
        {"europe-west3",GCP_Europe_West3},
        {"europe-west6",GCP_Europe_West6},
        {"europe-west9",GCP_Europe_West9},
    };
    public static List<ComputingLocation> All { get; } = new()
    {
        Germany,
        France,
        Austria,
        Switzerland,
        UnitedKingdom,
        UnitedKingdomLondon,
        UnitedKingdomSouthWales,
        Azure_Germany_WestCentral,
        Azure_Switzerland_North,
        Azure_France_Central,
        Azure_UK_South,
        Azure_UK_West,
        AWS_EU_Central1,
        AWS_EU_Central2,
        AWS_EU_West3,
        GCP_Europe_West3,
        GCP_Europe_West6,
        GCP_Europe_West9,
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
        new InactiveComputingLocation("xk"),
        new InactiveComputingLocation("NorthScotland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("SouthScotland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("NorthWestEngland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("NorthEastEngland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("Yorkshire",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("NorthWales",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("WestMidlands",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("EastMidlands",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("EastEngland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("SouthWestEngland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("SouthEngland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("SouthEastEngland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("England",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("Scotland",ForecastProvider.UKNationalGrid),
        new InactiveComputingLocation("Wales",ForecastProvider.UKNationalGrid),
    };


    public static bool TryParse(string name, [NotNullWhen(true)] out ComputingLocation? location)
    {
        if (Active.TryGetValue(name, out location))
        {
            return true;
        }

        location = All.FirstOrDefault(i => i.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        return location != null;
    }
}
public record ComputingLocation
{
    public string Name { get; }
    public bool IsActive { get; }
    public ForecastProvider ForecastProvider { get; }

    public ComputingLocation(string namedLocation) : this(namedLocation, true, ForecastProvider.EnergyCharts)
    {
    }
    public ComputingLocation(string namedLocation, bool isActive, ForecastProvider provider)
    {
        Name = namedLocation.ToLowerInvariant();
        IsActive = isActive;
        ForecastProvider = provider;
    }
}
public record CloudRegion : ComputingLocation
{
    public string Region { get; }

    public CloudRegion(string region, string namedLocation) : this(region, namedLocation, ForecastProvider.EnergyCharts)
    {
    }
    public CloudRegion(string region, string namedLocation, ForecastProvider provider) : base(namedLocation, true, provider)
    {
        Region = region;
    }
}
public record InactiveComputingLocation : ComputingLocation
{
    public InactiveComputingLocation(string namedLocation) : this(namedLocation, ForecastProvider.EnergyCharts)
    {
    }
    public InactiveComputingLocation(string namedLocation, ForecastProvider provider) : base(namedLocation, false, provider)
    {
    }
}
public enum ForecastProvider
{
    EnergyCharts,
    UKNationalGrid
}
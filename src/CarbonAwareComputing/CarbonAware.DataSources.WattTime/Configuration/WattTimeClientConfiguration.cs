using CarbonAware.Exceptions;
using System.Text;

namespace CarbonAware.DataSources.WattTime.Configuration;

/// <summary>
/// A configuration class for holding WattTime client config values.
/// </summary>
internal class WattTimeClientConfiguration
{
    public const string Key = "WattTimeClient";

    /// <summary>
    /// Gets or sets the username to use when connecting to WattTime.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password to use when connecting to WattTime
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the base url to use when connecting to WattTime
    /// </summary>
    public string BaseUrl { get; set; } = "https://api2.watttime.org/v2/";

    /// <summary>
    /// Gets or sets the cached expiration time (in seconds) for a BalancingAuthority instance.
    /// It defaults to 86400 secs.
    /// </summary>
    public int BalancingAuthorityCacheTTL { get; set; } = 86400;

    /// <summary>
    /// Validate that this object is properly configured.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.Username))
        {
            throw new ConfigurationException($"{Key}:{nameof(this.Username)} is required for WattTime.");
        }

        if (string.IsNullOrWhiteSpace(this.Password))
        {
            throw new ConfigurationException($"{Key}:{nameof(this.Password)} is required for WattTime.");
        }

        if (!Uri.IsWellFormedUriString(this.BaseUrl, UriKind.Absolute))
        {
            throw new ConfigurationException($"{Key}:{nameof(this.BaseUrl)} is not a valid absolute url.");
        }

        // Validate credential encoding/decoding with UTF8
        if (!Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(this.Username)).Equals(this.Username))
        {
            throw new ConfigurationException($"{Key}:{nameof(this.Username)} failed to be encoded/decoded with UTF8.");
        }

        if (!Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(this.Password)).Equals(this.Password))
        {
            throw new ConfigurationException($"{Key}:{nameof(this.Password)} failed to be encoded/decoded with UTF8.");
        }
    }
}

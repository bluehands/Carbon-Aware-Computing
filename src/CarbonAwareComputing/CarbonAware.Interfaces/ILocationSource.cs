﻿using CarbonAware.Model;

namespace CarbonAware.Interfaces;

/// <summary>
/// Represents a location source for Location type.
/// </summary>
internal interface ILocationSource
{
    /// <summary>
    /// Converts given Location to a new Location with type Geoposition
    /// </summary>
    /// <param name="location">The location to be converted. </param>
    /// <returns>New location representing Geoposition information.</returns>
    public Task<Location> ToGeopositionLocationAsync(Location location);

    /// <summary>
    /// Retuns a dictionary with all Geoposition supported.
    /// </summary>
    /// <returns>New location representing Geoposition information.</returns>
    public Task<IDictionary<string, Location>> GetGeopositionLocationsAsync();
}

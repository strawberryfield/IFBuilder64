//-----------------------------------------------------------------------
// <copyright file="IExits.cs" company="Casasoft">
//     Author: Roberto Ceccarelli (http://strawberryfield.altervista.org)
//     Copyright (c) 2026 All rights reserved.
// </copyright>
//
// This file is part of Casasoft IFBuilder64
// https://github.com/strawberryfield/IFBuilder64
//
// Casasoft IFBuilder64 is free software:
// you can redistribute it and/or modify it
// under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Casasoft IFBuilder64 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
//-----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents the set of directional exits (links) for a location used in JSON serialization.
/// </summary>
/// <remarks>
/// Each property corresponds to a direction and holds an identifier (for example, a room ID
/// or name) that consumers can use to resolve the linked location. Properties are optional:
/// when a property is <c>null</c> it should be omitted from JSON (for example by applying
/// <see cref="JsonIgnoreAttribute"/> on the concrete implementation or by configuring the
/// serializer to ignore null values).
///
/// Direction semantics:
/// - <c>north</c>, <c>south</c>, <c>east</c>, <c>west</c> are horizontal directions.
/// - <c>up</c>, <c>down</c> are vertical directions.
///
/// Identifiers are intentionally opaque strings; the meaning (absolute ID, relative name,
/// path, etc.) is determined by the consuming code.
/// </remarks>
/// <example>
/// Example JSON for a room with two exits:
/// <code>
/// {
///   "north": "library",
///   "up": "attic"
/// }
/// </code>
/// </example>
public interface IExits
{
    /// <summary>
    /// Gets or sets the identifier of the location reached when going north through this door.
    /// </summary>
    /// <remarks>
    /// This value is a reference (for example, a room ID or name) used by consumers to
    /// link locations together. When <c>null</c>, the property is omitted from JSON
    /// due to the <see cref="JsonIgnoreAttribute"/> applied.
    /// </remarks>
    public string? north { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going south through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    public string? south { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going east through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    public string? east { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going west through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    public string? west { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going up through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    public string? up { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going down through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    public string? down { get; set; }

}

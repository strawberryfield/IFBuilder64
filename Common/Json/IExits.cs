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

namespace Casasoft.IFBuilder64.Json;

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

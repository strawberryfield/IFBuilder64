//-----------------------------------------------------------------------
// <copyright file="Door.cs" company="Casasoft">
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
/// Represents a door entry in the JSON schema used by IFBuilder64.
/// </summary>
/// <remarks>
/// A <see cref="Door"/> is a JSON-serializable description of a connection
/// between locations (for example, rooms) and contains metadata used by
/// the builder and runtime to determine how the connection behaves (locked,
/// open/closed state, directional targets, etc.).
/// </remarks>
public class Door : IExits
{
    /// <summary>
    /// Gets or sets the door's name.
    /// </summary>
    /// <remarks>
    /// This value is used as the primary human-readable identifier for the door.
    /// It is expected to be a short, descriptive string.
    /// </remarks>
    public required string name { get; set; }

    /// <summary>
    /// Gets or sets the door's description.
    /// </summary>
    /// <remarks>
    /// A longer, free-form text that describes the door. May contain punctuation
    /// and plain text suitable for display in UI or documentation.
    /// </remarks>
    public required string description { get; set; }

    /// <summary>
    /// Message to display when the door cannot be opened.
    /// </summary>
    /// <remarks>
    /// Null values are omitted from serialized JSON because of the
    /// <see cref="JsonIgnoreAttribute"/> applied to this member.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? denyopen { get; set; }

    /// <summary>
    /// Gets or sets the identifier of an item that can unlock or interact with this door.
    /// </summary>
    /// <remarks>
    /// If specified, this usually refers to an item required to unlock, open,
    /// or otherwise change the state of this item (for example, a key).
    /// Consumers should interpret the format according to the JSON schema used.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? unlockitem { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the door is currently open.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the door is considered open. When <c>false</c>,
    /// it is considered closed. A <c>null</c> value indicates no explicit state.
    /// The <see cref="Options"/> bitmask encodes the effective open state when serialized.
    /// </remarks>
    public bool? open { get; set; }

    /// <summary>
    /// Computes the serialized options bitmask for this door.
    /// </summary>
    /// <returns>
    /// A byte containing option flags:
    /// - bit 0 (0x01): set when <see cref="open"/> is <c>true</c>.
    /// Additional bits are reserved for future options.
    /// </returns>
    public byte Options()
    {
        byte options = 0;
        if (open == true) options |= 0x01;
        return options;
    }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going north through this door.
    /// </summary>
    /// <remarks>
    /// This value is a reference (for example, a room ID or name) used by consumers to
    /// link locations together. When <c>null</c>, the property is omitted from JSON
    /// due to the <see cref="JsonIgnoreAttribute"/> applied.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? north { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going south through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? south { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going east through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? east { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going west through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? west { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going up through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? up { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the location reached when going down through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. This property is optional and omitted when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? down { get; set; }

}

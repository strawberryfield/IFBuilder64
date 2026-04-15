//-----------------------------------------------------------------------
// <copyright file="Room.cs" company="Casasoft">
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
using System.ComponentModel;

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents a navigable room in the JSON model used by IFBuilder64.
/// </summary>
/// <remarks>
/// Instances of <see cref="Room"/> model a single location in the interactive
/// fiction world. Each room exposes a human-readable <see cref="description"/>
/// and a required <see cref="name"/> identifier. Exits are represented by string
/// properties (for example a numeric index, a symbolic identifier or a GUID),
/// one for each common direction. Exit properties are nullable: when serializing
/// to JSON, properties with <c>null</c> values are omitted due to the
/// <see cref="JsonIgnoreAttribute"/> configuration.
/// 
/// Notes:
/// - The concrete meaning of exit strings (index, id, or name) is determined by
///   the consuming code; this class treats them as opaque identifiers.
/// - The class uses C# 11 required properties for <see cref="name"/> and
///   <see cref="description"/>, indicating they must be present when creating
///   instances (e.g., during deserialization or initialization).
/// </remarks>
[Description("Represents a navigable room in the game's JSON model. Describes room metadata and directional exits.")]
public class Room : IExits
{
    /// <summary>
    /// Gets or sets the unique identifier or short name for the room.
    /// </summary>
    /// <value>
    /// A non-null <see cref="string"/> that identifies the room. This value is
    /// required for correct model construction and may be used as a lookup key
    /// by consumers.
    /// </value>
    [Description("Unique identifier or short name for the room.")]
    public required string name { get; set; }

    /// <summary>
    /// Gets or sets the human-readable description of the room.
    /// </summary>
    /// <value>
    /// A non-null <see cref="string"/> with the description shown to players.
    /// May be empty but should not be <c>null</c> where display text is expected.
    /// </value>
    [Description("Human-readable description of the room shown to players.")]
    public required string description { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the room reachable by going north.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing an identifier for the north exit,
    /// or <c>null</c> if no north exit is present. The identifier's format
    /// (numeric index, symbolic name, etc.) is defined by the consuming logic.
    /// </value>
    /// <remarks>
    /// When serializing the model to JSON, a <c>null</c> value will be omitted
    /// from the output because of the <see cref="JsonIgnoreCondition.WhenWritingNull"/>
    /// setting on the property.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going north.")]
    public string? north { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the room reachable by going south.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing an identifier for the south exit,
    /// or <c>null</c> if no south exit is present.
    /// </value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going south.")]
    public string? south { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the room reachable by going east.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing an identifier for the east exit,
    /// or <c>null</c> if no east exit is present.
    /// </value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going east.")]
    public string? east { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the room reachable by going west.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing an identifier for the west exit,
    /// or <c>null</c> if no west exit is present.
    /// </value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going west.")]
    public string? west { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the room reachable by going up.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing an identifier for the up exit,
    /// or <c>null</c> if no up exit is present.
    /// </value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going up.")]
    public string? up { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the room reachable by going down.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing an identifier for the down exit,
    /// or <c>null</c> if no down exit is present.
    /// </value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going down.")]
    public string? down { get; set; }

}

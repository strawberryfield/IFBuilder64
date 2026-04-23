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
using System.ComponentModel;

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents a door entry in the JSON schema used by IFBuilder64.
/// </summary>
/// <remarks>
/// A <see cref="Door"/> describes a navigable connection between locations (for example,
/// rooms) and carries metadata used by the builder and runtime to determine behavior:
/// whether the connection is open or closed, whether it is locked and what item unlocks it,
/// optional messages shown to the player, and explicit directional targets (north/south/etc.).
///
/// The class is designed to be JSON-serializable with System.Text.Json. Properties that are
/// optional and set to <c>null</c> will be omitted from serialized JSON because of the
/// <see cref="JsonIgnoreAttribute"/> settings applied where appropriate.
///
/// Typical usage:
/// - The builder produces a collection of <see cref="Door"/> instances describing map
///   connectivity.
/// - The runtime reads these instances and uses the directional properties (for example,
///   <see cref="north"/>, <see cref="south"/>) to resolve target locations when a player
///   attempts to move in that direction.
///
/// Example JSON representation (compact):
/// <![CDATA[
/// {
///   "name": "Oak Door",
///   "description": "A heavy oak door with iron bands.",
///   "denyopen": "The door is stuck.",
///   "unlockitem": "iron_key",
///   "open": false,
///   "north": "Hallway",
///   "south": "Library"
/// }
/// ]]>
/// </remarks>
[Description("Represents a door entry in the game's JSON model. Describes a connection between locations and related metadata.")]
public class Door : IExits
{
    /// <summary>
    /// Gets or sets the door's name.
    /// </summary>
    /// <remarks>
    /// This value is the primary human-readable identifier for the door. It should be a
    /// concise, localized string suitable for presenting to players or editors.
    /// </remarks>
    [Description("Primary human-readable identifier for the door.")]
    public required string name { get; set; }

    /// <summary>
    /// Gets or sets the door's description.
    /// </summary>
    /// <remarks>
    /// A longer, free-form text that describes the door. Intended for display in UI,
    /// tooltips or editor views. May include punctuation and plain text; avoid markup
    /// that the runtime will not understand unless explicitly supported.
    /// </remarks>
    [Description("A free-form text description of the door suitable for display in UI or documentation.")]
    public required string description { get; set; }

    /// <summary>
    /// Optional message displayed when an attempt to open the door is denied.
    /// </summary>
    /// <remarks>
    /// If present, this string should contain a player-facing message explaining why the
    /// open action failed (for example, locked or stuck). When <c>null</c> the property is
    /// omitted from serialized JSON.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional message displayed when the door cannot be opened.")]
    public string? denyopen { get; set; }

    /// <summary>
    /// Optional identifier of an item that can unlock or otherwise interact with this door.
    /// </summary>
    /// <remarks>
    /// Typically refers to an item ID (for example, a key) that the runtime can match
    /// against a player's inventory to allow unlocking or special interaction. The exact
    /// interpretation of the identifier depends on the game's item model.
    /// When <c>null</c> the property is omitted from serialized JSON.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of an item (e.g. a key) that can unlock or interact with this door.")]
    public string? unlockitem { get; set; }

    /// <summary>
    /// Indicates whether the door is currently open (<c>true</c>), closed (<c>false</c>),
    /// or unspecified (<c>null</c>).
    /// </summary>
    /// <remarks>
    /// A <c>null</c> value means there is no explicit open/closed state encoded on this
    /// object (the runtime may use defaults or computed state). When present, the value
    /// participates in generation of the serialized options bitmask (see <see cref="Options"/>).
    /// </remarks>
    [Description("Indicates whether the door is currently open (true), closed (false), or unspecified (null).")]
    public bool? open { get; set; }

    /// <summary>
    /// Computes the serialized options bitmask for this door.
    /// </summary>
    /// <returns>
    /// A byte containing option flags. Current bit assignments:
    /// - bit 0 (0x01): set when <see cref="open"/> is <c>true</c> (door is open).
    /// Additional bits are reserved for future options; consumers should ignore unknown bits.
    /// </returns>
    /// <remarks>
    /// The <see cref="Options"/> method centralizes the mapping from higher-level boolean
    /// state to the compact bitmask used in serialized formats. Callers that need to
    /// interpret the mask should test bit 0 to determine the open state.
    ///
    /// Example:
    /// <code>
    /// <![CDATA[
    /// byte mask = door.Options();
    /// bool isOpen = (mask & 0x01) != 0;
    /// ]]>
    /// </code>
    /// </remarks>
    public byte Options()
    {
        byte options = 0;
        if (open == true) options |= 0x01;
        return options;
    }

    /// <summary>
    /// Optional identifier of the location reached when going north through this door.
    /// </summary>
    /// <remarks>
    /// The value is a reference (for example, a room ID or name) that consumers use to
    /// link locations together. When <c>null</c> the property is omitted from serialized JSON.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going north through this door.")]
    public string? north { get; set; }

    /// <summary>
    /// Optional identifier of the location reached when going south through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics. Use whichever directional properties are
    /// appropriate for the connection being modeled.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going south through this door.")]
    public string? south { get; set; }

    /// <summary>
    /// Optional identifier of the location reached when going east through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going east through this door.")]
    public string? east { get; set; }

    /// <summary>
    /// Optional identifier of the location reached when going west through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going west through this door.")]
    public string? west { get; set; }

    /// <summary>
    /// Optional identifier of the location reached when going up through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics; useful for stairs, ladders or vertical connections.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going up through this door.")]
    public string? up { get; set; }

    /// <summary>
    /// Optional identifier of the location reached when going down through this door.
    /// </summary>
    /// <remarks>
    /// See <see cref="north"/> for semantics; useful for stairs, chutes or vertical connections.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Optional identifier of the location reached when going down through this door.")]
    public string? down { get; set; }

}

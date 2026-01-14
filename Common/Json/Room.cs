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

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents a room in the JSON model used by IFBuilder64.
/// </summary>
/// <remarks>
/// Each instance contains a textual description and up to seven directional exits.
/// Directional properties are stored as <see cref="byte"/> and typically encode
/// an index or identifier of the target room. A value of <c>0</c> commonly
/// indicates the absence of an exit, but callers should rely on consumer logic
/// for exact semantics.
/// </remarks>
public class Room
{
    /// <summary>
    /// Gets or sets the human-readable description of the room.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing the room description. May be empty but
    /// should not be <c>null</c> where consumers expect displayed text.
    /// </value>
    public required string description { get; set; }

    /// <summary>
    /// Gets or sets the index of the room to the north.
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the target room index for the north exit.
    /// </value>
    /// <remarks>
    /// A value of <c>0</c> typically means there is no north exit.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? north { get; set; }

    /// <summary>
    /// Gets or sets the index of the room to the south.
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the target room index for the south exit.
    /// </value>
    /// <remarks>
    /// A value of <c>0</c> typically means there is no south exit.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? south { get; set; }

    /// <summary>
    /// Gets or sets the index of the room to the east.
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the target room index for the east exit.
    /// </value>
    /// <remarks>
    /// A value of <c>0</c> typically means there is no east exit.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? east { get; set; }

    /// <summary>
    /// Gets or sets the index of the room to the west.
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the target room index for the west exit.
    /// </value>
    /// <remarks>
    /// A value of <c>0</c> typically means there is no west exit.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? west { get; set; }

    /// <summary>
    /// Gets or sets the index of the room above (up).
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the target room index for the up exit.
    /// </value>
    /// <remarks>
    /// A value of <c>0</c> typically means there is no up exit.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? up { get; set; }

    /// <summary>
    /// Gets or sets the index of the room below (down).
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the target room index for the down exit.
    /// </value>
    /// <remarks>
    /// A value of <c>0</c> typically means there is no down exit.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? down { get; set; }

    /// <summary>
    /// Returns the directional exit indices in a fixed order.
    /// </summary>
    /// <returns>
    /// A new <see cref="byte"/>[] with six elements containing the exit indices
    /// in the following order: north, south, east, west, up, down.
    /// Each element contains the index or identifier of the target room;
    /// a value of <c>0</c> typically indicates the absence of an exit and
    /// should be interpreted by callers according to their semantics.
    /// </returns>
    /// <remarks>
    /// This method allocates and returns a new array on each call. If callers
    /// need to avoid repeated allocations (for example in hot paths), they
    /// should reuse a buffer or implement an alternative accessor that writes
    /// into a provided span or array.
    /// </remarks>
    public byte[] Directions() => [north ?? 0, south ?? 0, east ?? 0, west ?? 0, up ?? 0, down ?? 0];
}

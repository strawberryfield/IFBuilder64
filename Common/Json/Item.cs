//-----------------------------------------------------------------------
// <copyright file="Item.cs" company="Casasoft">
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
/// Represents a serializable item used by the JSON model.
/// </summary>
/// <remarks>
/// Instances of this class are intended for (de)serialization of item data
/// exchanged with tools and files that consume the IFBuilder64 JSON schema.
/// Property names are preserved to match the JSON contract.
/// </remarks>
public class Item
{
    /// <summary>
    /// Gets or sets the item's name.
    /// </summary>
    /// <remarks>
    /// This value is used as the primary human-readable identifier for the item.
    /// It is expected to be a short, descriptive string.
    /// </remarks>
    public required string name { get; set; }

    /// <summary>
    /// Gets or sets the item's description.
    /// </summary>
    /// <remarks>
    /// A longer, free-form text that describes the item. May contain punctuation
    /// and plain text suitable for display in UI or documentation.
    /// </remarks>
    public required string description { get; set; }

    /// <summary>
    /// Gets or sets an optional description shown when the item is opened.
    /// </summary>
    /// <remarks>
    /// This text, when present, can be displayed to the player or consumer
    /// when the item's "open" action is performed. The property is nullable
    /// and will be omitted from serialized JSON when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? opendescription { get; set; }

    /// <summary>
    /// Gets or sets the item's location identifier.
    /// </summary>
    /// <remarks>
    /// A numeric location code represented as a byte in the consuming system.
    /// The JSON contract stores this as a <c>string?</c>, therefore consumers
    /// should parse or validate the value according to the runtime expectations.
    /// Typical semantics map values to rooms, inventory slots or special locations.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? location { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the container item that holds this item.
    /// </summary>
    /// <remarks>
    /// When set, this value references another item (by its identifier or name)
    /// indicating that this item is contained inside that item rather than
    /// placed in a room or the player's inventory.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? initem { get; set; }

    /// <summary>
    /// Gets or sets the identifier of an item that can unlock or interact with this item.
    /// </summary>
    /// <remarks>
    /// If specified, this usually refers to an item required to unlock, open,
    /// or otherwise change the state of this item (for example, a key).
    /// Consumers should interpret the format according to the JSON schema used.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? unlockitem { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item cannot be picked up.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item is not portable and should not be moved into
    /// the player's inventory by typical pickup actions.
    /// A <c>null</c> value indicates no explicit portability instruction.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? notportable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item can be opened.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item supports an "open" operation. A <c>null</c>
    /// value indicates the property is unspecified in the JSON input.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? openable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item can be closed.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item supports a "close" operation. A <c>null</c>
    /// value indicates the property is unspecified in the JSON input.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? closeable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is currently open.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the item is considered open. When <c>false</c>,
    /// it is considered closed. A <c>null</c> value indicates no explicit state.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? open { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is currently switched on.
    /// </summary>
    /// <remarks>
    /// This flag indicates the runtime powered/active state for items that can
    /// be turned on (for example, lamps, radios). A <c>null</c> value means the
    /// JSON input did not specify the state and consumers should use a default
    /// or explicit initialization logic.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? switchedon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item can be switched on or off.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item supports being toggled between on/off states.
    /// A <c>null</c> value indicates the switchability is unspecified in the JSON input.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? switchable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item acts as a container.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the item is intended to hold other items (for example,
    /// a box or a chest). A <c>null</c> value indicates the container status is unspecified.
    /// The property is nullable and omitted from JSON when <c>null</c>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? container { get; set; }

    /// <summary>
    /// Computes a compact options bitmask representing boolean item flags.
    /// </summary>
    /// <remarks>
    /// The returned byte packs the following flags (bit values shown):
    /// - 0x01: <c>notportable</c>
    /// - 0x02: <c>open</c>
    /// - 0x04: <c>openable</c>
    /// - 0x08: <c>closeable</c>
    /// - 0x10: <c>switchedon</c>
    /// - 0x20: <c>switchable</c>
    /// - 0x40: <c>container</c>
    ///
    /// Each flag is included only if the corresponding nullable boolean is
    /// explicitly <c>true</c>. This method provides a compact representation
    /// for consumers that expect a single options byte when converting the
    /// JSON model to legacy or binary formats.
    /// </remarks>
    /// <returns>
    /// A <c>byte</c> containing the combined options flags.
    /// </returns>
    public byte Options() {
        byte options = 0;
        if (notportable == true) options |= 0x01;
        if (open == true) options |= 0x02;
        if (openable == true) options |= 0x04;
        if (closeable == true) options |= 0x08;
        if (switchedon == true) options |= 0x10;
        if (switchable == true) options |= 0x20;
        if (container == true) options |= 0x40;
        return options;
    }
}

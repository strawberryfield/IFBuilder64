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
    ///
    /// </remarks>
    public required string description { get; set; }

    /// <summary>
    /// Gets or sets the item's location identifier.
    /// </summary>
    /// <remarks>
    /// A numeric location code represented as a byte. The exact range and meaning
    /// of values depends on the consuming system; typically values map to
    /// predefined locations or slots within the environment.
    /// Note: the property is declared as <c>string?</c> to match the JSON contract;
    /// consumers may parse or interpret the value as needed.
    /// </remarks>
    public string? location { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the container item that holds this item.
    /// </summary>
    /// <remarks>
    /// When set, this value references another item (by its identifier or name)
    /// indicating that this item is contained inside that item rather than
    /// placed in a room or the player's inventory.
    /// </remarks>
    public string? initem { get; set; }

    /// <summary>
    /// Gets or sets the identifier of an item that can unlock or interact with this item.
    /// </summary>
    /// <remarks>
    /// If specified, this usually refers to an item required to unlock, open,
    /// or otherwise change the state of this item (for example, a key).
    /// Consumers should interpret the format according to the JSON schema used.
    /// </remarks>
    public string? unlockitem { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item cannot be picked up.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item is not portable and should not be moved into
    /// the player's inventory by typical pickup actions.
    /// A <c>null</c> value indicates no explicit portability instruction.
    /// </remarks>
    public bool? notportable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item can be opened.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item supports an "open" operation. A <c>null</c>
    /// value indicates the property is unspecified in the JSON input.
    /// </remarks>
    public bool? openable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item can be closed.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the item supports a "close" operation. A <c>null</c>
    /// value indicates the property is unspecified in the JSON input.
    /// </remarks>
    public bool? closeable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is currently open.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the item is considered open. When <c>false</c>,
    /// it is considered closed. A <c>null</c> value indicates no explicit state.
    /// </remarks>
    public bool? open { get; set; }

    /// <summary>
    /// Computes a compact options bitmask representing boolean item flags.
    /// </summary>
    /// <remarks>
    /// The returned byte packs the following flags (bit values shown):
    /// - 0x01: <c>notportable</c>
    /// - 0x02: <c>open</c>
    /// - 0x04: <c>openable</c>
    /// - 0x08: <c>closeable</c>
    ///
    /// Each flag is included only if the corresponding nullable boolean is
    /// explicitly <c>true</c>. This method is designed to provide a compact
    /// representation for consumers that expect a single options byte.
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
        return options;
    }
}

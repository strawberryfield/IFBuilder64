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
    /// </remarks>
    public byte? location { get; set; }
    public byte? initem { get; set; }

    public bool? notportable { get; set; }
    public bool? openable { get; set; }
    public bool? closeable { get; set; }
    public bool? open { get; set; }

    public byte Options() {        
        byte options = 0;
        if (notportable == true) options |= 0x01;
        if (open == true) options |= 0x02;
        if (openable == true) options |= 0x04;
        if (closeable == true) options |= 0x08;
        return options;
    }
}

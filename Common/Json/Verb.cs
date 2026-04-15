//-----------------------------------------------------------------------
// <copyright file="Verb.cs" company="Casasoft">
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

using System.ComponentModel;

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents a verb entry used in JSON data exchanged by IFBuilder64.
/// </summary>
/// <remarks>
/// A verb consists of a textual command and an associated numeric action code.
/// This class is a simple DTO intended for JSON serialization/deserialization.
/// </remarks>
[Description("Represents a verb entry in the game's JSON model. Contains a command string and an associated action code.")]
public class Verb
{
    /// <summary>
    /// Gets or sets the textual command for the verb.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that contains the command text (for example, "take", "open").
    /// This value is used when matching or displaying verb names.
    /// </value>
    [Description("Textual command for the verb (e.g., \"take\", \"open\").")]
    public required string command { get; set; }

    /// <summary>
    /// Gets or sets the numeric action code associated with the verb.
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> value representing the action code. The meaning of specific
    /// codes is determined by the consuming logic within IFBuilder64.
    /// </value>
    [Description("Numeric action code associated with the verb.")]
    public byte action { get; set; }
}

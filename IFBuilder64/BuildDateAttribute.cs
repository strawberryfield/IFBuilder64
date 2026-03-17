//-----------------------------------------------------------------------
// <copyright file="IFBuilder64/BuildDateAttribute.cs" company="Casasoft">
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

using System.Globalization;

[AttributeUsage(AttributeTargets.Assembly)]
internal class BuildDateAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDateAttribute"/> class.
    /// </summary>
    /// <param name="value">
    /// A string representation of the build date and time in the format
    /// "yyyyMMddHHmmss" (for example "20260317123045" for 2026-03-17 12:30:45).
    /// </param>
    /// <remarks>
    /// The provided <paramref name="value"/> is parsed using
    /// <see cref="DateTime.ParseExact(string,string,IFormatProvider,DateTimeStyles)"/> with
    /// <see cref="CultureInfo.InvariantCulture"/> and <see cref="DateTimeStyles.None"/>.
    /// Apply this attribute at the assembly level to embed the build timestamp into the assembly metadata:
    /// <code>
    /// [assembly: BuildDate("20260317123045")]
    /// </code>
    /// </remarks>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="value"/> does not match the expected "yyyyMMddHHmmss" format.
    /// </exception>
    public BuildDateAttribute(string value)
    {
        DateTime = DateTime.ParseExact(
            value,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
    }

    /// <summary>
    /// Gets the parsed build <see cref="DateTime"/> value.
    /// </summary>
    /// <remarks>
    /// This value reflects the moment encoded in the attribute string and is useful for
    /// displaying the build timestamp at runtime or embedding it in diagnostics/about dialogs.
    /// </remarks>
    public DateTime DateTime { get; }
}
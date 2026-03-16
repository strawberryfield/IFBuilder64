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
    public BuildDateAttribute(string value)
    {
        DateTime = DateTime.ParseExact(
            value,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
    }

    public DateTime DateTime { get; }
}
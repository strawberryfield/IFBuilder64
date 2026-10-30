//-----------------------------------------------------------------------
// <copyright file="MapPosition.cs" company="Casasoft">
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
/// Position of a room on the graphical editor map grid.
/// </summary>
[Description("Position of a room on the editor map grid.")]
public class MapPosition
{
    /// <summary>Column on the grid.</summary>
    [Description("Column on the grid.")]
    public int x { get; set; }

    /// <summary>Row on the grid.</summary>
    [Description("Row on the grid.")]
    public int y { get; set; }

    /// <summary>Level (floor); 0 = ground, positive = up, negative = down.</summary>
    [Description("Level (floor); 0 = ground, positive = up, negative = down.")]
    public int z { get; set; }
}

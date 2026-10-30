//-----------------------------------------------------------------------
// <copyright file="ModelloTests.cs" company="Casasoft">
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

using System.Text.Json;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

public class ModelloTests
{
    [Fact]
    public void StanzaSenzaMappa_NonScriveIlCampo()
    {
        var json = JsonSerializer.Serialize(new Room { name = "a", description = "" });
        Assert.DoesNotContain("\"map\"", json);
    }

    [Fact]
    public void StanzaConMappa_AndataERitorno()
    {
        var r = new Room { name = "a", description = "", map = new MapPosition { x = 2, y = -1, z = 1 } };
        var r2 = JsonSerializer.Deserialize<Room>(JsonSerializer.Serialize(r))!;
        Assert.Equal((2, -1, 1), (r2.map!.x, r2.map.y, r2.map.z));
    }
}

/// <summary>
/// Definizione della collezione che cambia la cartella corrente del processo:
/// va eseguita senza parallelismo con gli altri test.
/// </summary>
[CollectionDefinition("CartellaCorrente", DisableParallelization = true)]
public class CartellaCorrenteCollezione;

[Collection("CartellaCorrente")]
public class CartellaCorrenteTests
{
    [Fact]
    public void Template_SiCaricaDaAltraCartellaCorrente()
    {
        var vecchia = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());
            var g = new Game(TemplateLanguage.Italiano);
            Assert.Equal(55, g.messages.Count);
            Assert.Equal(6, g.directions.Count);
        }
        finally
        {
            Directory.SetCurrentDirectory(vecchia);
        }
    }
}

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

    [Fact]
    public void OggettoSenzaId_UsaLaRegola()
    {
        Assert.Equal("baule@torre", new Item { name = "baule", description = "", notportable = true, location = "torre" }.id);
        Assert.Equal("chiave", new Item { name = "chiave", description = "" }.id);
    }

    [Fact]
    public void OggettoConId_UsaIlValore() =>
        Assert.Equal("scopa", new Item { name = "broom", description = "", id = "scopa" }.id);

    [Fact]
    public void OggettoIdSoloSpazi_UsaLaRegola() =>
        Assert.Equal("broom", new Item { name = "broom", description = "", id = "  " }.id);

    [Fact]
    public void OggettoJsonIdConSpazi_Ripulito()
    {
        var i = JsonSerializer.Deserialize<Item>("""{"name":"key","id":"  chiave ","description":""}""")!;
        Assert.Equal("chiave", i.id);
    }

    [Fact]
    public void OggettoConId_AndataERitorno()
    {
        var i = new Item { name = "broom", description = "", id = "scopa" };
        var i2 = JsonSerializer.Deserialize<Item>(JsonSerializer.Serialize(i))!;
        Assert.Equal("scopa", i2.id);
    }

    [Fact]
    public void OggettoJsonSenzaId_UsaLaRegola()
    {
        var i = JsonSerializer.Deserialize<Item>("""{"name":"benches","notportable":true,"location":"aula","description":""}""")!;
        Assert.Equal("benches@aula", i.id);
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

//-----------------------------------------------------------------------
// <copyright file="BuilderTests.cs" company="Casasoft">
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


using Casasoft.IFBuilder64.Builder;
using Casasoft.IFBuilder64.Json;
using System.Text.Json;

namespace Casasoft.IFBuilder64.Tests;

public class BuilderTests
{
    private static string PercorsoDati(string nome) => Path.Combine(AppContext.BaseDirectory, "Dati", nome);

    private static string CartellaRiferimenti => Path.Combine(AppContext.BaseDirectory, "Riferimenti");

    private static Game CaricaEsempio() =>
        JsonSerializer.Deserialize<Game>(File.ReadAllText(PercorsoDati("esempio.it.json")))!;

    [Fact]
    public void Build_ProduceGliStessiByteDellaCli()
    {
        var game = CaricaEsempio();
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var r = new GameBuilder().Build(game, Path.Combine(dir, "esempio"));
            Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
            Assert.Equal(9, r.FileGenerati.Count);
            var riferimenti = Directory.GetFiles(CartellaRiferimenti);
            Assert.Equal(9, riferimenti.Length);
            foreach (var rif in riferimenti)
            {
                Assert.Equal(Confronto.Normalizza(rif), Confronto.Normalizza(Path.Combine(dir, Path.GetFileName(rif))));
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Build_NonModificaIlGameDelChiamante()
    {
        var game = CaricaEsempio();
        var prima = JsonSerializer.Serialize(game);
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            new GameBuilder().Build(game, Path.Combine(dir, "esempio"));
            Assert.Equal(prima, JsonSerializer.Serialize(game));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Build_NonScriveSuConsole()
    {
        var game = CaricaEsempio();
        var dir = Directory.CreateTempSubdirectory().FullName;
        var vecchio = Console.Out;
        var scrittore = new StringWriter();
        try
        {
            Console.SetOut(scrittore);
            var r = new GameBuilder().Build(game, Path.Combine(dir, "esempio"));
            Assert.NotEmpty(r.Log);
        }
        finally
        {
            Console.SetOut(vecchio);
            Directory.Delete(dir, true);
        }
        Assert.Equal("", scrittore.ToString());
    }

    private static BuildResult CompilaInTemp(Game game)
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            return new GameBuilder().Build(game, Path.Combine(dir, "vuoto"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Build_SenzaMessaggiUtente_Riesce()
    {
        var game = CaricaEsempio();
        game.usermessages = [];
        var r = CompilaInTemp(game);
        Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
    }

    [Fact]
    public void Build_SenzaPorte_Riesce()
    {
        var game = CaricaEsempio();
        game.doors = [];
        var r = CompilaInTemp(game);
        Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
    }

    [Fact]
    public void Build_SenzaOggetti_Riesce()
    {
        var game = CaricaEsempio();
        game.items = [];
        foreach (var porta in game.doors)
        {
            porta.unlockitem = null;
        }
        var r = CompilaInTemp(game);
        Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
    }

    [Fact]
    public void Build_SenzaOggettiPortabili_Riesce()
    {
        var game = CaricaEsempio();
        game.items = [.. game.items.Where(i => i.notportable is true)];
        foreach (var oggetto in game.items)
        {
            oggetto.initem = null;
            oggetto.unlockitem = null;
        }
        foreach (var porta in game.doors)
        {
            porta.unlockitem = null;
        }
        var r = CompilaInTemp(game);
        Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
    }

    [Fact]
    public void Build_SenzaOggettiStatici_Riesce()
    {
        var game = CaricaEsempio();
        game.items = [.. game.items.Where(i => i.notportable is not true)];
        foreach (var oggetto in game.items)
        {
            oggetto.initem = null;
        }
        var r = CompilaInTemp(game);
        Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
    }

    [Fact]
    public void Build_SenzaVerbi_Riesce()
    {
        var game = CaricaEsempio();
        game.verbs = [];
        var r = CompilaInTemp(game);
        Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
    }

    [Fact]
    public void Build_Errore_RigaDiLogConPrefissoError()
    {
        var game = CaricaEsempio();
        var inesistente = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sotto", "esempio");
        var r = new GameBuilder().Build(game, inesistente);
        Assert.False(r.Successo);
        Assert.StartsWith("Error: ", r.Log[^1]);
    }

    [Fact]
    public void Build_CartellaInesistente_RestituisceErroreSenzaEccezione()
    {
        var game = CaricaEsempio();
        var inesistente = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sotto", "esempio");
        var r = new GameBuilder().Build(game, inesistente);
        Assert.False(r.Successo);
        Assert.NotEmpty(r.Log);
    }
}

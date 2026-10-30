﻿//-----------------------------------------------------------------------
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
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
                Assert.Equal(Normalizza(rif), Normalizza(Path.Combine(dir, Path.GetFileName(rif))));
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

    [Fact]
    public void Build_CartellaInesistente_RestituisceErroreSenzaEccezione()
    {
        var game = CaricaEsempio();
        var inesistente = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sotto", "esempio");
        var r = new GameBuilder().Build(game, inesistente);
        Assert.False(r.Successo);
        Assert.NotEmpty(r.Log);
    }

    /// <summary>
    /// Byte del file; per *ab.seq azzera la quinta riga (data, separatore '\r'),
    /// per *iFiction.xml svuota <releasedate> e <compilerversion>.
    /// </summary>
    private static string Normalizza(string percorso)
    {
        var byteFile = File.ReadAllBytes(percorso);
        var nome = Path.GetFileName(percorso);
        if (nome.EndsWith("ab.seq", StringComparison.Ordinal))
        {
            var righe = Encoding.Latin1.GetString(byteFile).Split('\r');
            righe[4] = "";
            return string.Join('\r', righe);
        }
        if (nome.EndsWith("iFiction.xml", StringComparison.Ordinal))
        {
            var testo = Encoding.UTF8.GetString(byteFile);
            testo = Regex.Replace(testo, "(<releasedate>)[^<]*(</releasedate>)", "$1$2");
            testo = Regex.Replace(testo, "(<compilerversion>)[^<]*(</compilerversion>)", "$1$2");
            return testo;
        }
        return Convert.ToHexString(byteFile);
    }
}

//-----------------------------------------------------------------------
// <copyright file="CompilazioneTests.cs" company="Casasoft">
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


using Casasoft.IFBuilder64.Editor.Servizi;

namespace Casasoft.IFBuilder64.Tests;

public class CompilazioneTests
{
    private static string PercorsoEsempio => Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json");

    private static string CartellaRiferimenti => Path.Combine(AppContext.BaseDirectory, "Riferimenti");

    [Fact]
    public async Task Compila_StessiByteDellaCli()
    {
        var doc = ServizioFile.Apri(PercorsoEsempio);
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var r = await new ServizioCompilazione().CompilaAsync(doc, Path.Combine(dir, "esempio"));
            Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
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
    public async Task Compila_NonModificaIlDocumento()
    {
        var doc = ServizioFile.Apri(PercorsoEsempio);
        var nomiPrima = doc.Stanze.Select(s => s.Nome).ToList();
        var modificatoPrima = doc.Modificato;
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var r = await new ServizioCompilazione().CompilaAsync(doc, Path.Combine(dir, "esempio"));
            Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
        Assert.Equal(nomiPrima, doc.Stanze.Select(s => s.Nome).ToList());
        Assert.Equal(modificatoPrima, doc.Modificato);
    }

    [Fact]
    public void OutputPredefinito_StessaRegolaDellaCli()
    {
        Assert.Equal(Path.Combine("c", "gioco."), ServizioCompilazione.OutputPredefinito(Path.Combine("c", "gioco.json")));
    }
}

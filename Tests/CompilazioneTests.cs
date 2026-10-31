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


using Casasoft.IFBuilder64.Builder;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;
using System.Text.Json;

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
    public async Task NuovoGioco_SalvatoERiaperto_SiCompilaComeLaCli()
    {
        var doc = ServizioFile.Nuovo(TemplateLanguage.Italiano);
        var ingresso = new StanzaViewModel { Nome = "ingresso", Descrizione = "Un ingresso." };
        var sala = new StanzaViewModel { Nome = "sala", Descrizione = "Una sala." };
        var cucina = new StanzaViewModel { Nome = "cucina", Descrizione = "Una cucina." };
        doc.Stanze.Add(ingresso);
        doc.Stanze.Add(sala);
        doc.Stanze.Add(cucina);
        Collegamenti.Collega(doc, ingresso, Direzione.Nord, sala, senzaRitorno: false);
        Collegamenti.Collega(doc, sala, Direzione.Est, cucina, senzaRitorno: false);
        Collegamenti.TrasformaInPorta(doc, sala, Direzione.Est);
        doc.StanzaIniziale = "ingresso";
        doc.Oggetti.Add(new OggettoViewModel { Nome = "lampada", Descrizione = "Una lampada.", Posizione = "sala" });
        doc.Oggetti.Add(new OggettoViewModel { Nome = "tavolo", Descrizione = "Un tavolo.", Posizione = "cucina", Statico = true });

        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var json = Path.Combine(dir, "nuovo.json");
            ServizioFile.Salva(doc, json);
            var riaperto = ServizioFile.Apri(json);

            var r = await new ServizioCompilazione().CompilaAsync(riaperto, Path.Combine(dir, "editor"));
            Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));

            var game = JsonSerializer.Deserialize<Game>(File.ReadAllText(json))!;
            var cli = new GameBuilder().Build(game, Path.Combine(dir, "cli"));
            Assert.True(cli.Successo, string.Join(Environment.NewLine, cli.Log));

            Assert.Equal(cli.FileGenerati.Count, r.FileGenerati.Count);
            foreach (var fileCli in cli.FileGenerati)
            {
                var suffisso = Path.GetFileName(fileCli)["cli".Length..];
                Assert.Equal(Confronto.Normalizza(fileCli), Confronto.Normalizza(Path.Combine(dir, "editor" + suffisso)));
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void OutputPredefinito_StessaRegolaDellaCli()
    {
        Assert.Equal(Path.Combine("c", "gioco."), ServizioCompilazione.OutputPredefinito(Path.Combine("c", "gioco.json")));
    }
}

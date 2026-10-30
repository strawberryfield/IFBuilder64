//-----------------------------------------------------------------------
// <copyright file="Tests/ImpostazioniTests.cs" company="Casasoft">
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

/// <summary>
/// Test delle impostazioni utente dell'editor. Usano solo file temporanei.
/// </summary>
public class ImpostazioniTests
{
    private static string PercorsoTemporaneo() =>
        Path.Combine(Path.GetTempPath(), "ifb64-test-" + Guid.NewGuid().ToString("N"), "editor.json");

    private static void Pulisci(string percorso)
    {
        var cartella = Path.GetDirectoryName(percorso)!;
        if (Directory.Exists(cartella)) Directory.Delete(cartella, true);
    }

    [Fact]
    public void Impostazioni_AndataERitorno()
    {
        var percorso = PercorsoTemporaneo();
        try
        {
            var originale = new ImpostazioniUtente
            {
                Lingua = "it",
                UltimaCartellaOutput = @"C:\avventure\àèì",
                FileRecenti = ["a.json", "b.json"]
            };
            originale.Salva(percorso);

            var letto = ImpostazioniUtente.Carica(percorso);
            Assert.Equal("it", letto.Lingua);
            Assert.Equal(@"C:\avventure\àèì", letto.UltimaCartellaOutput);
            Assert.Equal(["a.json", "b.json"], letto.FileRecenti);
        }
        finally { Pulisci(percorso); }
    }

    [Fact]
    public void Impostazioni_FileMancanteOCorrotto_RestituisceDefault()
    {
        var percorso = PercorsoTemporaneo();
        try
        {
            var mancante = ImpostazioniUtente.Carica(percorso);
            Assert.Null(mancante.Lingua);
            Assert.Empty(mancante.FileRecenti);

            Directory.CreateDirectory(Path.GetDirectoryName(percorso)!);
            File.WriteAllText(percorso, "{ non è json");
            var corrotto = ImpostazioniUtente.Carica(percorso);
            Assert.Null(corrotto.Lingua);
            Assert.Empty(corrotto.FileRecenti);
            Assert.Null(corrotto.UltimaCartellaOutput);
        }
        finally { Pulisci(percorso); }
    }

    [Fact]
    public void Impostazioni_RecentiSenzaDuplicatiMax10()
    {
        var impostazioni = new ImpostazioniUtente();
        for (var i = 1; i <= 11; i++) impostazioni.AggiungiRecente($"f{i}.json");
        impostazioni.AggiungiRecente("f5.json"); // duplicato: risale in testa

        Assert.Equal(10, impostazioni.FileRecenti.Count);
        Assert.Equal("f5.json", impostazioni.FileRecenti[0]);
        Assert.Equal("f11.json", impostazioni.FileRecenti[1]);
        Assert.Single(impostazioni.FileRecenti, "f5.json");
        Assert.DoesNotContain("f1.json", impostazioni.FileRecenti);
    }

    [Fact]
    public void Impostazioni_SalvaCreaLaCartella()
    {
        var percorso = PercorsoTemporaneo();
        try
        {
            new ImpostazioniUtente().Salva(percorso);
            Assert.True(File.Exists(percorso));
        }
        finally { Pulisci(percorso); }
    }
}

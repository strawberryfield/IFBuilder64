//-----------------------------------------------------------------------
// <copyright file="Tests/TestiLocalizzatiTests.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Testi mostrati all'utente che dipendono dalla lingua dell'interfaccia.</summary>
[Collection("CartellaCorrente")]
public class TestiLocalizzatiTests
{
    private static void ConCultura(string cultura, Action azione)
    {
        var precedente = Testi.Culture;
        try
        {
            Testi.Culture = new CultureInfo(cultura);
            azione();
        }
        finally
        {
            Testi.Culture = precedente;
        }
    }

    private static async Task ConCulturaAsync(string cultura, Func<Task> azione)
    {
        var precedente = Testi.Culture;
        try
        {
            Testi.Culture = new CultureInfo(cultura);
            await azione();
        }
        finally
        {
            Testi.Culture = precedente;
        }
    }

    /// <summary>Stanze a e b (a → est → b), oggetto x in b, b stanza iniziale.</summary>
    private static (DocumentoViewModel doc, StanzaViewModel a, StanzaViewModel b) Documento()
    {
        var d = new DocumentoViewModel();
        var a = new StanzaViewModel { Nome = "a", HaPosizione = true };
        var b = new StanzaViewModel { Nome = "b", HaPosizione = true, X = 1 };
        d.Stanze.Add(a);
        d.Stanze.Add(b);
        a.Est = "b";
        d.Oggetti.Add(new OggettoViewModel { Nome = "x", Posizione = "b" });
        d.StanzaIniziale = "b";
        return (d, a, b);
    }

    [Theory]
    [InlineData("en", "a (East)", "x (Location)", "Initial room")]
    [InlineData("it", "a (Est)", "x (Posizione)", "Stanza iniziale")]
    public async Task EliminaStanza_RiferimentiConNomiDeiCampiLocalizzati(string cultura, string uscita, string posizione, string iniziale)
    {
        var (d, _, b) = Documento();
        var dialoghi = new DialoghiFinti { RispostaConferma = false };
        var mappa = new MappaViewModel(d, dialoghi);

        await ConCulturaAsync(cultura, () => mappa.EliminaStanzaAsync(b));

        var messaggio = dialoghi.UltimoMessaggioConferma!;
        Assert.Contains("- " + uscita, messaggio);
        Assert.Contains("- " + posizione, messaggio);
        Assert.Contains("- " + iniziale, messaggio);
    }

    [Theory]
    [InlineData("en", "b: no exit towards North.")]
    [InlineData("it", "b: nessuna uscita in direzione Nord.")]
    public void TrasformaInPorta_SenzaUscita_MessaggioLocalizzato(string cultura, string atteso)
    {
        var (d, _, b) = Documento();
        ConCultura(cultura, () =>
            Assert.Equal(atteso, Assert.Throws<InvalidOperationException>(() => Collegamenti.TrasformaInPorta(d, b, Direzione.Nord)).Message));
    }

    [Theory]
    [InlineData("en", "p: the door does not connect two existing rooms.")]
    [InlineData("it", "p: la porta non ha due lati su stanze esistenti.")]
    public void TrasformaInPassaggio_PortaSenzaDueLati_MessaggioLocalizzato(string cultura, string atteso)
    {
        var (d, _, _) = Documento();
        var porta = new PortaViewModel { Nome = "p" };
        d.Porte.Add(porta);
        ConCultura(cultura, () =>
            Assert.Equal(atteso, Assert.Throws<InvalidOperationException>(() => Collegamenti.TrasformaInPassaggio(d, porta)).Message));
    }
}

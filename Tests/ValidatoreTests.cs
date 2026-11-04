//-----------------------------------------------------------------------
// <copyright file="Tests/ValidatoreTests.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Test delle regole del validatore.</summary>
public class ValidatoreTests
{
    private static readonly int _messaggiTemplate = ServizioFile.MessaggiTemplate(TemplateLanguage.Italiano).Count;

    /// <summary>Documento minimo valido: template italiano più una stanza iniziale.</summary>
    private static DocumentoViewModel Minimo()
    {
        var d = ServizioFile.Nuovo(TemplateLanguage.Italiano);
        d.Stanze.Add(new StanzaViewModel { Nome = "sala" });
        d.StanzaIniziale = "sala";
        return d;
    }

    private static IReadOnlyList<Avviso> Valida(DocumentoViewModel d) => Validatore.Valida(d, _messaggiTemplate);

    private static StanzaViewModel Stanza(DocumentoViewModel d, string nome)
    {
        var s = new StanzaViewModel { Nome = nome };
        d.Stanze.Add(s);
        return s;
    }

    [Fact]
    public void DocumentoMinimo_NessunAvviso()
    {
        Assert.Empty(Valida(Minimo()));
    }

    [Fact]
    public void NessunaStanza_Errore()
    {
        var d = ServizioFile.Nuovo(TemplateLanguage.Italiano);
        d.Stanze.Clear();
        var avvisi = Valida(d);
        Assert.Contains(avvisi, a => a.Gravita == Gravita.Errore && ReferenceEquals(a.Elemento, d));
        Assert.True(Validatore.HaErrori(avvisi));
    }

    [Theory]
    [InlineData("")]
    [InlineData("inesistente")]
    public void StanzaInizialeVuotaOInesistente_Errore(string iniziale)
    {
        var d = Minimo();
        d.StanzaIniziale = iniziale;
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Errore, a.Gravita);
        Assert.Same(d, a.Elemento);
    }

    [Fact]
    public void NomiStanzaDuplicati_Errore()
    {
        var d = Minimo();
        var seconda = Stanza(d, "sala");
        var a = Assert.Single(Valida(d), x => x.Gravita == Gravita.Errore);
        Assert.Equal(Gravita.Errore, a.Gravita);
        Assert.Same(seconda, a.Elemento);
    }

    [Fact]
    public void PorteOmonimeSullaStessaStanza_Errore()
    {
        var d = Minimo();
        Stanza(d, "torre");
        Stanza(d, "cantina");
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "torre", Sud = "sala" });
        var seconda = new PortaViewModel { Nome = "p", Est = "cantina", Ovest = "sala" };
        d.Porte.Add(seconda);
        var a = Assert.Single(Valida(d), x => x.Gravita == Gravita.Errore);
        Assert.Same(seconda, a.Elemento);
        Assert.Contains("sala", a.Messaggio);
    }

    [Fact]
    public void PorteOmonimeSuStanzeDiverse_NonDuplicato()
    {
        var d = Minimo();
        foreach (var n in new[] { "torre", "cantina", "soffitta" }) Stanza(d, n);
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "torre", Sud = "sala" });
        d.Porte.Add(new PortaViewModel { Nome = "p", Est = "soffitta", Ovest = "cantina" });
        Assert.DoesNotContain(Valida(d), a => a.Gravita == Gravita.Errore);
    }

    [Fact]
    public void IdOggettoDuplicati_Errore()
    {
        var d = Minimo();
        d.Oggetti.Add(new OggettoViewModel { Nome = "chiave" });
        var secondo = new OggettoViewModel { Nome = "chiave" };
        d.Oggetti.Add(secondo);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Errore, a.Gravita);
        Assert.Same(secondo, a.Elemento);
    }

    [Fact]
    public void StessoNomeInStanzeDiverse_NonDuplicato()
    {
        var d = Minimo();
        Stanza(d, "torre");
        d.Oggetti.Add(new OggettoViewModel { Nome = "baule", Statico = true, Posizione = "sala" });
        d.Oggetti.Add(new OggettoViewModel { Nome = "baule", Statico = true, Posizione = "torre" });
        Assert.DoesNotContain(Valida(d), a => a.Gravita == Gravita.Errore);
    }

    [Fact]
    public void Oltre255Stanze_Errore()
    {
        var d = Minimo();
        while (d.Stanze.Count < 255) Stanza(d, "s" + d.Stanze.Count);
        Assert.DoesNotContain(Valida(d), a => a.Gravita == Gravita.Errore);

        Stanza(d, "s255");
        Assert.Contains(Valida(d), a => a.Gravita == Gravita.Errore && ReferenceEquals(a.Elemento, d));
    }

    [Fact]
    public void Oltre255Oggetti_Errore()
    {
        var d = Minimo();
        while (d.Oggetti.Count < 255) d.Oggetti.Add(new OggettoViewModel { Nome = "o" + d.Oggetti.Count });
        Assert.DoesNotContain(Valida(d), a => a.Gravita == Gravita.Errore);

        d.Oggetti.Add(new OggettoViewModel { Nome = "o255" });
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Errore, a.Gravita);
        Assert.Same(d, a.Elemento);
    }

    [Fact]
    public void Oltre255Porte_Errore()
    {
        var d = Minimo();
        for (var i = 0; i < 255; i++) d.Porte.Add(new PortaViewModel { Nome = "p" + i });
        Assert.DoesNotContain(Valida(d), a => a.Gravita == Gravita.Errore);

        d.Porte.Add(new PortaViewModel { Nome = "p255" });
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Errore, a.Gravita);
        Assert.Same(d, a.Elemento);
    }

    [Fact]
    public void StaticoSenzaStanza_Errore()
    {
        var d = Minimo();
        var o = new OggettoViewModel { Nome = "baule", Statico = true };
        d.Oggetti.Add(o);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Errore, a.Gravita);
        Assert.Same(o, a.Elemento);
    }

    [Fact]
    public void UscitaVersoStanzaInesistente_Avviso()
    {
        var d = Minimo();
        d.Stanze[^1].Nord = "nulla";
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(d.Stanze[^1], a.Elemento);
    }

    [Fact]
    public void LatoPortaVersoStanzaInesistente_Avviso()
    {
        var d = Minimo();
        var p = new PortaViewModel { Nome = "p", Nord = "sala", Sud = "nulla" };
        d.Porte.Add(p);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(p, a.Elemento);
    }

    [Fact]
    public void LocationInitemUnlockitemOrfani_Avviso()
    {
        var d = Minimo();
        var o = new OggettoViewModel { Nome = "x", Posizione = "nulla", DentroA = "nulla", OggettoSblocco = "nulla" };
        d.Oggetti.Add(o);
        var avvisi = Valida(d);
        Assert.Equal(3, avvisi.Count);
        Assert.All(avvisi, a =>
        {
            Assert.Equal(Gravita.Avviso, a.Gravita);
            Assert.Same(o, a.Elemento);
        });
    }

    [Fact]
    public void RiferimentiValidi_NessunAvviso()
    {
        var d = Minimo();
        d.Oggetti.Add(new OggettoViewModel { Nome = "chiave", Posizione = "player" });
        d.Oggetti.Add(new OggettoViewModel { Nome = "borsa", Posizione = "sala", OggettoSblocco = "chiave" });
        d.Oggetti.Add(new OggettoViewModel { Nome = "moneta", DentroA = "borsa" });
        Assert.Empty(Valida(d));
    }

    [Fact]
    public void SbloccoPerIdPersonalizzato_NonOrfano()
    {
        var d = ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));
        var prima = Valida(d).Select(a => a.Messaggio).ToList();
        Assert.Null(d.ImpostaIdOggetto(d.Oggetti.First(o => o.Nome == "chiave"), "k"));
        Assert.Equal(prima, Valida(d).Select(a => a.Messaggio).ToList());
    }

    [Fact]
    public void SbloccoVersoNomeNonId_Orfano()
    {
        var d = Minimo();
        d.Oggetti.Add(new OggettoViewModel { Nome = "tavolo", Posizione = "sala", Statico = true });
        var p = new PortaViewModel { Nome = "p", OggettoSblocco = "tavolo" };
        d.Porte.Add(p);
        var a = Assert.Single(Valida(d));
        Assert.Same(p, a.Elemento);
    }

    [Fact]
    public void SbloccoOrfanoSuPorta_Avviso()
    {
        var d = Minimo();
        var p = new PortaViewModel { Nome = "p", OggettoSblocco = "nulla" };
        d.Porte.Add(p);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(p, a.Elemento);
    }

    [Fact]
    public void StanzaNonRaggiungibile_Avviso()
    {
        var d = Minimo();
        var isolata = Stanza(d, "isolata");
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(isolata, a.Elemento);
    }

    [Fact]
    public void StanzaRaggiungibileAttraversoPorta_NessunAvviso()
    {
        var d = Minimo();
        Stanza(d, "torre");
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "sala", Sud = "torre", Aperta = false });
        Assert.Empty(Valida(d));
    }

    [Fact]
    public void StanzeRaggiungibili_SeguonoUsciteEPorte()
    {
        var d = Minimo();
        var torre = Stanza(d, "torre");
        var isolata = Stanza(d, "isolata");
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "sala", Sud = "torre", Aperta = false });

        var r = Validatore.StanzeRaggiungibili(d);

        Assert.Contains(d.TrovaStanza("sala")!, r);
        Assert.Contains(torre, r);
        Assert.DoesNotContain(isolata, r);
        Assert.DoesNotContain(d.Stanze[0], r);

        d.StanzaIniziale = "inesistente";
        Assert.Empty(Validatore.StanzeRaggiungibili(d));
    }

    [Fact]
    public void PlayerNonSegnalatoNonRaggiungibile()
    {
        var d = Minimo();
        // Il template contiene già la stanza "player", che nessuna uscita raggiunge.
        Assert.Contains(d.Stanze, s => s.EGiocatore);
        Assert.Empty(Valida(d));
    }

    [Fact]
    public void UscitaDoppiaPortaChiusa_AvvisoConCorrezione()
    {
        var d = Minimo();
        var torre = Stanza(d, "torre");
        var sala = d.TrovaStanza("sala")!;
        sala.Nord = "torre";
        torre.Sud = "sala";
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "sala", Sud = "torre", Aperta = false });

        var avvisi = Valida(d);
        Assert.Equal(2, avvisi.Count(x => x.Correzione is not null));
        var a = Assert.Single(avvisi, x => ReferenceEquals(x.Elemento, sala));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        a.Correzione!();
        Assert.Null(sala.Nord);
        Assert.Equal("sala", torre.Sud);
    }

    [Fact]
    public void PortaConUnSoloLatoSuUscitaEsistente_NessunAvviso()
    {
        // Caso di latinorum: la stanza iniziale ha l'uscita ovest e una porta finta (un solo lato,
        // mai apribile) sullo stesso lato. Rimuovere l'uscita isolerebbe la stanza iniziale.
        var d = Minimo();
        var torre = Stanza(d, "torre");
        var sala = d.TrovaStanza("sala")!;
        sala.Ovest = "torre";
        torre.Est = "sala";
        d.Porte.Add(new PortaViewModel { Nome = "uscita", Ovest = "sala", Aperta = false });

        Assert.Empty(Valida(d));
        Assert.Equal("torre", sala.Ovest);
    }

    [Fact]
    public void PortaApertaConUscite_NessunAvviso()
    {
        var d = Minimo();
        var torre = Stanza(d, "torre");
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "sala", Sud = "torre", Aperta = true });
        d.TrovaStanza("sala")!.Nord = "torre";
        torre.Sud = "sala";
        Assert.Empty(Valida(d));
    }

    [Fact]
    public void PortaApertaSenzaUscite_Avviso()
    {
        var d = Minimo();
        Stanza(d, "torre");
        var p = new PortaViewModel { Nome = "p", Nord = "sala", Sud = "torre", Aperta = true };
        d.Porte.Add(p);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(p, a.Elemento);
    }

    [Fact]
    public void VerboFuoriCatalogo_Avviso()
    {
        var d = Minimo();
        var v = new VerboViewModel { Comando = "xyz", Azione = 26 };
        d.Verbi.Add(v);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(v, a.Elemento);
    }

    [Fact]
    public void NumeroMessaggiStandardDiverso_Avviso()
    {
        var d = Minimo();
        d.MessaggiStandard.RemoveAt(d.MessaggiStandard.Count - 1);
        var a = Assert.Single(Valida(d));
        Assert.Equal(Gravita.Avviso, a.Gravita);
        Assert.Same(d, a.Elemento);
    }

    [Fact]
    public void HaErrori_SoloConErrori()
    {
        Assert.False(Validatore.HaErrori([new Avviso(Gravita.Avviso, "x", null)]));
        Assert.True(Validatore.HaErrori([new Avviso(Gravita.Errore, "x", null)]));
    }

    [Fact]
    public void DocumentoEsempio_NessunErrore()
    {
        var d = ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));
        Assert.DoesNotContain(Valida(d), a => a.Gravita == Gravita.Errore);
    }
}

//-----------------------------------------------------------------------
// <copyright file="Tests/MappaViewModelTests.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

public class MappaViewModelTests
{
    private readonly DialoghiFinti _dialoghi = new();

    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    /// <summary>Documento con "player" e due stanze posizionate al livello 0.</summary>
    private static DocumentoViewModel Piccolo()
    {
        var d = ServizioFile.Nuovo(TemplateLanguage.Italiano);
        d.Stanze.Add(new StanzaViewModel { Nome = "a", HaPosizione = true, X = 0, Y = 0, Z = 0 });
        d.Stanze.Add(new StanzaViewModel { Nome = "b", HaPosizione = true, X = 1, Y = 0, Z = 0 });
        d.StanzaIniziale = "a";
        return d;
    }

    private MappaViewModel Crea(DocumentoViewModel d) => new(d, _dialoghi);

    [Fact]
    public void CreaStanza_NomeProvvisorioUnivocoEPosizione()
    {
        var d = Piccolo();
        var m = Crea(d);
        m.LivelloCorrente = 2;

        var s1 = m.CreaStanza(3, 4);
        var s2 = m.CreaStanza(5, 4);

        Assert.Equal(Testi.PrefissoStanza + "1", s1.Nome);
        Assert.Equal(Testi.PrefissoStanza + "2", s2.Nome);
        Assert.True(s1.HaPosizione);
        Assert.Equal((3, 4, 2), (s1.X, s1.Y, s1.Z));
        Assert.Same(s1, m.StanzaIn(3, 4));
        Assert.Same(s2, m.Selezione);
    }

    [Fact]
    public void CreaStanza_InCodaNonAIndice0()
    {
        var d = Piccolo();
        var m = Crea(d);

        var s = m.CreaStanza(7, 7);

        Assert.True(d.Stanze[0].EGiocatore);
        Assert.Same(s, d.Stanze[^1]);
    }

    [Fact]
    public void SpostaStanza_SuCellaOccupata_Rifiutato()
    {
        var d = Piccolo();
        var m = Crea(d);
        var a = d.TrovaStanza("a")!;

        Assert.False(m.SpostaStanza(a, 1, 0));
        Assert.Equal((0, 0), (a.X, a.Y));

        Assert.True(m.SpostaStanza(a, 0, 2));
        Assert.Equal((0, 2, 0), (a.X, a.Y, a.Z));
        Assert.Null(m.StanzaIn(0, 0));
    }

    [Fact]
    public void StanzeDelLivello_EsclusoPlayer()
    {
        var d = Piccolo();
        var player = d.Stanze[0];
        player.HaPosizione = true;
        player.X = 5;
        var m = Crea(d);

        Assert.DoesNotContain(player, m.StanzeDelLivello);
        Assert.Equal(["a", "b"], m.StanzeDelLivello.Select(s => s.Nome).Order());
        Assert.Null(m.StanzaIn(5, 0));

        var esempio = Crea(Esempio());
        esempio.LivelloCorrente = 1;
        Assert.Equal(["sommita"], esempio.StanzeDelLivello.Select(s => s.Nome));
    }

    [Fact]
    public void Livelli_DelleStanze_InizioSulLivelloIniziale()
    {
        var m = Crea(Esempio());
        Assert.Equal([1, 0, -1], m.Livelli);
        Assert.Equal(0, m.LivelloCorrente);
    }

    [Fact]
    public void AggiungiLivello_SopraESotto()
    {
        var m = Crea(Esempio());

        m.AggiungiLivello(sopra: true);
        Assert.Equal(2, m.LivelloCorrente);
        Assert.Equal([2, 1, 0, -1], m.Livelli);

        m.AggiungiLivello(sopra: false);
        Assert.Equal(-2, m.LivelloCorrente);
        Assert.Equal([2, 1, 0, -1, -2], m.Livelli);
        Assert.Empty(m.StanzeDelLivello);

        // Il livello vuoto resta finché la mappa esiste
        m.CreaStanza(0, 0);
        Assert.Contains(2, m.Livelli);
    }

    [Fact]
    public void CollegaVerticale_ScriveSuEGiu()
    {
        var d = Piccolo();
        var m = Crea(d);
        var a = d.TrovaStanza("a")!;
        m.AggiungiLivello(sopra: true);
        var alto = m.CreaStanza(0, 0);

        m.CollegaVerticale(a, Direzione.Su, alto);

        Assert.Equal(alto.Nome, a.Su);
        Assert.Equal("a", alto.Giu);
        Assert.Throws<ArgumentException>(() => m.CollegaVerticale(a, Direzione.Nord, alto));
    }

    [Fact]
    public void StanzeCollegabili_SoloLivelloSopraOSotto()
    {
        var m = Crea(Esempio());
        var doc = m.Documento;
        var cortile = doc.TrovaStanza("cortile")!;

        Assert.Equal(["sommita"], m.StanzeCollegabili(cortile, Direzione.Su).Select(s => s.Nome));
        Assert.Equal(["cantina"], m.StanzeCollegabili(cortile, Direzione.Giu).Select(s => s.Nome));
    }

    [Fact]
    public async Task CollegaVerticaleAsync_StanzaScelta_Collega()
    {
        var m = Crea(Esempio());
        var giardino = m.Documento.TrovaStanza("giardino")!;
        var sommita = m.Documento.TrovaStanza("sommita")!;
        _dialoghi.RispostaScegliStanza = sommita;

        await m.CollegaVerticaleAsync(giardino, Direzione.Su);

        Assert.Equal([sommita], _dialoghi.StanzeOfferte);
        Assert.Equal("sommita", giardino.Su);
        Assert.Equal("giardino", sommita.Giu);
    }

    [Fact]
    public async Task CollegaVerticaleAsync_Annullato_NonCambiaNulla()
    {
        var m = Crea(Esempio());
        var giardino = m.Documento.TrovaStanza("giardino")!;
        m.Documento.Modificato = false;
        _dialoghi.RispostaScegliStanza = null;

        await m.CollegaVerticaleAsync(giardino, Direzione.Su);

        Assert.Null(giardino.Su);
        Assert.False(m.Documento.Modificato);
    }

    [Fact]
    public async Task DisponiTutto_AnnullatoNonCambiaNulla()
    {
        var d = Esempio();
        var m = Crea(d);
        var torre = d.TrovaStanza("torre")!;
        torre.X = 40;
        d.Modificato = false;
        _dialoghi.RispostaConferma = false;

        await m.DisponiTuttoCommand.ExecuteAsync(null);

        Assert.Contains(nameof(IDialoghi.ConfermaAsync), _dialoghi.Chiamate);
        Assert.Equal(40, torre.X);
        Assert.False(d.Modificato);
    }

    [Fact]
    public async Task DisponiTutto_Confermato_RidisponeTutto()
    {
        var d = Esempio();
        var m = Crea(d);
        var torre = d.TrovaStanza("torre")!;
        torre.X = 40;
        _dialoghi.RispostaConferma = true;

        await m.DisponiTuttoCommand.ExecuteAsync(null);

        Assert.Equal(0, torre.X);
    }

    [Fact]
    public void ImpostaIniziale_ScriveStanzaIniziale()
    {
        var d = Piccolo();
        var m = Crea(d);
        m.ImpostaIniziale(d.TrovaStanza("b")!);
        Assert.Equal("b", d.StanzaIniziale);
    }

    [Fact]
    public void NonRaggiungibili_AggiornateAlleModifiche()
    {
        var d = Piccolo();
        var m = Crea(d);
        var a = d.TrovaStanza("a")!;
        var b = d.TrovaStanza("b")!;
        Assert.Equal([b], m.NonRaggiungibili);

        Collegamenti.Collega(d, a, Direzione.Est, b, senzaRitorno: true);

        Assert.Empty(m.NonRaggiungibili);
        Assert.DoesNotContain(d.Stanze[0], m.NonRaggiungibili);
    }

    [Fact]
    public void CollegamentiDelLivello_SenzaDoppioni()
    {
        var d = Piccolo();
        var a = d.TrovaStanza("a")!;
        var b = d.TrovaStanza("b")!;
        var c = new StanzaViewModel { Nome = "c", HaPosizione = true, X = 0, Y = 1 };
        var e = new StanzaViewModel { Nome = "e", HaPosizione = true, X = 0, Y = -1 };
        d.Stanze.Add(c);
        d.Stanze.Add(e);
        // Porta aperta con le uscite dichiarate anche nelle stanze (da a verso est si attraversa il cancello)
        Collegamenti.Collega(d, a, Direzione.Est, b, senzaRitorno: false);
        d.Porte.Add(new PortaViewModel { Nome = "cancello", Est = "a", Ovest = "b", Aperta = true });
        // Passaggio a doppio senso e porta chiusa
        Collegamenti.Collega(d, a, Direzione.Sud, c, senzaRitorno: false);
        d.Porte.Add(new PortaViewModel { Nome = "portone", Nord = "a", Sud = "e", Aperta = false });
        var m = Crea(d);

        var l = m.CollegamentiDelLivello;

        Assert.Equal(3, l.Count);
        Assert.All(l, x => Assert.True(x.DoppioSenso));
        Assert.Single(l, x => x.Collegamento.Porta is null
            && new[] { x.Collegamento.Da.Nome, x.Collegamento.A.Nome }.Order().SequenceEqual(["a", "c"]));
        Assert.Single(l, x => x.Collegamento.Porta?.Nome == "cancello");
        Assert.Single(l, x => x.Collegamento.Porta?.Nome == "portone");
    }

    [Fact]
    public void CollegamentiDelLivello_SoloLivelloCorrente()
    {
        var m = Crea(Esempio());
        Assert.Contains(m.CollegamentiDelLivello, x => x.Collegamento.Porta?.Nome == "portone");
        Assert.DoesNotContain(m.CollegamentiDelLivello, x => x.Collegamento.Da.Nome is "cantina" or "sommita");

        m.LivelloCorrente = -1;
        Assert.Empty(m.CollegamentiDelLivello);
    }

    [Fact]
    public void CollegamentiDelLivello_SensoUnico()
    {
        var d = Piccolo();
        var m = Crea(d);
        Collegamenti.Collega(d, d.TrovaStanza("a")!, Direzione.Est, d.TrovaStanza("b")!, senzaRitorno: true);

        var c = Assert.Single(m.CollegamentiDelLivello);
        Assert.False(c.DoppioSenso);
        Assert.Equal("a", c.Collegamento.Da.Nome);
    }

    [Fact]
    public async Task EliminaStanzaAsync_ConRiferimenti_ChiedeConferma()
    {
        var d = Esempio();
        var m = Crea(d);
        var giardino = d.TrovaStanza("giardino")!;
        m.Selezione = giardino;
        _dialoghi.RispostaConferma = false;

        await m.EliminaStanzaAsync(giardino);
        Assert.Contains(giardino, d.Stanze);
        Assert.Contains(nameof(IDialoghi.ConfermaAsync), _dialoghi.Chiamate);

        _dialoghi.RispostaConferma = true;
        await m.EliminaStanzaAsync(giardino);
        Assert.DoesNotContain(giardino, d.Stanze);
        Assert.Null(d.TrovaStanza("cortile")!.Est);
        Assert.Null(m.Selezione);
    }

    [Fact]
    public async Task EliminaStanzaAsync_SenzaRiferimenti_NonChiede()
    {
        var d = Piccolo();
        var m = Crea(d);
        var b = d.TrovaStanza("b")!;

        await m.EliminaStanzaAsync(b);

        Assert.DoesNotContain(b, d.Stanze);
        Assert.Empty(_dialoghi.Chiamate);
    }

    [Fact]
    public void TrasformaCollegamento_PortaEPassaggio()
    {
        var d = Piccolo();
        var m = Crea(d);
        Collegamenti.Collega(d, d.TrovaStanza("a")!, Direzione.Est, d.TrovaStanza("b")!, senzaRitorno: false);

        m.TrasformaInPorta(Assert.Single(m.CollegamentiDelLivello).Collegamento);
        var porta = Assert.Single(d.Porte);
        Assert.Same(porta, m.Selezione);
        Assert.Same(porta, Assert.Single(m.CollegamentiDelLivello).Collegamento.Porta);

        m.TrasformaInPassaggio(porta);
        Assert.Empty(d.Porte);
        Assert.Null(m.Selezione);
        Assert.True(Assert.Single(m.CollegamentiDelLivello).DoppioSenso);
    }

    [Fact]
    public void EliminaCollegamento_UscitaEPorta()
    {
        var m = Crea(Esempio());
        var d = m.Documento;

        m.EliminaCollegamento(m.CollegamentiDelLivello.Single(c => c.Collegamento.Porta is null
            && (c.Collegamento.Da.Nome == "ponte" || c.Collegamento.A.Nome == "ponte")).Collegamento);
        Assert.Null(d.TrovaStanza("ponte")!.Nord);
        Assert.Null(d.TrovaStanza("cortile")!.Sud);

        var portone = d.Porte.Single(p => p.Nome == "portone");
        m.EliminaCollegamento(m.CollegamentiDelLivello.Single(c => c.Collegamento.Porta == portone).Collegamento);
        Assert.DoesNotContain(portone, d.Porte);
    }

    [Fact]
    public void Selezione_StanzaSuAltroLivello_CambiaLivello()
    {
        var m = Crea(Esempio());
        m.Selezione = m.Documento.TrovaStanza("cantina");
        Assert.Equal(-1, m.LivelloCorrente);
    }
}

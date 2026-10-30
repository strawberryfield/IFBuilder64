//-----------------------------------------------------------------------
// <copyright file="Tests/RiferimentiTests.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Test delle rinomine a cascata e delle eliminazioni con riferimenti.</summary>
public class RiferimentiTests
{
    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    private static OggettoViewModel Og(DocumentoViewModel d, string nome) => d.Oggetti.First(o => o.Nome == nome);
    private static PortaViewModel Po(DocumentoViewModel d, string nome) => d.Porte.First(p => p.Nome == nome);

    [Fact]
    public void RinominaStanza_AggiornaUsciteDoorLocationInizialeEInitem()
    {
        var d = Esempio();
        var torre = d.TrovaStanza("torre")!;
        var portone = Po(d, "portone");
        Assert.Equal("torre", portone.Sud);
        var stanzeConTorre = d.Stanze.Where(s => Enum.GetValues<Direzione>().Any(x => s.Uscita(x) == "torre")).ToList();
        Assert.NotEmpty(stanzeConTorre);

        Assert.Null(d.RinominaStanza(torre, "mastio"));

        Assert.Equal("mastio", torre.Nome);
        Assert.Equal("mastio", portone.Sud);
        Assert.Equal("mastio", Og(d, "baule").Posizione);
        Assert.Equal("baule@mastio", Og(d, "pergamena").DentroA);
        Assert.All(stanzeConTorre, s => Assert.DoesNotContain("torre", Enum.GetValues<Direzione>().Select(x => s.Uscita(x))));
    }

    [Fact]
    public void RinominaStanza_AggiornaStanzaIniziale()
    {
        var d = Esempio();
        Assert.Null(d.RinominaStanza(d.TrovaStanza("ponte")!, "passerella"));
        Assert.Equal("passerella", d.StanzaIniziale);
    }

    [Fact]
    public void RinominaContenitoreStatico_AggiornaInitem()
    {
        var d = Esempio();
        Assert.Null(d.RinominaOggetto(Og(d, "baule"), "cassa"));
        Assert.Equal("cassa@torre", Og(d, "pergamena").DentroA);
    }

    [Fact]
    public void SpostaContenitoreStatico_AggiornaInitem()
    {
        var d = Esempio();
        d.SpostaOggetto(Og(d, "baule"), "cortile", null);
        Assert.Equal("baule@cortile", Og(d, "pergamena").DentroA);
    }

    [Fact]
    public void ImpostaStatico_AggiornaInitem()
    {
        var d = Esempio();
        d.ImpostaStatico(Og(d, "baule"), false);
        Assert.Equal("baule", Og(d, "pergamena").DentroA);
    }

    [Fact]
    public void RinominaOggetto_AggiornaUnlockitem()
    {
        var d = Esempio();
        Assert.Null(d.RinominaOggetto(Og(d, "chiave"), "chiavone"));
        Assert.Equal("chiavone", Po(d, "portone").OggettoSblocco);
    }

    [Fact]
    public void RinominaDuplicato_Rifiutato()
    {
        var d = Esempio();
        var stanza = d.TrovaStanza("cortile")!;
        Assert.NotNull(d.RinominaStanza(stanza, "torre"));
        Assert.Equal("cortile", stanza.Nome);

        var chiave = Og(d, "chiave");
        Assert.NotNull(d.RinominaOggetto(chiave, "moneta"));
        Assert.Equal("chiave", chiave.Nome);
        Assert.Equal("chiave", Po(d, "portone").OggettoSblocco);

        var cancello = Po(d, "cancello");
        Assert.NotNull(d.RinominaPorta(cancello, "portone"));
        Assert.Equal("cancello", cancello.Nome);
    }

    [Fact]
    public void NomeVuotoOIdentico()
    {
        var d = Esempio();
        var s = d.TrovaStanza("cortile")!;
        Assert.NotNull(d.RinominaStanza(s, "   "));
        Assert.Null(d.RinominaStanza(s, "cortile"));
        Assert.Equal("cortile", s.Nome);
    }

    [Fact]
    public void StessoNomeStaticoInStanzeDiverse_Ammesso()
    {
        var d = Esempio();
        var a = new OggettoViewModel { Nome = "tavolo", Posizione = "cortile", Statico = true };
        var b = new OggettoViewModel { Nome = "sgabello", Posizione = "torre", Statico = true };
        d.Oggetti.Add(a);
        d.Oggetti.Add(b);
        Assert.Null(d.RinominaOggetto(b, "tavolo"));
        Assert.Equal("tavolo@torre", b.Id);
        Assert.Equal("tavolo@cortile", a.Id);
    }

    [Fact]
    public void RinominaOEliminaPlayer_Rifiutato()
    {
        var d = Esempio();
        var player = d.Stanze.First(s => s.EGiocatore);
        Assert.NotNull(d.RinominaStanza(player, "tasca"));
        Assert.Equal("player", player.Nome);
        Assert.Throws<InvalidOperationException>(() => d.Elimina(player));
        Assert.Contains(player, d.Stanze);
    }

    [Fact]
    public void Elimina_SvuotaIRiferimenti()
    {
        var d = Esempio();
        var chiave = Og(d, "chiave");
        Assert.Single(d.TrovaRiferimenti(chiave));
        d.Elimina(chiave);
        Assert.Null(Po(d, "portone").OggettoSblocco);
        Assert.DoesNotContain(chiave, d.Oggetti);
    }

    [Fact]
    public void EliminaStanza_SvuotaUsciteOggettiEIniziale()
    {
        var d = Esempio();
        var torre = d.TrovaStanza("torre")!;
        var rif = d.TrovaRiferimenti(torre);
        Assert.Contains(rif, r => r.Elemento == Po(d, "portone") && r.Campo == "Sud");
        Assert.Contains(rif, r => r.Elemento == Og(d, "baule") && r.Campo == "Posizione");
        Assert.Contains(rif, r => r.Elemento == Og(d, "pergamena") && r.Campo == "DentroA");

        d.Elimina(torre);

        Assert.DoesNotContain(torre, d.Stanze);
        Assert.Null(Po(d, "portone").Sud);
        Assert.Null(Og(d, "baule").Posizione);
        Assert.Null(Og(d, "pergamena").DentroA);

        var ponte = d.TrovaStanza("ponte")!;
        d.Elimina(ponte);
        Assert.Equal(string.Empty, d.StanzaIniziale);
    }

    [Fact]
    public void TrovaRiferimentiPorta_Vuoto()
    {
        var d = Esempio();
        Assert.Empty(d.TrovaRiferimenti(Po(d, "portone")));
    }

    private static (DocumentoViewModel d, OggettoViewModel a, OggettoViewModel b) DueTavoli()
    {
        var d = Esempio();
        var a = new OggettoViewModel { Nome = "tavolo", Posizione = "cortile", Statico = true };
        var b = new OggettoViewModel { Nome = "tavolo", Posizione = "torre", Statico = true };
        d.Oggetti.Add(a);
        d.Oggetti.Add(b);
        Po(d, "portone").OggettoSblocco = "tavolo";
        return (d, a, b);
    }

    [Fact]
    public void RinominaUnoDeiDueOmonimi_NonToccaGliSblocchi()
    {
        var (d, a, _) = DueTavoli();
        Assert.Null(d.RinominaOggetto(a, "scrivania"));
        Assert.Equal("tavolo", Po(d, "portone").OggettoSblocco);
    }

    [Fact]
    public void EliminaUnoDeiDueOmonimi_NonSvuotaGliSblocchi()
    {
        var (d, a, b) = DueTavoli();
        Assert.DoesNotContain(d.TrovaRiferimenti(a), r => r.Campo == "OggettoSblocco");
        d.Elimina(a);
        Assert.Equal("tavolo", Po(d, "portone").OggettoSblocco);
        Assert.Single(d.TrovaRiferimenti(b));
        d.Elimina(b);
        Assert.Null(Po(d, "portone").OggettoSblocco);
    }
}

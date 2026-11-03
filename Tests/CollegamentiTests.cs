//-----------------------------------------------------------------------
// <copyright file="Tests/CollegamentiTests.cs" company="Casasoft">
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

/// <summary>Test dei collegamenti tra stanze e della conversione porta/passaggio.</summary>
public class CollegamentiTests
{
    private static (DocumentoViewModel doc, StanzaViewModel a, StanzaViewModel b) Coppia()
    {
        var d = new DocumentoViewModel();
        var a = new StanzaViewModel { Nome = "a" };
        var b = new StanzaViewModel { Nome = "b" };
        d.Stanze.Add(a);
        d.Stanze.Add(b);
        return (d, a, b);
    }

    [Fact]
    public void Collega_ScriveUscitaEOpposta()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Est, b, false);
        Assert.Equal("b", a.Est);
        Assert.Equal("a", b.Ovest);
    }

    [Fact]
    public void Collega_SenzaRitorno()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Est, b, true);
        Assert.Equal("b", a.Est);
        Assert.Null(b.Ovest);
    }

    [Fact]
    public void Elimina_RimuoveAncheRitornoReciproco()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Est, b, false);
        Collegamenti.Elimina(d, a, Direzione.Est);
        Assert.Null(a.Est);
        Assert.Null(b.Ovest);
    }

    [Fact]
    public void Elimina_NonToccaUscitaDiversaDellAltraStanza()
    {
        var (d, a, b) = Coppia();
        var c = new StanzaViewModel { Nome = "c" };
        d.Stanze.Add(c);
        a.Est = "b";
        b.Ovest = "c";
        Collegamenti.Elimina(d, a, Direzione.Est);
        Assert.Null(a.Est);
        Assert.Equal("c", b.Ovest);
    }

    [Fact]
    public void TrasformaInPorta_RimuoveUsciteECreaPortaChiusa()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Nord, b, false);
        var p = Collegamenti.TrasformaInPorta(d, a, Direzione.Nord);
        Assert.Equal("a", p.Nord);
        Assert.Equal("b", p.Sud);
        Assert.False(p.Aperta);
        Assert.Equal("porta1", p.Nome);
        Assert.Contains(p, d.Porte);
        Assert.Null(a.Nord);
        Assert.Null(b.Sud);
    }

    [Fact]
    public void TrasformaInPorta_UscitaSenzaRitornoCreaPortaADueLati()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Nord, b, true);
        var p = Collegamenti.TrasformaInPorta(d, a, Direzione.Nord);
        Assert.Equal("a", p.Nord);
        Assert.Equal("b", p.Sud);
        Assert.Null(a.Nord);
    }

    [Fact]
    public void TrasformaInPorta_UscitaInesistenteLanciaEccezione()
    {
        var (d, a, _) = Coppia();
        Assert.Throws<InvalidOperationException>(() => Collegamenti.TrasformaInPorta(d, a, Direzione.Nord));
    }

    [Fact]
    public void TrasformaInPorta_NomeUnivoco()
    {
        var (d, a, b) = Coppia();
        d.Porte.Add(new PortaViewModel { Nome = "porta1" });
        Collegamenti.Collega(d, a, Direzione.Nord, b, false);
        Assert.Equal("porta2", Collegamenti.TrasformaInPorta(d, a, Direzione.Nord).Nome);
    }

    [Fact]
    public void TrasformaInPassaggio_RicreaUsciteERimuovePorta()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Nord, b, false);
        var p = Collegamenti.TrasformaInPorta(d, a, Direzione.Nord);
        Collegamenti.TrasformaInPassaggio(d, p);
        Assert.Equal("b", a.Nord);
        Assert.Equal("a", b.Sud);
        Assert.DoesNotContain(p, d.Porte);
    }

    [Fact]
    public void TrasformaInPassaggio_PortaSenzaDueLatiLanciaEccezione()
    {
        var (d, _, _) = Coppia();
        var p = new PortaViewModel { Nome = "p", Nord = "a" };
        d.Porte.Add(p);
        Assert.Throws<InvalidOperationException>(() => Collegamenti.TrasformaInPassaggio(d, p));
    }

    [Fact]
    public void Tutti_IncludeLatiDellePorte()
    {
        var d = new DocumentoViewModel();
        var cortile = new StanzaViewModel { Nome = "cortile" };
        var torre = new StanzaViewModel { Nome = "torre" };
        d.Stanze.Add(cortile);
        d.Stanze.Add(torre);
        var p = new PortaViewModel { Nome = "portone", Nord = "cortile", Sud = "torre", Aperta = false };
        d.Porte.Add(p);
        var tutti = Collegamenti.Tutti(d).ToList();
        Assert.Equal(2, tutti.Count);
        Assert.Contains(new Collegamento(cortile, Direzione.Nord, torre, p), tutti);
        Assert.Contains(new Collegamento(torre, Direzione.Sud, cortile, p), tutti);
    }

    [Fact]
    public void Tutti_IgnoraDestinazioniInesistentiEPorteConLatiDiversiDaDue()
    {
        var (d, a, _) = Coppia();
        a.Est = "fantasma";
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "a" });
        Assert.Empty(Collegamenti.Tutti(d));
    }

    [Fact]
    public void Tutti_PortaApertaConUsciteRestituisceEntrambi()
    {
        var d = ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));
        var tutti = Collegamenti.Tutti(d).ToList();
        Assert.Contains(tutti, c => c.Porta is null && c.Da.Nome == "cortile" && c.Direzione == Direzione.Est && c.A.Nome == "giardino");
        Assert.Contains(tutti, c => c.Porta?.Nome == "cancello");
        Assert.Contains(tutti, c => c.Porta?.Nome == "portone" && c.Da.Nome == "cortile" && c.A.Nome == "torre");
    }

    [Fact]
    public void ImpostaAperta_ScriveERimuoveUsciteNelleStanze()
    {
        var (d, a, b) = Coppia();
        Collegamenti.Collega(d, a, Direzione.Nord, b, false);
        var p = Collegamenti.TrasformaInPorta(d, a, Direzione.Nord);

        Collegamenti.ImpostaAperta(d, p, true);
        Assert.True(p.Aperta);
        Assert.Equal("b", a.Nord);
        Assert.Equal("a", b.Sud);

        Collegamenti.ImpostaAperta(d, p, false);
        Assert.False(p.Aperta);
        Assert.Null(a.Nord);
        Assert.Null(b.Sud);
    }

    [Fact]
    public void ImpostaAperta_ChiudendoNonRimuoveUscitaVersoAltraStanza()
    {
        var (d, a, b) = Coppia();
        var c = new StanzaViewModel { Nome = "c" };
        d.Stanze.Add(c);
        var p = new PortaViewModel { Nome = "p", Nord = "a", Sud = "b", Aperta = true };
        d.Porte.Add(p);
        a.Nord = "c";
        b.Sud = "a";
        Collegamenti.ImpostaAperta(d, p, false);
        Assert.Equal("c", a.Nord);
        Assert.Null(b.Sud);
    }

    [Fact]
    public void CorreggiUscitaDoppia_PortaChiusaRimuoveUscita()
    {
        var (d, a, b) = Coppia();
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "a", Sud = "b", Aperta = false });
        a.Nord = "b";
        Collegamenti.CorreggiUscitaDoppia(d, a, Direzione.Nord);
        Assert.Null(a.Nord);
    }

    [Fact]
    public void CorreggiUscitaDoppia_PortaNullaEquivaleAChiusa()
    {
        var (d, a, b) = Coppia();
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "a", Sud = "b", Aperta = null });
        a.Nord = "b";
        Collegamenti.CorreggiUscitaDoppia(d, a, Direzione.Nord);
        Assert.Null(a.Nord);
    }

    [Fact]
    public void CorreggiUscitaDoppia_PortaApertaNonTocca()
    {
        var (d, a, b) = Coppia();
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "a", Sud = "b", Aperta = true });
        a.Nord = "b";
        Collegamenti.CorreggiUscitaDoppia(d, a, Direzione.Nord);
        Assert.Equal("b", a.Nord);
    }

    [Fact]
    public void CorreggiUscitaDoppia_PortaConUnSoloLatoNonTocca()
    {
        // Porta finta (un solo lato, mai apribile): non scrive uscite, quindi non duplica nulla.
        var (d, a, _) = Coppia();
        d.Porte.Add(new PortaViewModel { Nome = "p", Nord = "a", Aperta = false });
        a.Nord = "b";
        Collegamenti.CorreggiUscitaDoppia(d, a, Direzione.Nord);
        Assert.Equal("b", a.Nord);
    }
}

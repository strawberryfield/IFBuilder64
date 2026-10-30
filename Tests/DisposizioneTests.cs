//-----------------------------------------------------------------------
// <copyright file="Tests/DisposizioneTests.cs" company="Casasoft">
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

/// <summary>Test della disposizione automatica delle stanze sulla griglia.</summary>
public class DisposizioneTests
{
    private static StanzaViewModel Aggiungi(DocumentoViewModel d, string nome)
    {
        var s = new StanzaViewModel { Nome = nome };
        d.Stanze.Add(s);
        return s;
    }

    private static DocumentoViewModel DocConPlayer()
    {
        var d = new DocumentoViewModel();
        Aggiungi(d, "player");
        return d;
    }

    [Fact]
    public void Bfs_RispettaLeDirezioni()
    {
        var d = DocConPlayer();
        var a = Aggiungi(d, "a");
        var b = Aggiungi(d, "b");
        var c = Aggiungi(d, "c");
        d.StanzaIniziale = "a";
        Collegamenti.Collega(d, a, Direzione.Nord, b, false);
        Collegamenti.Collega(d, a, Direzione.Su, c, false);
        DisposizioneAutomatica.Disponi(d, true);
        Assert.Equal((0, 0, 0), (a.X, a.Y, a.Z));
        Assert.Equal((0, -1, 0), (b.X, b.Y, b.Z));
        Assert.Equal((0, 0, 1), (c.X, c.Y, c.Z));
    }

    [Fact]
    public void NessunaSovrapposizione()
    {
        var doc = Editor.Servizi.ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));
        DisposizioneAutomatica.Disponi(doc, true);
        var posizionate = doc.Stanze.Where(s => s.HaPosizione).Select(s => (s.X, s.Y, s.Z)).ToList();
        Assert.Equal(posizionate.Count, posizionate.Distinct().Count());
        Assert.All(doc.Stanze.Where(s => !s.EGiocatore), s => Assert.True(s.HaPosizione));
    }

    [Fact]
    public void CellaOccupata_UsaLaPiuVicina()
    {
        var d = DocConPlayer();
        var a = Aggiungi(d, "a");
        var b = Aggiungi(d, "b");
        var c = Aggiungi(d, "c");
        d.StanzaIniziale = "a";
        a.Est = "b";
        b.Ovest = "a";
        // c e' raggiunta da a verso est dopo b: stessa cella prevista (1,0,0)
        d.Porte.Add(new PortaViewModel { Nome = "p", Est = "a", Ovest = "c" });
        DisposizioneAutomatica.Disponi(d, true);
        Assert.Equal((1, 0, 0), (b.X, b.Y, b.Z));
        Assert.Equal(0, c.Z);
        Assert.Equal(1, Math.Max(Math.Abs(c.X - 1), Math.Abs(c.Y - 0)));
    }

    [Fact]
    public void Isolate_InFilaSottoAlLivello0()
    {
        var d = DocConPlayer();
        var a = Aggiungi(d, "a");
        var b = Aggiungi(d, "b");
        var i1 = Aggiungi(d, "i1");
        var i2 = Aggiungi(d, "i2");
        d.StanzaIniziale = "a";
        Collegamenti.Collega(d, a, Direzione.Sud, b, false);
        DisposizioneAutomatica.Disponi(d, true);
        Assert.Equal(b.Y + 2, i1.Y);
        Assert.Equal(i1.Y, i2.Y);
        Assert.Equal(0, i1.Z);
        Assert.Equal(Math.Min(a.X, b.X), i1.X);
        Assert.Equal(i1.X + 1, i2.X);
    }

    [Fact]
    public void PosizioniEsistentiConservateSeNonTutte()
    {
        var d = DocConPlayer();
        var a = Aggiungi(d, "a");
        var b = Aggiungi(d, "b");
        d.StanzaIniziale = "a";
        Collegamenti.Collega(d, a, Direzione.Est, b, false);
        a.HaPosizione = true; a.X = 5; a.Y = 7; a.Z = 2;
        DisposizioneAutomatica.Disponi(d, false);
        Assert.Equal((5, 7, 2), (a.X, a.Y, a.Z));
        Assert.Equal((6, 7, 2), (b.X, b.Y, b.Z));
        DisposizioneAutomatica.Disponi(d, true);
        Assert.Equal((0, 0, 0), (a.X, a.Y, a.Z));
    }

    [Fact]
    public void PlayerMaiPosizionato()
    {
        var d = DocConPlayer();
        var a = Aggiungi(d, "a");
        d.StanzaIniziale = "player";
        DisposizioneAutomatica.Disponi(d, true);
        Assert.False(d.Stanze[0].HaPosizione);
        Assert.True(a.HaPosizione);
        Assert.Equal((0, 0, 0), (a.X, a.Y, a.Z));
        d.StanzaIniziale = "a";
        DisposizioneAutomatica.Disponi(d, true);
        Assert.False(d.Stanze[0].HaPosizione);
    }
}

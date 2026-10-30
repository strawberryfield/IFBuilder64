//-----------------------------------------------------------------------
// <copyright file="Tests/GeometriaMappaTests.cs" company="Casasoft">
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

using Avalonia;
using Casasoft.IFBuilder64.Editor.Controlli;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Tests;

public class GeometriaMappaTests
{
    private static readonly GeometriaMappa _g = new(40, 1.5, new Point(13, -7));

    [Fact]
    public void CellaDa_InversoDiRettangoloCella()
    {
        for (var x = -3; x <= 3; x++)
            for (var y = -3; y <= 3; y++)
            {
                var c = new Cella(x, y);
                var r = _g.RettangoloCella(c);
                Assert.Equal(60, r.Width, 6);
                Assert.Equal(60, r.Height, 6);
                Assert.Equal(c, _g.CellaDa(r.Center));
                Assert.Equal(c, _g.CellaDa(r.TopLeft));
                Assert.Equal(c, _g.CellaDa(r.BottomRight - new Point(0.01, 0.01)));
            }
        Assert.Equal(new Point(13, -7), _g.RettangoloCella(new Cella(0, 0)).TopLeft);
    }

    [Fact]
    public void BordoDa_RiconosceIQuattroBordiEIlCentro()
    {
        var c = new Cella(1, -2);
        // La fascia di bordo sta dentro il rettangolo disegnato della stanza
        var r = _g.RettangoloStanza(c);
        var fascia = _g.Lato * GeometriaMappa.FasciaBordo;

        Assert.Equal(Bordo.Nord, _g.BordoDa(new Point(r.Center.X, r.Top + fascia / 2), c));
        Assert.Equal(Bordo.Sud, _g.BordoDa(new Point(r.Center.X, r.Bottom - fascia / 2), c));
        Assert.Equal(Bordo.Ovest, _g.BordoDa(new Point(r.Left + fascia / 2, r.Center.Y), c));
        Assert.Equal(Bordo.Est, _g.BordoDa(new Point(r.Right - fascia / 2, r.Center.Y), c));
        Assert.Equal(Bordo.Nessuno, _g.BordoDa(r.Center, c));
        Assert.Equal(Bordo.Nessuno, _g.BordoDa(new Point(r.Left + fascia * 1.2, r.Top + fascia * 1.2), c));
        // Nel margine tra stanza e cella, o fuori dalla cella, non c'è bordo
        var cella = _g.RettangoloCella(c);
        Assert.Equal(Bordo.Nessuno, _g.BordoDa(new Point(cella.Center.X, cella.Top + 1), c));
        Assert.Equal(Bordo.Nessuno, _g.BordoDa(new Point(cella.Right + 5, cella.Center.Y), c));
        // In un angolo vince il bordo più vicino
        Assert.Equal(Bordo.Nord, _g.BordoDa(new Point(r.Left + fascia * 0.8, r.Top + fascia * 0.2), c));
    }

    [Fact]
    public void Tracciato_AdiacentiDuePunti_NonAdiacentiSpezzata()
    {
        var da = new Cella(0, 0);
        var adiacenti = _g.Tracciato(da, Direzione.Est, new Cella(1, 0));
        Assert.Equal(2, adiacenti.Count);
        Assert.Equal(adiacenti[0].Y, adiacenti[1].Y, 6);
        Assert.True(adiacenti[0].X < adiacenti[1].X);
        Assert.Equal(_g.Attacco(da, Direzione.Est), adiacenti[0]);
        Assert.Equal(_g.Attacco(new Cella(1, 0), Direzione.Ovest), adiacenti[1]);

        var spezzata = _g.Tracciato(da, Direzione.Nord, new Cella(3, -2));
        Assert.True(spezzata.Count > 2);
        Assert.Equal(_g.Attacco(da, Direzione.Nord), spezzata[0]);
        Assert.Equal(_g.Attacco(new Cella(3, -2), Direzione.Sud), spezzata[^1]);
        // Solo tratti orizzontali o verticali
        for (var i = 1; i < spezzata.Count; i++)
            Assert.True(Math.Abs(spezzata[i].X - spezzata[i - 1].X) < 1e-9 || Math.Abs(spezzata[i].Y - spezzata[i - 1].Y) < 1e-9);

        Assert.Throws<ArgumentException>(() => _g.Tracciato(da, Direzione.Su, new Cella(0, 0)));
    }

    [Fact]
    public void Distanza_DalTracciato()
    {
        IReadOnlyList<Point> t = [new Point(0, 0), new Point(10, 0), new Point(10, 10)];
        Assert.Equal(0, GeometriaMappa.Distanza(new Point(5, 0), t), 6);
        Assert.Equal(3, GeometriaMappa.Distanza(new Point(5, 3), t), 6);
        Assert.Equal(2, GeometriaMappa.Distanza(new Point(12, 5), t), 6);
        Assert.Equal(5, GeometriaMappa.Distanza(new Point(-3, 4), t), 6);
        Assert.Equal(new Point(10, 0), GeometriaMappa.PuntoMedio(t));
    }
}

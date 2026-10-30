//-----------------------------------------------------------------------
// <copyright file="Editor/Controlli/GeometriaMappa.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Controlli;

/// <summary>Cella della griglia della mappa (sul livello corrente).</summary>
public readonly record struct Cella(int X, int Y);

/// <summary>Bordo di una cella da cui parte il trascinamento di un collegamento.</summary>
public enum Bordo { Nessuno, Nord, Sud, Est, Ovest }

/// <summary>
/// Conversioni tra celle della mappa e coordinate del controllo, dato il lato della cella,
/// lo zoom e l'origine (posizione della cella 0,0 dopo il pan).
/// </summary>
public class GeometriaMappa
{
    /// <summary>Larghezza della fascia di bordo (dentro il rettangolo della stanza), in frazione del lato della cella.</summary>
    public const double FasciaBordo = 0.2;

    /// <summary>Margine tra il bordo della cella e il rettangolo della stanza, in frazione del lato.</summary>
    public const double MargineStanza = 0.12;

    /// <summary>Crea la geometria.</summary>
    /// <param name="latoCella">Lato della cella a zoom 1, in pixel.</param>
    /// <param name="zoom">Fattore di zoom.</param>
    /// <param name="origine">Angolo in alto a sinistra della cella (0,0).</param>
    public GeometriaMappa(double latoCella, double zoom, Point origine)
    {
        Lato = latoCella * zoom;
        Origine = origine;
    }

    /// <summary>Lato effettivo della cella (zoom compreso).</summary>
    public double Lato { get; }

    /// <summary>Angolo in alto a sinistra della cella (0,0).</summary>
    public Point Origine { get; }

    /// <summary>Rettangolo occupato dalla cella.</summary>
    public Rect RettangoloCella(Cella c) => new(Origine.X + c.X * Lato, Origine.Y + c.Y * Lato, Lato, Lato);

    /// <summary>Rettangolo della stanza disegnata nella cella (la cella meno il margine).</summary>
    public Rect RettangoloStanza(Cella c) => RettangoloCella(c).Deflate(Lato * MargineStanza);

    /// <summary>Cella che contiene il punto.</summary>
    public Cella CellaDa(Point p) =>
        new((int)Math.Floor((p.X - Origine.X) / Lato), (int)Math.Floor((p.Y - Origine.Y) / Lato));

    /// <summary>
    /// Bordo della stanza nella cella vicino al punto: fascia larga il 20% del lato della cella lungo
    /// ciascun lato del rettangolo della stanza, all'interno; in un angolo vince il bordo più vicino.
    /// <see cref="Bordo.Nessuno"/> al centro, nel margine tra stanza e cella o fuori dalla cella.
    /// </summary>
    public Bordo BordoDa(Point p, Cella c)
    {
        var r = RettangoloStanza(c);
        if (!r.Contains(p)) return Bordo.Nessuno;
        var fascia = Lato * FasciaBordo;
        var (bordo, distanza) = new[]
        {
            (Bordo.Nord, p.Y - r.Top),
            (Bordo.Sud, r.Bottom - p.Y),
            (Bordo.Ovest, p.X - r.Left),
            (Bordo.Est, r.Right - p.X)
        }.MinBy(b => b.Item2);
        return distanza < fascia ? bordo : Bordo.Nessuno;
    }

    /// <summary>Direzione orizzontale corrispondente al bordo, null per <see cref="Bordo.Nessuno"/>.</summary>
    public static Direzione? DirezioneDi(Bordo b) => b switch
    {
        Bordo.Nord => Direzione.Nord,
        Bordo.Sud => Direzione.Sud,
        Bordo.Est => Direzione.Est,
        Bordo.Ovest => Direzione.Ovest,
        _ => null
    };

    /// <summary>Punto medio del lato della stanza rivolto nella direzione (solo direzioni orizzontali).</summary>
    public Point Attacco(Cella c, Direzione d)
    {
        var r = RettangoloStanza(c);
        return d switch
        {
            Direzione.Nord => new Point(r.Center.X, r.Top),
            Direzione.Sud => new Point(r.Center.X, r.Bottom),
            Direzione.Est => new Point(r.Right, r.Center.Y),
            Direzione.Ovest => new Point(r.Left, r.Center.Y),
            _ => throw new ArgumentException($"Direzione non orizzontale: {d}", nameof(d))
        };
    }

    /// <summary>
    /// Punti del collegamento dalla cella <paramref name="da"/> verso <paramref name="a"/> uscendo in
    /// <paramref name="dir"/> ed entrando dal lato opposto: retta se le celle sono adiacenti in quella
    /// direzione, altrimenti spezzata a tratti orizzontali e verticali che corre lungo i bordi delle celle.
    /// </summary>
    public IReadOnlyList<Point> Tracciato(Cella da, Direzione dir, Cella a)
    {
        if (dir is Direzione.Su or Direzione.Giu)
            throw new ArgumentException($"Direzione non orizzontale: {dir}", nameof(dir));

        var inizio = Attacco(da, dir);
        var fine = Attacco(a, dir.Opposta());
        var (dx, dy, _) = dir.Spostamento();
        if (a.X == da.X + dx && a.Y == da.Y + dy) return [inizio, fine];

        // Brevi tratti fino al bordo della cella, poi un gomito lungo lo spazio tra le celle
        var uscita = Lato * MargineStanza;
        var p1 = new Point(inizio.X + dx * uscita, inizio.Y + dy * uscita);
        var p2 = new Point(fine.X - dx * uscita, fine.Y - dy * uscita);
        var gomito = dy != 0 ? new Point(p2.X, p1.Y) : new Point(p1.X, p2.Y);
        return [inizio, p1, gomito, p2, fine];
    }

    /// <summary>Distanza minima tra il punto e la spezzata.</summary>
    public static double Distanza(Point p, IReadOnlyList<Point> tracciato)
    {
        var minima = double.MaxValue;
        for (var i = 1; i < tracciato.Count; i++)
            minima = Math.Min(minima, DistanzaSegmento(p, tracciato[i - 1], tracciato[i]));
        return minima;
    }

    /// <summary>Punto a metà lunghezza della spezzata.</summary>
    public static Point PuntoMedio(IReadOnlyList<Point> tracciato)
    {
        var lunghezze = new double[tracciato.Count];
        for (var i = 1; i < tracciato.Count; i++)
            lunghezze[i] = Lunghezza(tracciato[i] - tracciato[i - 1]);
        var resto = lunghezze.Sum() / 2;
        for (var i = 1; i < tracciato.Count; i++)
        {
            if (resto <= lunghezze[i] && lunghezze[i] > 0)
            {
                var t = resto / lunghezze[i];
                return tracciato[i - 1] + (tracciato[i] - tracciato[i - 1]) * t;
            }
            resto -= lunghezze[i];
        }
        return tracciato[^1];
    }

    private static double Lunghezza(Point v) => Math.Sqrt(v.X * v.X + v.Y * v.Y);

    private static double DistanzaSegmento(Point p, Point a, Point b)
    {
        var ab = b - a;
        var quadrato = ab.X * ab.X + ab.Y * ab.Y;
        var t = quadrato == 0 ? 0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / quadrato, 0, 1);
        return Lunghezza(p - (a + ab * t));
    }
}

//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/DisposizioneAutomatica.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>Disposizione automatica delle stanze sulla griglia della mappa.</summary>
public static class DisposizioneAutomatica
{
    /// <summary>
    /// Assegna una posizione alle stanze che non ne hanno (tutte, se <paramref name="tutte"/> è true).
    /// Visita in ampiezza i collegamenti a partire dalla stanza iniziale; le stanze non raggiunte
    /// vengono messe in fila sotto il livello 0. La stanza "player" non viene mai posizionata.
    /// </summary>
    public static void Disponi(DocumentoViewModel doc, bool tutte)
    {
        if (tutte)
            foreach (var s in doc.Stanze)
                s.HaPosizione = false;

        var occupate = doc.Stanze.Where(s => s.HaPosizione && !s.EGiocatore)
            .Select(s => (s.X, s.Y, s.Z)).ToHashSet();
        var collegamenti = Collegamenti.Tutti(doc).Where(c => !c.Da.EGiocatore && !c.A.EGiocatore).ToList();

        void Posiziona(StanzaViewModel s, int x, int y, int z)
        {
            (x, y) = CellaLibera(occupate, x, y, z);
            s.X = x; s.Y = y; s.Z = z;
            s.HaPosizione = true;
            occupate.Add((x, y, z));
        }

        var iniziale = doc.TrovaStanza(doc.StanzaIniziale);
        if (iniziale is { EGiocatore: false })
        {
            if (!iniziale.HaPosizione) Posiziona(iniziale, 0, 0, 0);
            var visitate = new HashSet<StanzaViewModel> { iniziale };
            var coda = new Queue<StanzaViewModel>();
            coda.Enqueue(iniziale);
            while (coda.Count > 0)
            {
                var da = coda.Dequeue();
                foreach (var c in collegamenti.Where(c => c.Da == da))
                {
                    if (!visitate.Add(c.A)) continue;
                    if (!c.A.HaPosizione)
                    {
                        var (dx, dy, dz) = c.Direzione.Spostamento();
                        Posiziona(c.A, da.X + dx, da.Y + dy, da.Z + dz);
                    }
                    coda.Enqueue(c.A);
                }
            }
        }

        var rimaste = doc.Stanze.Where(s => !s.EGiocatore && !s.HaPosizione).ToList();
        if (rimaste.Count == 0) return;

        var livello0 = occupate.Where(p => p.Z == 0).ToList();
        var riga = livello0.Count == 0 ? 0 : livello0.Max(p => p.Y) + 2;
        var colonna = livello0.Count == 0 ? 0 : livello0.Min(p => p.X);
        foreach (var s in rimaste)
        {
            while (occupate.Contains((colonna, riga, 0))) colonna++;
            Posiziona(s, colonna, riga, 0);
        }
    }

    /// <summary>La cella indicata o, se occupata, la più vicina libera (anelli di Chebyshev, per riga e poi colonna).</summary>
    private static (int x, int y) CellaLibera(HashSet<(int X, int Y, int Z)> occupate, int x, int y, int z)
    {
        if (!occupate.Contains((x, y, z))) return (x, y);
        for (var r = 1; ; r++)
            for (var cy = y - r; cy <= y + r; cy++)
                for (var cx = x - r; cx <= x + r; cx++)
                    if (Math.Max(Math.Abs(cx - x), Math.Abs(cy - y)) == r && !occupate.Contains((cx, cy, z)))
                        return (cx, cy);
    }
}

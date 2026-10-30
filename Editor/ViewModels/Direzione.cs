//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/Direzione.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Direzioni di uscita, nello stesso ordine di <c>Game.RoomExits</c>.</summary>
public enum Direzione { Nord, Sud, Est, Ovest, Su, Giu }

/// <summary>Operazioni di supporto sulle direzioni.</summary>
public static class DirezioneEstensioni
{
    /// <summary>Direzione opposta (nord/sud, est/ovest, su/giù).</summary>
    public static Direzione Opposta(this Direzione d) => d switch
    {
        Direzione.Nord => Direzione.Sud,
        Direzione.Sud => Direzione.Nord,
        Direzione.Est => Direzione.Ovest,
        Direzione.Ovest => Direzione.Est,
        Direzione.Su => Direzione.Giu,
        Direzione.Giu => Direzione.Su,
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };

    /// <summary>Spostamento della cella sulla mappa: nord y-1, sud y+1, est x+1, ovest x-1, su z+1, giù z-1.</summary>
    public static (int dx, int dy, int dz) Spostamento(this Direzione d) => d switch
    {
        Direzione.Nord => (0, -1, 0),
        Direzione.Sud => (0, 1, 0),
        Direzione.Est => (1, 0, 0),
        Direzione.Ovest => (-1, 0, 0),
        Direzione.Su => (0, 0, 1),
        Direzione.Giu => (0, 0, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };
}

/// <summary>Elemento (stanza o porta) con le sei uscite direzionali.</summary>
public interface IConUscite
{
    /// <summary>Nome della destinazione nella direzione data, o null.</summary>
    string? Uscita(Direzione d);

    /// <summary>Imposta (o rimuove, con null) l'uscita nella direzione data.</summary>
    void ImpostaUscita(Direzione d, string? stanza);
}

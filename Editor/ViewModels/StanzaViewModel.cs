//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/StanzaViewModel.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Stanza del gioco in modifica.</summary>
public partial class StanzaViewModel : ObservableObject, IConUscite
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EGiocatore))]
    private string _nome = string.Empty;

    [ObservableProperty] private string _descrizione = string.Empty;
    [ObservableProperty] private string? _nord;
    [ObservableProperty] private string? _sud;
    [ObservableProperty] private string? _est;
    [ObservableProperty] private string? _ovest;
    [ObservableProperty] private string? _su;
    [ObservableProperty] private string? _giu;

    /// <summary>Indica se la stanza ha una posizione sulla mappa (altrimenti <c>map</c> non viene scritto).</summary>
    [ObservableProperty] private bool _haPosizione;
    [ObservableProperty] private int _x;
    [ObservableProperty] private int _y;
    [ObservableProperty] private int _z;

    /// <summary>La stanza speciale "player": oggetti in mano al giocatore.</summary>
    public bool EGiocatore => Nome == "player";

    /// <inheritdoc />
    public string? Uscita(Direzione d) => d switch
    {
        Direzione.Nord => Nord,
        Direzione.Sud => Sud,
        Direzione.Est => Est,
        Direzione.Ovest => Ovest,
        Direzione.Su => Su,
        _ => Giu
    };

    /// <inheritdoc />
    public void ImpostaUscita(Direzione d, string? stanza)
    {
        switch (d)
        {
            case Direzione.Nord: Nord = stanza; break;
            case Direzione.Sud: Sud = stanza; break;
            case Direzione.Est: Est = stanza; break;
            case Direzione.Ovest: Ovest = stanza; break;
            case Direzione.Su: Su = stanza; break;
            default: Giu = stanza; break;
        }
    }

    /// <summary>Crea il ViewModel a partire dal modello JSON.</summary>
    public static StanzaViewModel Da(Room r) => new()
    {
        Nome = r.name,
        Descrizione = r.description,
        Nord = r.north,
        Sud = r.south,
        Est = r.east,
        Ovest = r.west,
        Su = r.up,
        Giu = r.down,
        HaPosizione = r.map is not null,
        X = r.map?.x ?? 0,
        Y = r.map?.y ?? 0,
        Z = r.map?.z ?? 0
    };

    /// <summary>Converte nel modello JSON; <c>map</c> è scritto solo se la stanza ha una posizione.</summary>
    public Room ToRoom() => new()
    {
        name = Nome,
        description = Descrizione,
        north = Nord,
        south = Sud,
        east = Est,
        west = Ovest,
        up = Su,
        down = Giu,
        map = HaPosizione ? new MapPosition { x = X, y = Y, z = Z } : null
    };
}

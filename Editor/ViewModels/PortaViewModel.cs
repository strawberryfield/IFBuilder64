//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/PortaViewModel.cs" company="Casasoft">
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

/// <summary>Porta del gioco in modifica.</summary>
public partial class PortaViewModel : ObservableObject, IConUscite
{
    [ObservableProperty] private string _nome = string.Empty;
    [ObservableProperty] private string _descrizione = string.Empty;

    /// <summary>Messaggio mostrato se si tenta di aprire la porta (<c>denyopen</c>).</summary>
    [ObservableProperty] private string? _messaggioNegato;

    [ObservableProperty] private string? _oggettoSblocco;
    [ObservableProperty] private bool? _aperta;
    [ObservableProperty] private string? _nord;
    [ObservableProperty] private string? _sud;
    [ObservableProperty] private string? _est;
    [ObservableProperty] private string? _ovest;
    [ObservableProperty] private string? _su;
    [ObservableProperty] private string? _giu;

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
    public static PortaViewModel Da(Door d) => new()
    {
        Nome = d.name,
        Descrizione = d.description,
        MessaggioNegato = d.denyopen,
        OggettoSblocco = d.unlockitem,
        Aperta = d.open,
        Nord = d.north,
        Sud = d.south,
        Est = d.east,
        Ovest = d.west,
        Su = d.up,
        Giu = d.down
    };

    /// <summary>Converte nel modello JSON.</summary>
    public Door ToDoor() => new()
    {
        name = Nome,
        description = Descrizione,
        denyopen = MessaggioNegato,
        unlockitem = OggettoSblocco,
        open = Aperta,
        north = Nord,
        south = Sud,
        east = Est,
        west = Ovest,
        up = Su,
        down = Giu
    };
}

//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/OggettoViewModel.cs" company="Casasoft">
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

/// <summary>Oggetto del gioco in modifica.</summary>
public partial class OggettoViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Id))]
    [NotifyPropertyChangedFor(nameof(IdAutomatico))]
    private string _nome = string.Empty;

    /// <summary>Identificatore scelto dall'utente; null se l'id è automatico (segue nome, posizione e flag statico).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Id))]
    private string? _idPersonalizzato;

    [ObservableProperty] private string _descrizione = string.Empty;
    [ObservableProperty] private string? _descrizioneAperto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Id))]
    [NotifyPropertyChangedFor(nameof(IdAutomatico))]
    private string? _posizione;

    /// <summary>Contenitore in cui si trova l'oggetto (<c>initem</c>).</summary>
    [ObservableProperty] private string? _dentroA;

    [ObservableProperty] private string? _oggettoSblocco;

    /// <summary>Oggetto non trasportabile (<c>notportable</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Id))]
    [NotifyPropertyChangedFor(nameof(IdAutomatico))]
    private bool? _statico;

    [ObservableProperty] private bool? _apribile;
    [ObservableProperty] private bool? _chiudibile;
    [ObservableProperty] private bool? _aperto;
    [ObservableProperty] private bool? _accendibile;
    [ObservableProperty] private bool? _acceso;
    [ObservableProperty] private bool? _eContenitore;

    /// <summary>Identificatore calcolato con la regola di <c>Item.DefaultId</c>: "nome@posizione" per gli oggetti statici con posizione, altrimenti il nome.</summary>
    public string IdAutomatico => Item.DefaultId(Nome, Statico, Posizione);

    /// <summary>Identificatore effettivo: quello personalizzato se presente, altrimenti quello automatico.</summary>
    public string Id => IdPersonalizzato ?? IdAutomatico;

    /// <summary>Crea il ViewModel a partire dal modello JSON.</summary>
    public static OggettoViewModel Da(Item i) => new()
    {
        Nome = i.name,
        IdPersonalizzato = i.id == Item.DefaultId(i.name, i.notportable, i.location) ? null : i.id,
        Descrizione = i.description,
        DescrizioneAperto = i.opendescription,
        Posizione = i.location,
        DentroA = i.initem,
        OggettoSblocco = i.unlockitem,
        Statico = i.notportable,
        Apribile = i.openable,
        Chiudibile = i.closeable,
        Aperto = i.open,
        Accendibile = i.switchable,
        Acceso = i.switchedon,
        EContenitore = i.container
    };

    /// <summary>Converte nel modello JSON.</summary>
    public Item ToItem() => new()
    {
        name = Nome,
        id = IdPersonalizzato,
        description = Descrizione,
        opendescription = DescrizioneAperto,
        location = Posizione,
        initem = DentroA,
        unlockitem = OggettoSblocco,
        notportable = Statico,
        openable = Apribile,
        closeable = Chiudibile,
        open = Aperto,
        switchable = Accendibile,
        switchedon = Acceso,
        container = EContenitore
    };
}

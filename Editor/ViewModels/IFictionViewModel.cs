//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/IFictionViewModel.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Common.Json;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Metadati iFiction (Babel) del gioco.</summary>
public partial class IFictionViewModel : ObservableObject
{
    [ObservableProperty] private string _titolo = string.Empty;
    [ObservableProperty] private string _autore = string.Empty;
    [ObservableProperty] private string? _ifid;
    [ObservableProperty] private string _sottotitolo = string.Empty;
    [ObservableProperty] private string _genere = string.Empty;
    [ObservableProperty] private int? _primaPubblicazione;
    [ObservableProperty] private int? _versione;
    [ObservableProperty] private string? _descrizione;
    [ObservableProperty] private string? _lingua;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private string? _email;

    /// <summary>Dialoghi per chiedere conferma prima di sovrascrivere l'IFID; null per sovrascrivere senza chiedere.</summary>
    public IDialoghi? Dialoghi { get; set; }

    /// <summary>Genera un nuovo IFID (GUID maiuscolo), chiedendo conferma se ne esiste già uno.</summary>
    [RelayCommand]
    private async Task GeneraIfid()
    {
        if (!string.IsNullOrEmpty(Ifid) && Dialoghi is not null
            && !await Dialoghi.ConfermaAsync(Testi.Gioco_SovrascriviIfid_Titolo, Testi.Gioco_SovrascriviIfid_Messaggio))
            return;
        Ifid = Guid.NewGuid().ToString().ToUpperInvariant();
    }

    /// <summary>Crea il ViewModel a partire dal modello JSON.</summary>
    public static IFictionViewModel Da(IFiction f) => new()
    {
        Titolo = f.title,
        Autore = f.author,
        Ifid = f.ifid,
        Sottotitolo = f.headline,
        Genere = f.genre,
        PrimaPubblicazione = f.firstpublished,
        Versione = f.version,
        Descrizione = f.description,
        Lingua = f.language,
        Url = f.url,
        Email = f.email
    };

    /// <summary>Converte nel modello JSON.</summary>
    public IFiction ToIFiction() => new()
    {
        title = Titolo,
        author = Autore,
        ifid = Nullo(Ifid),
        headline = Sottotitolo,
        genre = Genere,
        firstpublished = PrimaPubblicazione,
        version = Versione,
        description = Nullo(Descrizione),
        language = Nullo(Lingua),
        url = Nullo(Url),
        email = Nullo(Email)
    };

    /// <summary>Testo vuoto o di soli spazi come null, così il JSON omette il campo.</summary>
    private static string? Nullo(string? testo) => string.IsNullOrWhiteSpace(testo) ? null : testo;
}

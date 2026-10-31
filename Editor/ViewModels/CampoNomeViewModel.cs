//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/CampoNomeViewModel.cs" company="Casasoft">
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

using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>
/// Campo di testo del nome di un elemento: la rinomina viene tentata solo alla conferma
/// e un eventuale errore resta visibile senza perdere quanto digitato.
/// </summary>
public partial class CampoNomeViewModel : ObservableObject
{
    private readonly Func<string> _leggi;
    private readonly Func<string, string?> _rinomina;

    /// <summary>Testo del campo.</summary>
    [ObservableProperty] private string _testo;

    /// <summary>Errore dell'ultima conferma, oppure null.</summary>
    [ObservableProperty] private string? _errore;

    /// <summary>Crea il campo con il nome corrente letto da <paramref name="leggi"/>.</summary>
    /// <param name="leggi">Legge il nome attuale dell'elemento.</param>
    /// <param name="rinomina">Rinomina l'elemento; restituisce null se riuscita, altrimenti l'errore.</param>
    public CampoNomeViewModel(Func<string> leggi, Func<string, string?> rinomina)
    {
        _leggi = leggi;
        _rinomina = rinomina;
        _testo = leggi();
    }

    /// <summary>Tenta la rinomina: in caso di errore lo mostra e lascia il testo digitato.</summary>
    public void Conferma()
    {
        Errore = _rinomina(Testo);
        if (Errore is null) Testo = _leggi();
    }

    /// <summary>Conferma solo se il testo è diverso dal nome corrente (modifica digitata e non confermata).</summary>
    public void ConfermaSeModificato()
    {
        if (Testo != _leggi()) Conferma();
    }

    /// <summary>Ripristina il nome corrente e cancella l'errore.</summary>
    public void Annulla()
    {
        Testo = _leggi();
        Errore = null;
    }
}

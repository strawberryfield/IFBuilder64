//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/IDialoghi.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>
/// Dialoghi usati dai ViewModel; separati dall'interfaccia per poterli sostituire nei test.
/// </summary>
public interface IDialoghi
{
    /// <summary>Chiede una conferma sì/no.</summary>
    Task<bool> ConfermaAsync(string titolo, string messaggio);

    /// <summary>Chiede se salvare le modifiche: true salva, false non salvare, null annulla.</summary>
    Task<bool?> ConfermaSalvataggioAsync();

    /// <summary>Sceglie un file di gioco da aprire; null se annullato.</summary>
    Task<string?> ApriFileAsync();

    /// <summary>Sceglie dove salvare il file di gioco; null se annullato.</summary>
    Task<string?> SalvaFileAsync(string? suggerito);

    /// <summary>Sceglie la lingua del template per un gioco nuovo; null se annullato.</summary>
    Task<TemplateLanguage?> ScegliTemplateAsync();

    /// <summary>Mostra un messaggio di errore.</summary>
    Task MostraErroreAsync(string messaggio);

    /// <summary>Modifica il nome base dei file generati; null se annullato, vuoto per tornare al predefinito.</summary>
    Task<string?> OpzioniCompilazioneAsync(string corrente);

    /// <summary>Sceglie una stanza dall'elenco; null se annullato.</summary>
    Task<StanzaViewModel?> ScegliStanzaAsync(string titolo, IReadOnlyList<StanzaViewModel> stanze);
}

//-----------------------------------------------------------------------
// <copyright file="Editor/Views/OpzioniCompilazioneWindow.axaml.cs" company="Casasoft">
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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>
/// Opzioni di compilazione: nome base dei file generati.
/// Restituisce il testo inserito (vuoto = predefinito) oppure null se annullata.
/// </summary>
public partial class OpzioniCompilazioneWindow : Window
{
    /// <summary>Crea la finestra (usato dal designer).</summary>
    public OpzioniCompilazioneWindow() => InitializeComponent();

    /// <summary>Crea la finestra con il nome base corrente.</summary>
    public OpzioniCompilazioneWindow(string corrente) : this() => TestoOutput.Text = corrente;

    /// <summary>Sceglie la cartella mantenendo il nome del file.</summary>
    private async void Sfoglia_Click(object? sender, RoutedEventArgs e)
    {
        var corrente = TestoOutput.Text ?? string.Empty;
        var opzioni = new FolderPickerOpenOptions { Title = Title, AllowMultiple = false };
        var cartellaCorrente = Path.GetDirectoryName(corrente);
        if (!string.IsNullOrEmpty(cartellaCorrente) && Directory.Exists(cartellaCorrente))
            opzioni.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(cartellaCorrente);
        var scelte = await StorageProvider.OpenFolderPickerAsync(opzioni);
        if (scelte.Count == 0 || scelte[0].TryGetLocalPath() is not { } cartella) return;
        var nome = Path.GetFileName(corrente);
        TestoOutput.Text = Path.Combine(cartella, string.IsNullOrEmpty(nome) ? "gioco" : nome);
    }

    private void Ok_Click(object? sender, RoutedEventArgs e) => Close(TestoOutput.Text ?? string.Empty);

    private void Annulla_Click(object? sender, RoutedEventArgs e) => Close(null);
}

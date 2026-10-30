//-----------------------------------------------------------------------
// <copyright file="Editor/Views/DialoghiAvalonia.cs" company="Casasoft">
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
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>
/// Implementazione di <see cref="IDialoghi"/> con le finestre di Avalonia:
/// selettori di file dello <see cref="IStorageProvider"/> e semplici finestre modali.
/// </summary>
/// <param name="proprietaria">Finestra a cui i dialoghi sono modali.</param>
public class DialoghiAvalonia(Window proprietaria) : IDialoghi
{
    private static FilePickerFileType[] FiltroJson =>
        [new FilePickerFileType(Testi.Filtro_Json) { Patterns = ["*.json"] }];

    /// <inheritdoc />
    public async Task<bool> ConfermaAsync(string titolo, string messaggio) =>
        await Scelta(titolo, messaggio, (Testi.Pulsante_Si, true), (Testi.Pulsante_No, false)) is true;

    /// <inheritdoc />
    public async Task<bool?> ConfermaSalvataggioAsync() =>
        await Scelta(Testi.ConfermaSalvataggio_Titolo, Testi.ConfermaSalvataggio_Messaggio,
            (Testi.Pulsante_Si, true), (Testi.Pulsante_No, false), (Testi.Pulsante_Annulla, null)) as bool?;

    /// <inheritdoc />
    public async Task<string?> ApriFileAsync()
    {
        var file = await proprietaria.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = FiltroJson
        });
        return file.Count == 0 ? null : file[0].TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> SalvaFileAsync(string? suggerito)
    {
        var opzioni = new FilePickerSaveOptions
        {
            DefaultExtension = "json",
            FileTypeChoices = FiltroJson,
            ShowOverwritePrompt = true,
            SuggestedFileName = suggerito is null ? null : Path.GetFileName(suggerito)
        };
        var cartella = Path.GetDirectoryName(suggerito);
        if (!string.IsNullOrEmpty(cartella) && Directory.Exists(cartella))
            opzioni.SuggestedStartLocation = await proprietaria.StorageProvider.TryGetFolderFromPathAsync(cartella);
        var file = await proprietaria.StorageProvider.SaveFilePickerAsync(opzioni);
        return file?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<TemplateLanguage?> ScegliTemplateAsync() =>
        await Scelta(Testi.Template_Titolo, Testi.Template_Messaggio,
            (Testi.Template_Italiano, TemplateLanguage.Italiano),
            (Testi.Template_Inglese, TemplateLanguage.English),
            (Testi.Pulsante_Annulla, null)) as TemplateLanguage?;

    /// <inheritdoc />
    public Task MostraErroreAsync(string messaggio) =>
        Scelta(Testi.Dialogo_Errore, messaggio, (Testi.Pulsante_Ok, null));

    /// <inheritdoc />
    public Task<string?> OpzioniCompilazioneAsync(string corrente) =>
        new OpzioniCompilazioneWindow(corrente).ShowDialog<string?>(proprietaria);

    /// <inheritdoc />
    public async Task<StanzaViewModel?> ScegliStanzaAsync(string titolo, IReadOnlyList<StanzaViewModel> stanze)
    {
        StanzaViewModel? risultato = null;
        var elenco = new ListBox
        {
            ItemsSource = stanze.Select(s => s.Nome).ToList(),
            Height = 240,
            SelectedIndex = stanze.Count > 0 ? 0 : -1
        };
        var ok = new Button { Content = Testi.Pulsante_Ok, IsDefault = true, IsEnabled = stanze.Count > 0 };
        var annulla = new Button { Content = Testi.Pulsante_Annulla, IsCancel = true };
        var finestra = new Window
        {
            Title = titolo,
            Width = 320,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    elenco,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { ok, annulla }
                    }
                }
            }
        };
        void Conferma()
        {
            if (elenco.SelectedIndex < 0) return;
            risultato = stanze[elenco.SelectedIndex];
            finestra.Close();
        }
        ok.Click += (_, _) => Conferma();
        elenco.DoubleTapped += (_, _) => Conferma();
        annulla.Click += (_, _) => finestra.Close();
        await finestra.ShowDialog(proprietaria);
        return risultato;
    }

    /// <summary>
    /// Mostra un messaggio con un pulsante per ogni scelta e restituisce il valore di quella premuta;
    /// chiudere la finestra equivale all'ultima scelta.
    /// </summary>
    private async Task<object?> Scelta(string titolo, string messaggio, params (string Testo, object? Valore)[] scelte)
    {
        var pulsanti = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var finestra = new Window
        {
            Title = titolo,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new SelectableTextBlock { Text = messaggio, TextWrapping = TextWrapping.Wrap },
                    pulsanti
                }
            }
        };
        var risultato = scelte[^1].Valore;
        for (var i = 0; i < scelte.Length; i++)
        {
            var (testo, valore) = scelte[i];
            var pulsante = new Button { Content = testo, IsDefault = i == 0, IsCancel = i == scelte.Length - 1 };
            pulsante.Click += (_, _) =>
            {
                risultato = valore;
                finestra.Close();
            };
            pulsanti.Children.Add(pulsante);
        }
        await finestra.ShowDialog(proprietaria);
        return risultato;
    }
}

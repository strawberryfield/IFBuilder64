//-----------------------------------------------------------------------
// <copyright file="Editor/App.axaml.cs" company="Casasoft">
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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;

namespace Casasoft.IFBuilder64.Editor;

/// <summary>
/// Applicazione Avalonia dell'editor.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    private IDialoghi? _dialoghi;
    private bool _erroreInCorso;

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var finestra = new MainWindow();
            _dialoghi = new DialoghiAvalonia(finestra);
            var vm = new MainWindowViewModel(_dialoghi, ImpostazioniUtente.Carica());
            finestra.DataContext = vm;
            desktop.MainWindow = finestra;

            // Rete di sicurezza: un'eccezione sfuggita a un comando non deve chiudere l'editor.
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                e.Handled = true;
                MostraErroreImprevisto(e.Exception);
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                e.SetObserved();
                Dispatcher.UIThread.Post(() => MostraErroreImprevisto(e.Exception));
            };

            // Il primo argomento della riga di comando è il file da aprire.
            var percorso = desktop.Args is { Length: > 0 } argomenti ? argomenti[0] : null;
            EventHandler? aperta = null;
            aperta = async (_, _) =>
            {
                finestra.Opened -= aperta;
                await vm.AvviaAsync(percorso);
            };
            finestra.Opened += aperta;
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Mostra un'eccezione non gestita in un dialogo di errore, senza rientrare se il dialogo stesso fallisce.</summary>
    private async void MostraErroreImprevisto(Exception ex)
    {
        if (_dialoghi is null || _erroreInCorso) return;
        _erroreInCorso = true;
        try
        {
            await _dialoghi.MostraErroreAsync(string.Format(Testi.Errore_Imprevisto, ex.GetBaseException().Message));
        }
        catch (Exception)
        {
            // il dialogo non si può mostrare (ad esempio durante la chiusura): l'editor resta comunque aperto
        }
        finally
        {
            _erroreInCorso = false;
        }
    }
}

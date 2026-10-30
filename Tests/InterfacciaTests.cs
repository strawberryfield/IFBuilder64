//-----------------------------------------------------------------------
// <copyright file="Tests/InterfacciaTests.cs" company="Casasoft">
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
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;
using Casasoft.IFBuilder64.Tests;

[assembly: AvaloniaTestApplication(typeof(AppTest))]

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Test di base della finestra principale, senza schermo e senza confronto di immagini.</summary>
public sealed class InterfacciaTests : IDisposable
{
    private readonly string _cartella = Directory.CreateTempSubdirectory().FullName;
    private readonly DialoghiFinti _dialoghi = new();

    public void Dispose() => Directory.Delete(_cartella, true);

    private (MainWindow Finestra, MainWindowViewModel Vm) ApriFinestra()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "editor.json"));
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        Dispatcher.UIThread.RunJobs();
        return (finestra, vm);
    }

    private string CopiaEsempio()
    {
        var p = Path.Combine(_cartella, "esempio.json");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"), p, true);
        return p;
    }

    [AvaloniaFact]
    public void Finestra_SiApre()
    {
        var (finestra, vm) = ApriFinestra();

        Assert.True(finestra.IsVisible);
        Assert.Equal(vm.Titolo, finestra.Title);
        var schede = finestra.FindControl<TabControl>("Schede");
        Assert.NotNull(schede);
        Assert.Equal(5, schede.ItemCount);
        Assert.NotNull(finestra.FindControl<ContentControl>("ContenutoProprieta"));
        Assert.NotNull(finestra.FindControl<ListBox>("ListaAvvisi"));
        finestra.Close();
    }

    [AvaloniaFact]
    public async Task CaricaDocumento_TitoloAggiornato()
    {
        var (finestra, vm) = ApriFinestra();
        _dialoghi.RispostaApriFile = CopiaEsempio();

        await vm.ApriCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("IFBuilder64 Editor - esempio.json", finestra.Title);
        finestra.Close();
    }

    [AvaloniaFact]
    public async Task F5_CompilaInCartellaTemporanea_LogContieneFileGenerati()
    {
        var (finestra, vm) = ApriFinestra();
        _dialoghi.RispostaApriFile = CopiaEsempio();
        await vm.ApriCommand.ExecuteAsync(null);
        vm.OutputBase = Path.Combine(_cartella, "esempio");
        Dispatcher.UIThread.RunJobs();

        finestra.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
        finestra.KeyRelease(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);

        var limite = DateTime.UtcNow.AddSeconds(10);
        while ((vm.CompilazioneInCorso || vm.LogCompilazione.Count == 0) && DateTime.UtcNow < limite)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(20);
        }

        Assert.False(vm.CompilazioneInCorso);
        Assert.Contains(vm.LogCompilazione, r => r.EndsWith("esempiodt.prg", StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(_cartella, "esempiodt.prg")));
        finestra.Close();
    }
}

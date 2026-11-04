//-----------------------------------------------------------------------
// <copyright file="Tests/MessaggiViewModelTests.cs" company="Casasoft">
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

using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;

namespace Casasoft.IFBuilder64.Tests;

public sealed class MessaggiViewModelTests : IDisposable
{
    private readonly DialoghiFinti _dialoghi = new();
    private readonly string _cartella = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_cartella, true);

    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    private static string[] Testi(DocumentoViewModel d) => d.MessaggiUtente.Select(m => m.Testo).ToArray();

    [Fact]
    public void Ripristina_UsaTestoTemplate()
    {
        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);
        var m = d.MessaggiStandard.First(x => x.TestoTemplate is not null);
        var originale = m.TestoTemplate;
        m.Testo = "modificato";
        Assert.True(v.RipristinaCommand.CanExecute(m));

        v.RipristinaCommand.Execute(m);

        Assert.Equal(originale, m.Testo);
        Assert.False(v.RipristinaCommand.CanExecute(m));
    }

    [Fact]
    public void Ripristina_SenzaTemplate_Disabilitato()
    {
        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);
        var m = d.MessaggiStandard[0];
        m.TestoTemplate = null;
        m.Testo = "x";

        Assert.False(v.RipristinaCommand.CanExecute(m));
        v.RipristinaCommand.Execute(m);
        Assert.Equal("x", m.Testo);
    }

    [Fact]
    public void Aggiungi_InCodaConIndice()
    {
        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);
        var n = d.MessaggiUtente.Count;

        v.AggiungiCommand.Execute(null);

        var nuovo = d.MessaggiUtente[^1];
        Assert.Equal(n + 1, d.MessaggiUtente.Count);
        Assert.Equal(n, nuovo.Indice);
        Assert.Equal("", nuovo.Testo);
        Assert.Same(nuovo, v.SelezionatoUtente);
        Assert.DoesNotContain("ConfermaAsync", _dialoghi.Chiamate);
    }

    [Fact]
    public async Task Elimina_ChiedeConferma_RinumeraIndici()
    {
        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);
        var prima = Testi(d);
        Assert.Equal(3, prima.Length);

        await v.EliminaCommand.ExecuteAsync(d.MessaggiUtente[1]);

        Assert.Contains("ConfermaAsync", _dialoghi.Chiamate);
        Assert.Contains("{var:Message}=1", _dialoghi.UltimoMessaggioConferma);
        Assert.Equal(new[] { prima[0], prima[2] }, Testi(d));
        Assert.Equal(new[] { 0, 1 }, d.MessaggiUtente.Select(m => m.Indice));
    }

    [Fact]
    public async Task Sposta_Annullato_NonCambiaOrdine()
    {
        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);
        var prima = Testi(d);
        _dialoghi.RispostaConferma = false;

        await v.SuCommand.ExecuteAsync(d.MessaggiUtente[1]);
        await v.GiuCommand.ExecuteAsync(d.MessaggiUtente[1]);
        await v.EliminaCommand.ExecuteAsync(d.MessaggiUtente[1]);

        Assert.Equal(prima, Testi(d));
        Assert.Equal(new[] { 0, 1, 2 }, d.MessaggiUtente.Select(m => m.Indice));

        _dialoghi.RispostaConferma = true;
        await v.GiuCommand.ExecuteAsync(d.MessaggiUtente[0]);
        Assert.Equal(new[] { prima[1], prima[0], prima[2] }, Testi(d));
        Assert.Equal(new[] { 0, 1, 2 }, d.MessaggiUtente.Select(m => m.Indice));
        await v.SuCommand.ExecuteAsync(d.MessaggiUtente[2]);
        Assert.Equal(new[] { prima[1], prima[2], prima[0] }, Testi(d));
        Assert.Equal(new[] { 0, 1, 2 }, d.MessaggiUtente.Select(m => m.Indice));
    }

    [Fact]
    public void Sposta_AgliEstremi_Disabilitato()
    {
        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);

        Assert.False(v.SuCommand.CanExecute(d.MessaggiUtente[0]));
        Assert.True(v.GiuCommand.CanExecute(d.MessaggiUtente[0]));
        Assert.False(v.GiuCommand.CanExecute(d.MessaggiUtente[^1]));
        Assert.True(v.SuCommand.CanExecute(d.MessaggiUtente[^1]));
    }

    [Fact]
    public void MessaggiStandard_NonSiAggiungonoNeEliminano()
    {
        var comandi = typeof(MessaggiViewModel).GetProperties()
            .Where(p => typeof(System.Windows.Input.ICommand).IsAssignableFrom(p.PropertyType))
            .Select(p => p.Name).Order().ToArray();

        Assert.Equal(new[] { "AggiungiCommand", "EliminaCommand", "GiuCommand", "RipristinaCommand", "SuCommand" }, comandi);

        var d = Esempio();
        var v = new MessaggiViewModel(d, _dialoghi);
        var standard = d.MessaggiStandard.ToList();
        v.AggiungiCommand.Execute(null);
        Assert.Equal(standard, d.MessaggiStandard);
    }

    [Fact]
    public void VaiAdAvvisoMessaggioPersonalizzato_CambiaSchedaESeleziona()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"), new PianificatoreFinto().Pianifica);
        vm.Documento = Esempio();
        var m = vm.Documento.MessaggiUtente[1];

        vm.VaiAdAvvisoCommand.Execute(new Avviso(Gravita.Avviso, "x", m));

        Assert.Equal(MainWindowViewModel.SchedaMessaggi, vm.SchedaAttiva);
        Assert.Same(m, vm.Messaggi.SelezionatoUtente);
        Assert.Null(vm.Messaggi.SelezionatoStandard);

        var s = vm.Documento.MessaggiStandard[2];
        vm.VaiAdAvvisoCommand.Execute(new Avviso(Gravita.Avviso, "x", s));
        Assert.Same(s, vm.Messaggi.SelezionatoStandard);
        Assert.Null(vm.Messaggi.SelezionatoUtente);
    }

    [AvaloniaFact]
    public void SchedaMessaggi_MostraStandardEPersonalizzati()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"));
        vm.Documento = Esempio();
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        vm.SchedaAttiva = MainWindowViewModel.SchedaMessaggi;
        Dispatcher.UIThread.RunJobs();

        var vista = finestra.GetVisualDescendants().OfType<MessaggiView>().Single(x => x.IsEffectivelyVisible);
        var elenchi = vista.GetVisualDescendants().OfType<Avalonia.Controls.ListBox>().ToList();
        var standard = elenchi.Single(l => l.Name == "ElencoStandard");
        var utente = elenchi.Single(l => l.Name == "ElencoUtente");
        Assert.Equal(55, standard.ItemCount);
        Assert.Equal(3, utente.ItemCount);
    }
}

//-----------------------------------------------------------------------
// <copyright file="Tests/OggettiViewModelTests.cs" company="Casasoft">
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
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;

namespace Casasoft.IFBuilder64.Tests;

public sealed class VerbiViewModelTests : IDisposable
{
    private readonly DialoghiFinti _dialoghi = new();
    private readonly string _cartella = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_cartella, true);

    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    [Fact]
    public void AggiungiSinonimo_StessaAzioneDopoIlSelezionato()
    {
        var d = Esempio();
        var v = new VerbiViewModel(d);
        var originale = d.Verbi[3];
        v.Selezionato = originale;

        v.AggiungiSinonimoCommand.Execute(null);

        var sinonimo = d.Verbi[4];
        Assert.Equal(originale.Azione, sinonimo.Azione);
        Assert.NotEqual(originale.Comando, sinonimo.Comando);
        Assert.StartsWith(originale.Comando, sinonimo.Comando);
        Assert.Equal(d.Verbi.Count, d.Verbi.Select(x => x.Comando).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Same(sinonimo, v.Selezionato);
    }

    [Fact]
    public void FiltroAzione()
    {
        var d = Esempio();
        var v = new VerbiViewModel(d);
        Assert.Equal(d.Verbi.Count, v.VerbiFiltrati.Count());

        v.FiltroAzione = CatalogoAzioni.Trova(8);

        Assert.NotEmpty(v.VerbiFiltrati);
        Assert.All(v.VerbiFiltrati, x => Assert.Equal(8, x.Azione));
        Assert.Equal(d.Verbi.Count(x => x.Azione == 8), v.VerbiFiltrati.Count());

        v.FiltroIndice = 0;
        Assert.Null(v.FiltroAzione);
        Assert.Equal(d.Verbi.Count, v.VerbiFiltrati.Count());
    }

    [Fact]
    public void VerboFuoriCatalogo_NonVieneAlterato()
    {
        var d = Esempio();
        var v = new VerbiViewModel(d);
        var fuori = new VerboViewModel { Comando = "xyzzy", Azione = 99 };
        d.Verbi.Add(fuori);

        var voci = v.AzioniPer(fuori);
        Assert.Equal(CatalogoAzioni.NumeroAzioni + 1, voci.Count);
        Assert.Equal("? (99)", v.VoceCorrente(fuori).ToString());
        // Le altre righe non hanno la voce extra
        Assert.Equal(CatalogoAzioni.NumeroAzioni, v.AzioniPer(d.Verbi[0]).Count);

        // Il null del ComboBox e la voce "? (n)" non scrivono sul modello
        v.ImpostaAzione(fuori, null);
        v.ImpostaAzione(fuori, v.VoceCorrente(fuori));
        Assert.Equal(99, fuori.Azione);

        v.ImpostaAzione(fuori, CatalogoAzioni.Trova(7));
        Assert.Equal(7, fuori.Azione);
        Assert.Equal(CatalogoAzioni.NumeroAzioni, v.AzioniPer(fuori).Count);
    }

    [Fact]
    public void Nuovo_UsaLAzioneDelFiltroENomeUnivoco()
    {
        var d = Esempio();
        var v = new VerbiViewModel(d);
        v.FiltroAzione = CatalogoAzioni.Trova(12);

        v.NuovoCommand.Execute(null);

        var nuovo = d.Verbi[^1];
        Assert.Equal(12, nuovo.Azione);
        Assert.Equal("verbo1", nuovo.Comando);
        Assert.Same(nuovo, v.Selezionato);

        v.NuovoCommand.Execute(null);
        Assert.Equal("verbo2", d.Verbi[^1].Comando);

        v.FiltroAzione = null;
        v.NuovoCommand.Execute(null);
        Assert.Equal(0, d.Verbi[^1].Azione);
    }

    [Fact]
    public void Elimina_RimuoveIlSelezionato_ComandiDisabilitatiSenzaSelezione()
    {
        var d = Esempio();
        var v = new VerbiViewModel(d);
        Assert.False(v.EliminaCommand.CanExecute(null));
        Assert.False(v.AggiungiSinonimoCommand.CanExecute(null));
        var primo = d.Verbi[0];
        v.Selezionato = primo;
        Assert.True(v.EliminaCommand.CanExecute(null));

        v.EliminaCommand.Execute(null);

        Assert.DoesNotContain(primo, d.Verbi);
        Assert.DoesNotContain(primo, v.VerbiFiltrati);
        Assert.Null(v.Selezionato);
    }

    [Fact]
    public void VaiAdAvvisoVerbo_CambiaSchedaESelezionaAnnullandoIlFiltro()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"));
        vm.Documento = Esempio();
        var d = vm.Documento;
        var verbo = d.Verbi.First(x => x.Azione != 8);
        vm.Verbi.FiltroAzione = CatalogoAzioni.Trova(8);
        Assert.DoesNotContain(verbo, vm.Verbi.VerbiFiltrati);

        vm.VaiAdAvvisoCommand.Execute(new Avviso(Gravita.Avviso, "x", verbo));

        Assert.Equal(MainWindowViewModel.SchedaVerbi, vm.SchedaAttiva);
        Assert.Null(vm.Verbi.FiltroAzione);
        Assert.Same(verbo, vm.Verbi.Selezionato);
        Assert.Null(vm.Pannello);

        vm.Verbi.Selezionato = d.Verbi[0];
        Assert.Same(d.Verbi[0], vm.ElementoSelezionato);
    }

    [AvaloniaFact]
    public void SchedaVerbi_MostraTuttiIVerbiDelDocumento()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"));
        vm.Documento = Esempio();
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        vm.SchedaAttiva = MainWindowViewModel.SchedaVerbi;
        Dispatcher.UIThread.RunJobs();

        var griglia = finestra.GetVisualDescendants().OfType<DataGrid>().Single(g => g.IsEffectivelyVisible);
        var righe = griglia.ItemsSource!.Cast<VerboViewModel>().ToList();
        Assert.Equal(vm.Documento.Verbi.Count, righe.Count);
        Assert.True(righe.Count > 0);
    }
}

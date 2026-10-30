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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Casasoft.IFBuilder64.Editor.Controlli;
using Casasoft.IFBuilder64.Editor.Risorse;
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

    [AvaloniaFact]
    public async Task Mappa_EsempioAperto_SiDisegnaSenzaEccezioni()
    {
        var (finestra, vm) = ApriFinestra();
        _dialoghi.RispostaApriFile = CopiaEsempio();
        await vm.ApriCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        var mappa = Assert.Single(finestra.GetVisualDescendants().OfType<MappaControl>());
        Assert.Same(vm.Mappa, mappa.Mappa);
        Assert.True(mappa.Bounds.Width > 0 && mappa.Bounds.Height > 0);
        vm.Mappa.Selezione = vm.Documento.TrovaStanza("cortile");
        foreach (var livello in vm.Mappa.Livelli)
        {
            vm.Mappa.LivelloCorrente = livello;
            Dispatcher.UIThread.RunJobs();
            using var immagine = new RenderTargetBitmap(new PixelSize((int)mappa.Bounds.Width, (int)mappa.Bounds.Height));
            immagine.Render(mappa);
        }
        finestra.Close();
    }

    [AvaloniaFact]
    public void Mappa_ClicSuCellaVuota_CreaStanza()
    {
        var (finestra, vm) = ApriFinestra();
        var mappa = Assert.Single(finestra.GetVisualDescendants().OfType<MappaControl>());
        var stanze = vm.Documento.Stanze.Count;

        // Centro della cella (0,0) nelle coordinate della finestra
        var centro = mappa.Geometria.RettangoloCella(new Cella(0, 0)).Center;
        var punto = mappa.TranslatePoint(centro, finestra)!.Value;
        finestra.MouseDown(punto, MouseButton.Left);
        finestra.MouseUp(punto, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(stanze + 1, vm.Documento.Stanze.Count);
        var nuova = vm.Documento.Stanze[^1];
        Assert.Equal((0, 0), (nuova.X, nuova.Y));
        Assert.Same(nuova, vm.ElementoSelezionato);
        finestra.Close();
    }

    /// <summary>Finestra con l'esempio aperto e la mappa al livello 0.</summary>
    private async Task<(MainWindow Finestra, MainWindowViewModel Vm, MappaControl Mappa)> ApriMappaEsempio()
    {
        var (finestra, vm) = ApriFinestra();
        _dialoghi.RispostaApriFile = CopiaEsempio();
        await vm.ApriCommand.ExecuteAsync(null);
        finestra.Height = 1000;
        Dispatcher.UIThread.RunJobs();
        var mappa = Assert.Single(finestra.GetVisualDescendants().OfType<MappaControl>());
        // Tutte le stanze del livello 0 devono stare nell'area visibile della mappa
        var stanze = vm.Mappa.StanzeDelLivello.Select(s => mappa.Geometria.RettangoloCella(new Cella(s.X, s.Y))).ToList();
        Assert.All(stanze, r => Assert.True(new Rect(mappa.Bounds.Size).Contains(r)));
        return (finestra, vm, mappa);
    }

    private static Point PuntoFinestra(MappaControl mappa, MainWindow finestra, Point p) =>
        mappa.TranslatePoint(p, finestra)!.Value;

    private static Point CentroCella(MappaControl mappa, StanzaViewModel s) =>
        mappa.Geometria.RettangoloCella(new Cella(s.X, s.Y)).Center;

    /// <summary>Punto medio del collegamento tra due stanze adiacenti, sul confine tra le due celle.</summary>
    private static Point MetaCollegamento(MappaControl mappa, StanzaViewModel da, Direzione dir, StanzaViewModel a) =>
        GeometriaMappa.PuntoMedio(mappa.Geometria.Tracciato(new Cella(da.X, da.Y), dir, new Cella(a.X, a.Y)));

    private static List<string> Voci(MappaControl mappa) =>
        Assert.IsAssignableFrom<IEnumerable<Control>>(mappa.UltimoMenu?.ItemsSource)
            .OfType<MenuItem>().Select(m => (string)m.Header!).ToList();

    [AvaloniaFact]
    public async Task Mappa_ClicSullaPortaTraStanzeAdiacenti_SelezionaLaPorta()
    {
        var (finestra, vm, mappa) = await ApriMappaEsempio();
        var cortile = vm.Documento.TrovaStanza("cortile")!;
        var torre = vm.Documento.TrovaStanza("torre")!;
        var portone = vm.Documento.Porte.Single(p => p.Nome == "portone");
        var punto = PuntoFinestra(mappa, finestra, MetaCollegamento(mappa, cortile, Direzione.Nord, torre));

        finestra.MouseDown(punto, MouseButton.Left);
        finestra.MouseUp(punto, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(portone, vm.Mappa.Selezione);
        Assert.Same(portone, vm.ElementoSelezionato);
        Assert.Null(cortile.Nord);
        Assert.Null(torre.Sud);
        finestra.Close();
    }

    [AvaloniaFact]
    public async Task Mappa_TastoDestroSulCollegamentoTraStanzeAdiacenti_MenuDelCollegamento()
    {
        var (finestra, vm, mappa) = await ApriMappaEsempio();
        var ponte = vm.Documento.TrovaStanza("ponte")!;
        var cortile = vm.Documento.TrovaStanza("cortile")!;
        var punto = PuntoFinestra(mappa, finestra, MetaCollegamento(mappa, ponte, Direzione.Nord, cortile));

        finestra.MouseDown(punto, MouseButton.Right);
        finestra.MouseUp(punto, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([Testi.Mappa_TrasformaInPorta, Testi.Mappa_Elimina], Voci(mappa));
        Assert.Null(vm.Mappa.Selezione);
        finestra.Close();
    }

    [AvaloniaFact]
    public async Task Mappa_TastoDestroSullaStanza_MenuDellaStanza()
    {
        var (finestra, vm, mappa) = await ApriMappaEsempio();
        var ponte = vm.Documento.TrovaStanza("ponte")!;
        var centro = PuntoFinestra(mappa, finestra, CentroCella(mappa, ponte));
        finestra.MouseDown(centro, MouseButton.Right);
        finestra.MouseUp(centro, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(Testi.Mappa_ImpostaIniziale, Voci(mappa));
        Assert.Same(ponte, vm.Mappa.Selezione);
        finestra.Close();
    }

    [AvaloniaFact]
    public async Task Mappa_TrascinamentoDalBordoDellaStanza_Collega()
    {
        var (finestra, vm, mappa) = await ApriMappaEsempio();
        var giardino = vm.Documento.TrovaStanza("giardino")!;
        var nuova = vm.Mappa.CreaStanza(giardino.X + 2, giardino.Y);
        Dispatcher.UIThread.RunJobs();
        var r = mappa.Geometria.RettangoloStanza(new Cella(giardino.X, giardino.Y));
        var inizio = PuntoFinestra(mappa, finestra, new Point(r.Right - 3, r.Center.Y));
        var fine = PuntoFinestra(mappa, finestra, CentroCella(mappa, nuova));

        finestra.MouseDown(inizio, MouseButton.Left);
        finestra.MouseMove(fine);
        finestra.MouseUp(fine, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(nuova.Nome, giardino.Est);
        Assert.Equal("giardino", nuova.Ovest);
        finestra.Close();
    }
}

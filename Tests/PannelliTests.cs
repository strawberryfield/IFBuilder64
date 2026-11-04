//-----------------------------------------------------------------------
// <copyright file="Tests/PannelliTests.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;

namespace Casasoft.IFBuilder64.Tests;

public class PannelliTests
{
    private readonly DialoghiFinti _dialoghi = new();

    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    private PannelloStanzaViewModel PannelloStanza(DocumentoViewModel d, string nome, Action<object?>? seleziona = null) =>
        new(d, d.TrovaStanza(nome)!, new MappaViewModel(d, _dialoghi), _dialoghi, seleziona ?? (_ => { }));

    [Fact]
    public void Stanza_StaticiEMobili_SeparatiPerFlag()
    {
        var d = Esempio();
        var p = PannelloStanza(d, "torre");
        Assert.Equal(["baule"], p.Statici.Select(o => o.Nome));
        Assert.Empty(p.Mobili);

        var c = PannelloStanza(d, "cantina");
        Assert.Empty(c.Statici);
        Assert.Equal(["borsa"], c.Mobili.Select(o => o.Nome));
    }

    [Fact]
    public void NuovoOggettoQui_CreaMobileInCodaConNomeUnivoco_EAggiornaLista()
    {
        var d = Esempio();
        var p = PannelloStanza(d, "cantina");
        var prima = d.Oggetti.Count;

        p.NuovoOggettoQuiCommand.Execute(null);
        p.NuovoOggettoQuiCommand.Execute(null);

        Assert.Equal(prima + 2, d.Oggetti.Count);
        var nuovo = d.Oggetti[^2];
        Assert.Equal(Testi.PrefissoOggetto + "1", nuovo.Nome);
        Assert.Equal(Testi.PrefissoOggetto + "2", d.Oggetti[^1].Nome);
        Assert.Equal("cantina", nuovo.Posizione);
        Assert.Equal(string.Empty, nuovo.Descrizione);
        Assert.NotEqual(true, nuovo.Statico);
        Assert.Contains(nuovo, p.Mobili);
    }

    [Fact]
    public async Task SpostaQui_OffreSoloOggettiDiAltreStanze_ESposta()
    {
        var d = Esempio();
        var p = PannelloStanza(d, "cantina");
        var chiave = d.Oggetti.First(o => o.Nome == "chiave");
        _dialoghi.RispostaScegliOggetto = chiave;

        await p.SpostaQuiCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_dialoghi.OggettiOfferti, o => o.Nome == "borsa");
        Assert.Contains(chiave, _dialoghi.OggettiOfferti);
        Assert.Equal("cantina", chiave.Posizione);
        Assert.Contains(chiave, p.Mobili);
    }

    [Fact]
    public async Task SpostaQui_Annullato_NonCambiaNulla()
    {
        var d = Esempio();
        var p = PannelloStanza(d, "cantina");
        _dialoghi.RispostaScegliOggetto = null;

        await p.SpostaQuiCommand.ExecuteAsync(null);

        Assert.Equal(["borsa"], p.Mobili.Select(o => o.Nome));
    }

    [Fact]
    public void ClicSuUscita_SelezionaStanzaDiArrivo()
    {
        var d = Esempio();
        object? selezionato = null;
        var p = PannelloStanza(d, "cortile", e => selezionato = e);

        Assert.Equal(6, p.Uscite.Count);
        var sud = p.Uscite.Single(u => u.Destinazione == "ponte");
        sud.Seleziona.Execute(null);

        Assert.Same(d.TrovaStanza("ponte"), selezionato);
        Assert.False(p.Uscite.First(u => u.Destinazione is null).Seleziona.CanExecute(null));
    }

    [Fact]
    public void ImpostaIniziale_DisabilitatoSeGiaIniziale()
    {
        var d = Esempio();
        var iniziale = PannelloStanza(d, "ponte");
        var altra = PannelloStanza(d, "cortile");

        Assert.False(iniziale.ImpostaInizialeCommand.CanExecute(null));
        Assert.True(altra.ImpostaInizialeCommand.CanExecute(null));
        altra.ImpostaInizialeCommand.Execute(null);

        Assert.Equal("cortile", d.StanzaIniziale);
        Assert.False(altra.ImpostaInizialeCommand.CanExecute(null));
    }

    [Fact]
    public void Rinomina_AggiornaListeOggetti()
    {
        var d = Esempio();
        var p = PannelloStanza(d, "torre");
        p.Nome.Testo = "campanile";
        p.Nome.Conferma();

        Assert.Equal("campanile", p.Stanza.Nome);
        Assert.Equal(["baule"], p.Statici.Select(o => o.Nome));
        Assert.Equal("campanile", d.Oggetti.First(o => o.Nome == "baule").Posizione);
    }

    [Fact]
    public void Porta_AperturaScriveERimuoveUsciteNelleStanze()
    {
        var d = Esempio();
        var porta = d.Porte.First(x => x.Nome == "portone");
        var p = new PannelloPortaViewModel(d, porta);
        Assert.False(p.Aperta);
        Assert.Null(d.TrovaStanza("cortile")!.Nord);

        p.Aperta = true;

        Assert.True(porta.Aperta);
        Assert.Equal("torre", d.TrovaStanza("cortile")!.Nord);
        Assert.Equal("cortile", d.TrovaStanza("torre")!.Sud);

        p.Aperta = false;

        Assert.False(porta.Aperta);
        Assert.Null(d.TrovaStanza("cortile")!.Nord);
        Assert.Null(d.TrovaStanza("torre")!.Sud);
    }

    [Fact]
    public void Porta_OggettoSblocco_NessunoEOrdinamento()
    {
        var d = Esempio();
        var porta = d.Porte.First(x => x.Nome == "portone");
        var p = new PannelloPortaViewModel(d, porta);

        Assert.Equal(PannelloPortaViewModel.Nessuno, p.OggettiSblocco[0]);
        Assert.Equal(p.OggettiSblocco.Skip(1).Order(StringComparer.Ordinal), p.OggettiSblocco.Skip(1));
        Assert.Equal(p.OggettiSblocco.Distinct().Count(), p.OggettiSblocco.Count);
        Assert.Equal("chiave", p.SbloccoSelezionato);

        p.SbloccoSelezionato = PannelloPortaViewModel.Nessuno;
        Assert.Null(porta.OggettoSblocco);
        p.SbloccoSelezionato = "moneta";
        Assert.Equal("moneta", porta.OggettoSblocco);
    }

    [Fact]
    public void Porta_SbloccoSelezionatoNull_VieneIgnorato()
    {
        var d = Esempio();
        var porta = d.Porte.First(x => x.Nome == "portone");
        var p = new PannelloPortaViewModel(d, porta);

        p.SbloccoSelezionato = null!;

        Assert.Equal("chiave", porta.OggettoSblocco);
    }

    [Fact]
    public void Porta_MessaggioVuoto_DiventaNull()
    {
        var d = Esempio();
        var porta = d.Porte.First(x => x.Nome == "portone");
        var p = new PannelloPortaViewModel(d, porta);

        p.MessaggioNegato = string.Empty;

        Assert.Null(porta.MessaggioNegato);
    }

    [Fact]
    public void MainWindowViewModel_PannelloSegueLaSelezione_PlayerNessunPannello()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(Path.GetTempPath(), "x-editor.json"), new PianificatoreFinto().Pianifica);
        var d = Esempio();
        vm.Documento = d;

        vm.ElementoSelezionato = d.TrovaStanza("cortile");
        Assert.IsType<PannelloStanzaViewModel>(vm.Pannello);

        vm.ElementoSelezionato = d.Porte[0];
        Assert.IsType<PannelloPortaViewModel>(vm.Pannello);

        vm.ElementoSelezionato = d.TrovaStanza("player");
        Assert.Null(vm.Pannello);

        vm.ElementoSelezionato = d.TrovaStanza("torre");
        vm.ElementoSelezionato = null;
        Assert.Null(vm.Pannello);
    }

    [AvaloniaFact]
    public void Finestra_CambioElencoOggetti_NonCancellaOggettoSblocco()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(Path.GetTempPath(), "x-editor.json"));
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        vm.Documento = Esempio();
        Dispatcher.UIThread.RunJobs();
        var porta = vm.Documento.Porte.First(p => p.Nome == "portone");
        vm.ElementoSelezionato = porta;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("chiave", porta.OggettoSblocco);

        vm.Documento.Oggetti.Add(new OggettoViewModel { Nome = "zaino" });
        Dispatcher.UIThread.RunJobs();
        vm.Documento.RinominaOggetto(vm.Documento.Oggetti.First(o => o.Nome == "moneta"), "denaro");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("chiave", porta.OggettoSblocco);
        var combo = finestra.GetVisualDescendants().OfType<ComboBox>().Single(c => c.Name == "OggettoSblocco");
        Assert.Equal("chiave", combo.SelectedItem);
        finestra.Close();
    }

    [Fact]
    public void MainWindowViewModel_ElementoEliminato_SvuotaSelezioneEPannello()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(Path.GetTempPath(), "x-editor.json"), new PianificatoreFinto().Pianifica);
        var d = Esempio();
        vm.Documento = d;

        var stanza = d.TrovaStanza("sommita")!;
        vm.ElementoSelezionato = stanza;
        d.Elimina(stanza);
        Assert.Null(vm.ElementoSelezionato);
        Assert.Null(vm.Pannello);

        var porta = d.Porte[0];
        vm.ElementoSelezionato = porta;
        d.Elimina(porta);
        Assert.Null(vm.ElementoSelezionato);
        Assert.Null(vm.Pannello);
    }

    [AvaloniaFact]
    public void Finestra_SelezionandoStanza_MostraPannelloStanzaConIlNome()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(Path.GetTempPath(), "x-editor.json"));
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        vm.Documento = Esempio();
        Dispatcher.UIThread.RunJobs();

        vm.ElementoSelezionato = vm.Documento.TrovaStanza("cortile");
        Dispatcher.UIThread.RunJobs();

        var pannello = finestra.GetVisualDescendants().OfType<PannelloStanza>().Single();
        var campo = pannello.GetVisualDescendants().OfType<TextBox>().First();
        Assert.Equal("cortile", campo.Text);

        vm.ElementoSelezionato = vm.Documento.Porte[0];
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(finestra.GetVisualDescendants().OfType<PannelloStanza>());
        Assert.Single(finestra.GetVisualDescendants().OfType<PannelloPorta>());
        finestra.Close();
    }

    private static PannelloOggettoViewModel PannelloOggetto(DocumentoViewModel d, string nome) =>
        new(d, d.Oggetti.First(o => o.Nome == nome));

    [Fact]
    public void PannelloOggetto_Identificatore_VuotoConSuggerimento()
    {
        using var p = PannelloOggetto(Esempio(), "chiave");
        Assert.Equal("", p.Identificatore.Testo);
        Assert.Equal("chiave", p.Identificatore.Suggerimento);
    }

    [Fact]
    public void PannelloOggetto_ConfermaIdentificatore_ImpostaId()
    {
        using var p = PannelloOggetto(Esempio(), "chiave");
        p.Identificatore.Testo = "k";
        p.Identificatore.Conferma();
        Assert.Equal("k", p.Oggetto.IdPersonalizzato);
        Assert.Null(p.Identificatore.Errore);
    }

    [Fact]
    public void PannelloOggetto_IdentificatoreConSpazi_Ripulito()
    {
        using var p = PannelloOggetto(Esempio(), "chiave");
        p.Identificatore.Testo = " k  ";
        p.Identificatore.Conferma();
        Assert.Equal("k", p.Oggetto.IdPersonalizzato);
        Assert.Equal("k", p.Identificatore.Testo);
    }

    [Fact]
    public void PannelloOggetto_IdentificatoreDuplicato_Errore()
    {
        using var p = PannelloOggetto(Esempio(), "moneta");
        p.Identificatore.Testo = "chiave";
        p.Identificatore.Conferma();
        Assert.NotNull(p.Identificatore.Errore);
        Assert.Equal("chiave", p.Identificatore.Testo);
    }

    [Fact]
    public void PannelloOggetto_Suggerimento_SegueLaRinomina()
    {
        using var p = PannelloOggetto(Esempio(), "chiave");
        p.Nome.Testo = "key";
        p.Nome.Conferma();
        Assert.Equal("key", p.Identificatore.Suggerimento);
    }

    [Fact]
    public void PannelloPorta_OggettiSblocco_ElencaGliId()
    {
        var d = Esempio();
        using var p = new PannelloPortaViewModel(d, d.Porte.First(x => x.Nome == "portone"));
        Assert.Contains("baule@torre", p.OggettiSblocco);
        Assert.Contains("pozzo@cortile", p.OggettiSblocco);
        Assert.DoesNotContain("baule", p.OggettiSblocco);
    }
}

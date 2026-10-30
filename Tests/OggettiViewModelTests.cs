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
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;

namespace Casasoft.IFBuilder64.Tests;

public sealed class OggettiViewModelTests : IDisposable
{
    private readonly DialoghiFinti _dialoghi = new();
    private readonly string _cartella = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_cartella, true);

    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    private OggettiViewModel Crea(DocumentoViewModel d) => new(d, _dialoghi);

    private static OggettoViewModel Og(DocumentoViewModel d, string id) => d.TrovaOggettoPerId(id)!;

    [Fact]
    public void PosizioniPerStatico_SoloStanze()
    {
        var d = Esempio();
        var v = Crea(d);

        var voci = v.PosizioniPer(Og(d, "pozzo@cortile"));

        Assert.All(voci, x => Assert.NotNull(d.TrovaStanza(x.Posizione)));
        Assert.All(voci, x => Assert.Null(x.DentroA));
        Assert.DoesNotContain(voci, x => x.Posizione == "player");
        Assert.Equal(d.Stanze.Where(s => !s.EGiocatore).Select(s => s.Nome), voci.Select(x => x.Posizione));
    }

    [Fact]
    public void PosizioniPerMobile_IncludeGiocatoreEContenitori()
    {
        var d = Esempio();
        var v = Crea(d);

        var voci = v.PosizioniPer(Og(d, "chiave"));

        Assert.Equal(new VocePosizione(Testi.Posizione_Nessuna, null, null), voci[0]);
        Assert.Contains(new VocePosizione(Testi.Posizione_Giocatore, "player", null), voci);
        Assert.Contains(voci, x => x.Posizione == "cantina" && x.Etichetta == "cantina");
        Assert.Contains(new VocePosizione(string.Format(Testi.Posizione_Dentro, "borsa"), null, "borsa"), voci);
        Assert.Contains(new VocePosizione(string.Format(Testi.Posizione_Dentro, "baule@torre"), null, "baule@torre"), voci);
        // Né la stanza "player" né i non contenitori
        Assert.DoesNotContain(voci, x => x.Etichetta == "player");
        Assert.DoesNotContain(voci, x => x.DentroA == "moneta" || x.DentroA == "me" || x.DentroA == "pozzo@cortile");
    }

    [Fact]
    public void PosizioniPerMobile_EscludeSeStessoEIContenutiTransitivamente()
    {
        var d = Esempio();
        var v = Crea(d);
        // moneta (non contenitore) diventa contenitore, dentro borsa
        var moneta = Og(d, "moneta");
        var borsa = Og(d, "borsa");
        moneta.EContenitore = true;

        var voci = v.PosizioniPer(borsa);

        Assert.DoesNotContain(voci, x => x.DentroA == "borsa");  // se stesso
        Assert.DoesNotContain(voci, x => x.DentroA == "moneta"); // contenuto in borsa
        Assert.Contains(voci, x => x.DentroA == "baule@torre");

        // Catena borsa > moneta > pergamena: borsa non può andare dentro nessuno dei suoi discendenti
        var pergamena = Og(d, "pergamena");
        pergamena.EContenitore = true;
        d.SpostaOggetto(pergamena, null, "moneta"); // borsa > moneta > pergamena
        var vociBorsa = Crea(d).PosizioniPer(borsa);
        Assert.DoesNotContain(vociBorsa, x => x.DentroA is "moneta" or "pergamena" or "borsa");
        Assert.Contains(vociBorsa, x => x.DentroA == "baule@torre");
    }

    [Fact]
    public void ImpostaPosizioneDentro_ImpostaInitemEAzzeraLocation()
    {
        var d = Esempio();
        var v = Crea(d);
        var chiave = Og(d, "chiave");
        d.SpostaOggetto(chiave, "cantina", null);

        var dentro = v.PosizioniPer(chiave).Single(x => x.DentroA == "borsa");
        v.ImpostaPosizione(chiave, dentro);

        Assert.Null(chiave.Posizione);
        Assert.Equal("borsa", chiave.DentroA);

        var stanza = v.PosizioniPer(chiave).Single(x => x.Posizione == "torre");
        v.ImpostaPosizione(chiave, stanza);
        Assert.Equal("torre", chiave.Posizione);
        Assert.Null(chiave.DentroA);
    }

    [Fact]
    public void ImpostaPosizione_StaticoCambiaId_EIContenutiLoSeguono()
    {
        var d = Esempio();
        var v = Crea(d);
        var baule = Og(d, "baule@torre");

        v.ImpostaPosizione(baule, v.PosizioniPer(baule).Single(x => x.Posizione == "cantina"));

        Assert.Equal("baule@cantina", baule.Id);
        Assert.Equal("baule@cantina", Og(d, "pergamena").DentroA);
    }

    [Fact]
    public void Nuovo_CreaMobileConNomeUnivocoInCodaESelezionato()
    {
        var d = Esempio();
        var v = Crea(d);
        var prima = d.Oggetti.Count;

        v.NuovoCommand.Execute(null);

        var nuovo = d.Oggetti[^1];
        Assert.Equal(prima + 1, d.Oggetti.Count);
        Assert.Equal(Testi.PrefissoOggetto + "1", nuovo.Nome);
        Assert.Equal(string.Empty, nuovo.Descrizione);
        Assert.NotEqual(true, nuovo.Statico);
        Assert.Same(nuovo, v.Selezionato);
        Assert.Contains(nuovo, v.OggettiFiltrati);
    }

    [Fact]
    public void Duplica_NomeUnivoco()
    {
        var d = Esempio();
        var v = Crea(d);
        var chiave = Og(d, "chiave");
        chiave.Descrizione = "una chiave";
        chiave.OggettoSblocco = "x";
        chiave.Apribile = true;
        v.Selezionato = chiave;
        var indice = d.Oggetti.IndexOf(chiave);

        v.DuplicaCommand.Execute(null);

        var copia = d.Oggetti[indice + 1];
        Assert.NotSame(chiave, copia);
        Assert.Equal("chiave1", copia.Nome);
        Assert.Equal("una chiave", copia.Descrizione);
        Assert.Equal("x", copia.OggettoSblocco);
        Assert.True(copia.Apribile);
        Assert.Same(copia, v.Selezionato);

        v.Selezionato = chiave;
        v.DuplicaCommand.Execute(null);
        Assert.Equal("chiave2", d.Oggetti[indice + 1].Nome);
    }

    [Fact]
    public void Duplica_SenzaSelezione_NonFaNulla()
    {
        var d = Esempio();
        var v = Crea(d);
        var n = d.Oggetti.Count;
        Assert.False(v.DuplicaCommand.CanExecute(null));
        v.DuplicaCommand.Execute(null);
        Assert.Equal(n, d.Oggetti.Count);
    }

    [Fact]
    public async Task Elimina_ConRiferimenti_ChiedeConfermaElencandoli()
    {
        var d = Esempio();
        var v = Crea(d);
        var chiave = Og(d, "chiave");
        v.Selezionato = chiave;
        _dialoghi.RispostaConferma = false;

        await v.EliminaCommand.ExecuteAsync(null);

        Assert.Contains(chiave, d.Oggetti);
        Assert.Contains("portone", _dialoghi.UltimoMessaggioConferma);
        Assert.Equal("portone", d.Porte.Single(p => p.Nome == "portone").Nome);
        Assert.Equal("chiave", d.Porte.Single(p => p.Nome == "portone").OggettoSblocco);

        _dialoghi.RispostaConferma = true;
        await v.EliminaCommand.ExecuteAsync(null);

        Assert.DoesNotContain(chiave, d.Oggetti);
        Assert.Null(d.Porte.Single(p => p.Nome == "portone").OggettoSblocco);
        Assert.Null(v.Selezionato);
    }

    [Fact]
    public async Task Elimina_SenzaRiferimenti_NonChiedeConferma()
    {
        var d = Esempio();
        var v = Crea(d);
        var pozzo = Og(d, "pozzo@cortile");
        v.Selezionato = pozzo;
        _dialoghi.Chiamate.Clear();

        await v.EliminaCommand.ExecuteAsync(null);

        Assert.DoesNotContain(pozzo, d.Oggetti);
        Assert.DoesNotContain(nameof(IDialoghi.ConfermaAsync), _dialoghi.Chiamate);
    }

    [Fact]
    public void FiltroStanzaETipo()
    {
        var d = Esempio();
        var v = Crea(d);
        Assert.Equal(d.Oggetti.Count, v.OggettiFiltrati.Count());

        v.FiltroStanza = "cantina";
        Assert.Equal(["borsa"], v.OggettiFiltrati.Select(o => o.Nome));

        v.FiltroStanza = null;
        v.FiltroStatico = true;
        Assert.Equal(["pozzo", "baule"], v.OggettiFiltrati.Select(o => o.Nome));

        v.FiltroStatico = false;
        Assert.DoesNotContain(v.OggettiFiltrati, o => o.Statico == true);
        Assert.Equal(d.Oggetti.Count(o => o.Statico != true), v.OggettiFiltrati.Count());

        v.FiltroStanza = "torre";
        v.FiltroStatico = null;
        Assert.Equal(["baule"], v.OggettiFiltrati.Select(o => o.Nome));
    }

    [Fact]
    public void OggettiFiltrati_SegueLeModificheDelDocumento()
    {
        var d = Esempio();
        var v = Crea(d);
        v.FiltroStanza = "cantina";

        d.SpostaOggetto(Og(d, "chiave"), "cantina", null);

        Assert.Contains("chiave", v.OggettiFiltrati.Select(o => o.Nome));
    }

    [Fact]
    public void ImpostaStaticoSuStatico_AzzeraDentroAEPosizioneGiocatore()
    {
        var d = Esempio();
        var v = Crea(d);
        var moneta = Og(d, "moneta"); // dentro borsa
        v.ImpostaTipo(moneta, true);
        Assert.True(moneta.Statico);
        Assert.Null(moneta.DentroA);
        Assert.Null(moneta.Posizione);

        var me = Og(d, "me"); // in player
        v.ImpostaTipo(me, true);
        Assert.Null(me.Posizione);

        var borsa = Og(d, "borsa"); // in cantina
        v.ImpostaTipo(borsa, true);
        Assert.Equal("cantina", borsa.Posizione);
        Assert.Equal("borsa@cantina", borsa.Id);
        Assert.Equal("borsa@cantina", d.TrovaOggettoPerId("borsa@cantina")!.Id);

        v.ImpostaTipo(borsa, false);
        Assert.NotEqual(true, borsa.Statico);
        Assert.Equal("cantina", borsa.Posizione);
    }

    [Fact]
    public void ImpostaTipoStatico_AggiornaIContenutiDelContenitore()
    {
        var d = Esempio();
        var v = Crea(d);
        var borsa = Og(d, "borsa");

        v.ImpostaTipo(borsa, true);

        Assert.Equal("borsa@cantina", Og(d, "moneta").DentroA);
    }

    [Fact]
    public void Selezione_SincronizzataConElementoSelezionato()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"));
        vm.Documento = Esempio();
        var d = vm.Documento;
        Assert.NotNull(vm.Oggetti);
        Assert.Same(d, vm.Oggetti.Documento);

        vm.Oggetti.Selezionato = Og(d, "chiave");
        Assert.Same(Og(d, "chiave"), vm.ElementoSelezionato);
        var pannello = Assert.IsType<PannelloOggettoViewModel>(vm.Pannello);
        Assert.Same(Og(d, "chiave"), pannello.Oggetto);

        vm.ElementoSelezionato = Og(d, "borsa");
        Assert.Same(Og(d, "borsa"), vm.Oggetti.Selezionato);

        vm.ElementoSelezionato = d.TrovaStanza("cantina");
        Assert.Null(vm.Oggetti.Selezionato);
        Assert.IsType<PannelloStanzaViewModel>(vm.Pannello);

        vm.VaiAdAvvisoCommand.Execute(new Avviso(Gravita.Avviso, "x", Og(d, "pozzo@cortile")));
        Assert.Equal(MainWindowViewModel.SchedaOggetti, vm.SchedaAttiva);
        Assert.Same(Og(d, "pozzo@cortile"), vm.Oggetti.Selezionato);
        Assert.IsType<PannelloOggettoViewModel>(vm.Pannello);
    }

    [Fact]
    public void Eliminare_OggettoSelezionato_AzzeraSelezioneEPannello()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"));
        vm.Documento = Esempio();
        var d = vm.Documento;
        vm.ElementoSelezionato = Og(d, "pozzo@cortile");
        Assert.NotNull(vm.Pannello);

        d.Elimina(Og(d, "pozzo@cortile")); // da fuori della scheda

        Assert.Null(vm.ElementoSelezionato);
        Assert.Null(vm.Pannello);
        Assert.Null(vm.Oggetti.Selezionato);
    }

    [Fact]
    public void PannelloOggetto_SbloccoIgnoraNullDelComboBox_ENessunoCancella()
    {
        var d = Esempio();
        var chiave = Og(d, "chiave");
        var borsa = Og(d, "borsa");
        borsa.OggettoSblocco = "chiave";
        using var p = new PannelloOggettoViewModel(d, borsa);

        Assert.Equal("chiave", p.SbloccoSelezionato);
        Assert.DoesNotContain("borsa", p.OggettiSblocco);

        p.SbloccoSelezionato = null!;
        Assert.Equal("chiave", borsa.OggettoSblocco);

        p.SbloccoSelezionato = PannelloOggettoViewModel.Nessuno;
        Assert.Null(borsa.OggettoSblocco);

        p.SbloccoSelezionato = "chiave";
        Assert.Equal("chiave", borsa.OggettoSblocco);
        Assert.Same(chiave, Og(d, "chiave"));
    }

    [Fact]
    public void PannelloOggetto_DescrizioneApertoVuotoEquivaleANull_NomeRinomina()
    {
        var d = Esempio();
        var borsa = Og(d, "borsa");
        using var p = new PannelloOggettoViewModel(d, borsa);

        p.DescrizioneAperto = "aperta";
        Assert.Equal("aperta", borsa.DescrizioneAperto);
        p.DescrizioneAperto = string.Empty;
        Assert.Null(borsa.DescrizioneAperto);

        p.Nome.Testo = "sacca";
        p.Nome.Conferma();
        Assert.Null(p.Nome.Errore);
        Assert.Equal("sacca", borsa.Nome);
        Assert.Equal("sacca", Og(d, "moneta").DentroA);

        p.Nome.Testo = "chiave";
        p.Nome.Conferma();
        Assert.NotNull(p.Nome.Errore);
        Assert.Equal("sacca", borsa.Nome);
    }

    [AvaloniaFact]
    public void SchedaOggetti_MostraTutteLeRigheDellEsempio()
    {
        var vm = new MainWindowViewModel(_dialoghi, new ImpostazioniUtente(), Path.Combine(_cartella, "e.json"));
        vm.Documento = Esempio();
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        vm.SchedaAttiva = MainWindowViewModel.SchedaOggetti;
        Dispatcher.UIThread.RunJobs();

        var griglia = Assert.Single(finestra.GetVisualDescendants().OfType<DataGrid>());
        var righe = griglia.ItemsSource!.Cast<OggettoViewModel>().ToList();
        Assert.Equal(vm.Documento.Oggetti.Count, righe.Count);
        Assert.Equal(7, righe.Count);

        vm.Oggetti.Selezionato = Og(vm.Documento, "borsa");
        Dispatcher.UIThread.RunJobs();
        Assert.Same(Og(vm.Documento, "borsa"), griglia.SelectedItem);
        Assert.IsType<PannelloOggettoViewModel>(vm.Pannello);

        // Le celle di posizione e tipo sono menu a tendina per riga e scrivono sul documento
        var borsa = Og(vm.Documento, "borsa");
        var combo = griglia.GetVisualDescendants().OfType<ComboBox>()
            .First(c => c.SelectedItem is VocePosizione { Etichetta: "cantina" });
        var voci = (IReadOnlyList<VocePosizione>)combo.ItemsSource!;
        combo.SelectedItem = voci.Single(x => x.Posizione == "torre");
        Assert.Equal("torre", borsa.Posizione);

        var me = Og(vm.Documento, "me");
        var tipo = griglia.GetVisualDescendants().OfType<ComboBox>()
            .First(c => c.DataContext == me && c.SelectedItem is string);
        tipo.SelectedIndex = 0;
        Assert.True(me.Statico);
        Assert.Null(me.Posizione);
        finestra.Close();
    }
}

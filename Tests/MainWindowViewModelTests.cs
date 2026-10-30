//-----------------------------------------------------------------------
// <copyright file="Tests/MainWindowViewModelTests.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Finti dialoghi: registrano le chiamate e restituiscono risposte preimpostate.</summary>
public class DialoghiFinti : IDialoghi
{
    public List<string> Chiamate { get; } = [];
    public List<string> Errori { get; } = [];
    public bool RispostaConferma { get; set; } = true;
    public bool? RispostaConfermaSalvataggio { get; set; }
    public string? RispostaApriFile { get; set; }
    public string? RispostaSalvaFile { get; set; }
    public TemplateLanguage? RispostaTemplate { get; set; }
    public string? RispostaOpzioni { get; set; }
    public StanzaViewModel? RispostaScegliStanza { get; set; }
    public IReadOnlyList<StanzaViewModel> StanzeOfferte { get; private set; } = [];
    public OggettoViewModel? RispostaScegliOggetto { get; set; }
    public IReadOnlyList<OggettoViewModel> OggettiOfferti { get; private set; } = [];

    public Task<bool> ConfermaAsync(string titolo, string messaggio)
    {
        Chiamate.Add(nameof(ConfermaAsync));
        return Task.FromResult(RispostaConferma);
    }

    public Task<bool?> ConfermaSalvataggioAsync()
    {
        Chiamate.Add(nameof(ConfermaSalvataggioAsync));
        return Task.FromResult(RispostaConfermaSalvataggio);
    }

    public Task<string?> ApriFileAsync()
    {
        Chiamate.Add(nameof(ApriFileAsync));
        return Task.FromResult(RispostaApriFile);
    }

    public Task<string?> SalvaFileAsync(string? suggerito)
    {
        Chiamate.Add(nameof(SalvaFileAsync));
        return Task.FromResult(RispostaSalvaFile);
    }

    public Task<TemplateLanguage?> ScegliTemplateAsync()
    {
        Chiamate.Add(nameof(ScegliTemplateAsync));
        return Task.FromResult(RispostaTemplate);
    }

    public Task MostraErroreAsync(string messaggio)
    {
        Chiamate.Add(nameof(MostraErroreAsync));
        Errori.Add(messaggio);
        return Task.CompletedTask;
    }

    public Task<string?> OpzioniCompilazioneAsync(string corrente)
    {
        Chiamate.Add(nameof(OpzioniCompilazioneAsync));
        return Task.FromResult(RispostaOpzioni);
    }

    public Task<StanzaViewModel?> ScegliStanzaAsync(string titolo, IReadOnlyList<StanzaViewModel> stanze)
    {
        Chiamate.Add(nameof(ScegliStanzaAsync));
        StanzeOfferte = stanze;
        return Task.FromResult(RispostaScegliStanza);
    }

    public Task<OggettoViewModel?> ScegliOggettoAsync(string titolo, IReadOnlyList<OggettoViewModel> oggetti)
    {
        Chiamate.Add(nameof(ScegliOggettoAsync));
        OggettiOfferti = oggetti;
        return Task.FromResult(RispostaScegliOggetto);
    }
}

/// <summary>Pianificatore finto: ricorda l'ultima azione pianificata e se è stata annullata.</summary>
public class PianificatoreFinto
{
    public List<(TimeSpan Ritardo, Action Azione, Annullabile Handle)> Pianificate { get; } = [];

    public IDisposable Pianifica(TimeSpan ritardo, Action azione)
    {
        var h = new Annullabile();
        Pianificate.Add((ritardo, azione, h));
        return h;
    }

    public class Annullabile : IDisposable
    {
        public bool Annullata { get; private set; }
        public void Dispose() => Annullata = true;
    }
}

public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly string _cartella = Directory.CreateTempSubdirectory().FullName;
    private readonly DialoghiFinti _dialoghi = new();
    private readonly PianificatoreFinto _pianificatore = new();
    private readonly ImpostazioniUtente _impostazioni = new();

    private string PercorsoImpostazioni => Path.Combine(_cartella, "editor.json");

    public void Dispose() => Directory.Delete(_cartella, true);

    private MainWindowViewModel CreaVm() =>
        new(_dialoghi, _impostazioni, PercorsoImpostazioni, _pianificatore.Pianifica);

    /// <summary>Copia l'esempio nella cartella temporanea e restituisce il percorso.</summary>
    private string CopiaEsempio()
    {
        var p = Path.Combine(_cartella, "esempio.json");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"), p, true);
        return p;
    }

    private async Task<MainWindowViewModel> VmConEsempioAperto()
    {
        var vm = CreaVm();
        _dialoghi.RispostaApriFile = CopiaEsempio();
        await vm.ApriCommand.ExecuteAsync(null);
        Assert.NotNull(vm.Documento.Percorso);
        _dialoghi.Chiamate.Clear();
        return vm;
    }

    [Fact]
    public void Titolo_MostraAsteriscoSeModificato()
    {
        var vm = CreaVm();
        Assert.Equal("IFBuilder64 Editor - " + Testi.SenzaTitolo, vm.Titolo);

        vm.Documento.Intro = "altro";

        Assert.Equal("IFBuilder64 Editor - " + Testi.SenzaTitolo + "*", vm.Titolo);
    }

    [Fact]
    public async Task Titolo_UsaIlNomeDelFile()
    {
        var vm = await VmConEsempioAperto();
        Assert.Equal("IFBuilder64 Editor - esempio.json", vm.Titolo);
    }

    [Fact]
    public async Task Apri_JsonNonValido_MostraErroreEMantieneDocumento()
    {
        var vm = CreaVm();
        var prima = vm.Documento;
        var p = Path.Combine(_cartella, "rotto.json");
        File.WriteAllText(p, "{ \"rooms\": [ ");
        _dialoghi.RispostaApriFile = p;

        await vm.ApriCommand.ExecuteAsync(null);

        Assert.Same(prima, vm.Documento);
        Assert.Single(_dialoghi.Errori);
        Assert.False(string.IsNullOrWhiteSpace(_dialoghi.Errori[0]));
        Assert.Empty(_impostazioni.FileRecenti);
    }

    [Fact]
    public async Task Apri_ConModifiche_ChiedeConferma_AnnullaNonApre()
    {
        var vm = CreaVm();
        vm.Documento.Intro = "modificato";
        var prima = vm.Documento;
        _dialoghi.RispostaConfermaSalvataggio = null;
        _dialoghi.RispostaApriFile = CopiaEsempio();

        await vm.ApriCommand.ExecuteAsync(null);

        Assert.Same(prima, vm.Documento);
        Assert.Equal([nameof(IDialoghi.ConfermaSalvataggioAsync)], _dialoghi.Chiamate);
        Assert.True(vm.Documento.Modificato);
    }

    [Fact]
    public async Task Apri_ConModifiche_NonSalvare_Apre()
    {
        var vm = CreaVm();
        vm.Documento.Intro = "modificato";
        _dialoghi.RispostaConfermaSalvataggio = false;
        _dialoghi.RispostaApriFile = CopiaEsempio();

        await vm.ApriCommand.ExecuteAsync(null);

        Assert.Equal(_dialoghi.RispostaApriFile, vm.Documento.Percorso);
        Assert.False(vm.Documento.Modificato);
        Assert.DoesNotContain(nameof(IDialoghi.SalvaFileAsync), _dialoghi.Chiamate);
    }

    [Fact]
    public async Task Nuovo_TemplateAnnullato_NonCambiaDocumento()
    {
        var vm = CreaVm();
        var prima = vm.Documento;
        _dialoghi.RispostaTemplate = null;

        await vm.NuovoCommand.ExecuteAsync(null);

        Assert.Same(prima, vm.Documento);
        Assert.Contains(nameof(IDialoghi.ScegliTemplateAsync), _dialoghi.Chiamate);
    }

    [Fact]
    public async Task Nuovo_TemplateScelto_CreaDocumentoNonModificato()
    {
        var vm = await VmConEsempioAperto();
        _dialoghi.RispostaTemplate = TemplateLanguage.English;

        await vm.NuovoCommand.ExecuteAsync(null);

        Assert.Null(vm.Documento.Percorso);
        Assert.False(vm.Documento.Modificato);
        Assert.Equal("north", vm.Documento.Direzioni[0].Testo);
    }

    [Fact]
    public async Task Compila_DocumentoNuovo_ChiedeSalvaConNome_AnnullaNonCompila()
    {
        var vm = CreaVm();
        _dialoghi.RispostaSalvaFile = null;

        await vm.CompilaCommand.ExecuteAsync(null);

        Assert.Contains(nameof(IDialoghi.SalvaFileAsync), _dialoghi.Chiamate);
        Assert.Null(vm.Documento.Percorso);
        Assert.Empty(vm.LogCompilazione);
        Assert.Empty(Directory.GetFiles(_cartella, "*.prg"));
    }

    [Fact]
    public async Task Compila_ConErrori_NonCompilaEMostraAvvisi()
    {
        var vm = await VmConEsempioAperto();
        vm.Documento.StanzaIniziale = "inesistente";

        await vm.CompilaCommand.ExecuteAsync(null);

        Assert.Contains(vm.Avvisi, a => a.Gravita == Gravita.Errore);
        Assert.True(vm.PannelloInferioreAperto);
        Assert.Equal(0, vm.SchedaInferiore);
        Assert.Empty(vm.LogCompilazione);
        Assert.Empty(Directory.GetFiles(_cartella, "*.prg"));
        Assert.False(vm.CompilazioneInCorso);
    }

    [Fact]
    public async Task Compila_Ok_LogContieneIFileGenerati()
    {
        var vm = await VmConEsempioAperto();
        vm.Documento.Intro += " ";

        await vm.CompilaCommand.ExecuteAsync(null);

        var atteso = ServizioCompilazione.OutputPredefinito(vm.Documento.Percorso!) + "dt.prg";
        Assert.Contains(atteso, vm.LogCompilazione);
        Assert.True(File.Exists(atteso));
        Assert.False(vm.Documento.Modificato);
        Assert.False(vm.CompilazioneInCorso);
        Assert.Equal(1, vm.SchedaInferiore);
    }

    [Fact]
    public async Task Compila_ConOutputBaseImpostato_UsaQuelNome()
    {
        var vm = await VmConEsempioAperto();
        var cartellaOutput = Directory.CreateDirectory(Path.Combine(_cartella, "out")).FullName;
        _dialoghi.RispostaOpzioni = Path.Combine(cartellaOutput, "gioco");

        await vm.OpzioniCompilazioneCommand.ExecuteAsync(null);
        await vm.CompilaCommand.ExecuteAsync(null);

        Assert.Equal(_dialoghi.RispostaOpzioni, vm.OutputBase);
        Assert.Contains(Path.Combine(cartellaOutput, "giocodt.prg"), vm.LogCompilazione);
        Assert.Equal(cartellaOutput, ImpostazioniUtente.Carica(PercorsoImpostazioni).UltimaCartellaOutput);
    }

    [Fact]
    public async Task Salva_ErroreIO_MostraErroreERestaModificato()
    {
        var vm = await VmConEsempioAperto();
        vm.Documento.Percorso = Path.Combine(_cartella, "manca", "x.json");
        vm.Documento.Intro = "modificato";

        await vm.SalvaCommand.ExecuteAsync(null);

        Assert.Single(_dialoghi.Errori);
        Assert.True(vm.Documento.Modificato);
    }

    [Fact]
    public async Task VaiAdAvviso_SelezionaElemento()
    {
        var vm = await VmConEsempioAperto();
        var doc = vm.Documento;
        var casi = new (object Elemento, int Scheda)[]
        {
            (doc.Stanze[1], 0), (doc.Porte[0], 0), (doc.Oggetti[0], 1),
            (doc.Verbi[0], 2), (doc.MessaggiUtente[0], 3), (doc, 4)
        };
        foreach (var (elemento, scheda) in casi)
        {
            vm.SchedaAttiva = -1;
            vm.VaiAdAvvisoCommand.Execute(new Avviso(Gravita.Avviso, "m", elemento));
            Assert.Same(elemento, vm.ElementoSelezionato);
            Assert.Equal(scheda, vm.SchedaAttiva);
        }
    }

    [Fact]
    public async Task Apri_AggiungeAiRecenti()
    {
        var vm = await VmConEsempioAperto();
        var p = vm.Documento.Percorso!;

        Assert.Equal(p, _impostazioni.FileRecenti[0]);
        Assert.Equal(p, vm.Recenti[0]);
        Assert.Equal(p, ImpostazioniUtente.Carica(PercorsoImpostazioni).FileRecenti[0]);
    }

    [Fact]
    public async Task Modifica_PianificaValidazioneDopo500ms()
    {
        var vm = await VmConEsempioAperto();
        _pianificatore.Pianificate.Clear();

        vm.Documento.StanzaIniziale = "inesistente";
        vm.Documento.Intro = "x";

        Assert.Equal(2, _pianificatore.Pianificate.Count);
        Assert.True(_pianificatore.Pianificate[0].Handle.Annullata);
        Assert.Equal(TimeSpan.FromMilliseconds(500), _pianificatore.Pianificate[1].Ritardo);
        Assert.DoesNotContain(vm.Avvisi, a => a.Gravita == Gravita.Errore);

        _pianificatore.Pianificate[1].Azione();

        Assert.Contains(vm.Avvisi, a => a.Gravita == Gravita.Errore);
    }

    [Fact]
    public async Task Correggi_EseguiCorrezioneERivalida()
    {
        var vm = await VmConEsempioAperto();
        var eseguita = false;
        var avviso = new Avviso(Gravita.Avviso, "m", vm.Documento, () =>
        {
            eseguita = true;
            vm.Documento.StanzaIniziale = "inesistente";
        });

        vm.CorreggiCommand.Execute(avviso);

        Assert.True(eseguita);
        Assert.Contains(vm.Avvisi, a => a.Gravita == Gravita.Errore);
    }

    [Fact]
    public async Task PuoChiudere_ConModifiche_AnnullaRestituisceFalse()
    {
        var vm = await VmConEsempioAperto();
        Assert.True(await vm.PuoChiudereAsync());

        vm.Documento.Intro = "x";
        _dialoghi.RispostaConfermaSalvataggio = null;
        Assert.False(await vm.PuoChiudereAsync());

        _dialoghi.RispostaConfermaSalvataggio = true;
        Assert.True(await vm.PuoChiudereAsync());
        Assert.False(vm.Documento.Modificato);
    }

    [Fact]
    public async Task Mappa_NuovaPerOgniDocumento()
    {
        var vm = CreaVm();
        var prima = vm.Mappa;
        Assert.Same(vm.Documento, prima.Documento);

        _dialoghi.RispostaApriFile = CopiaEsempio();
        await vm.ApriCommand.ExecuteAsync(null);

        Assert.NotSame(prima, vm.Mappa);
        Assert.Same(vm.Documento, vm.Mappa.Documento);
    }

    [Fact]
    public async Task Selezione_MappaEPannelloSincronizzati()
    {
        var vm = await VmConEsempioAperto();
        var ponte = vm.Documento.TrovaStanza("ponte")!;

        vm.Mappa.Selezione = ponte;
        Assert.Same(ponte, vm.ElementoSelezionato);

        var portone = vm.Documento.Porte.Single(p => p.Nome == "portone");
        vm.ElementoSelezionato = portone;
        Assert.Same(portone, vm.Mappa.Selezione);

        // Un oggetto selezionato altrove toglie la selezione dalla mappa senza perdere l'oggetto
        var oggetto = vm.Documento.Oggetti[0];
        vm.ElementoSelezionato = oggetto;
        Assert.Null(vm.Mappa.Selezione);
        Assert.Same(oggetto, vm.ElementoSelezionato);
    }

    [Fact]
    public async Task VaiAdAvviso_Stanza_CambiaLivelloDellaMappa()
    {
        var vm = await VmConEsempioAperto();
        var sommita = vm.Documento.TrovaStanza("sommita")!;
        vm.SchedaAttiva = MainWindowViewModel.SchedaGioco;

        vm.VaiAdAvvisoCommand.Execute(new Avviso(Gravita.Avviso, "m", sommita));

        Assert.Equal(MainWindowViewModel.SchedaMappa, vm.SchedaAttiva);
        Assert.Equal(1, vm.Mappa.LivelloCorrente);
        Assert.Same(sommita, vm.Mappa.Selezione);
    }
}

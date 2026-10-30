//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/MainWindowViewModel.cs" company="Casasoft">
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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Threading;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>
/// ViewModel della finestra principale: documento corrente, comandi del menu File e Compila,
/// avvisi del validatore e log della compilazione.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>Ritardo della validazione dopo una modifica.</summary>
    public static readonly TimeSpan RitardoValidazione = TimeSpan.FromMilliseconds(500);

    /// <summary>Indici delle schede della finestra.</summary>
    public const int SchedaMappa = 0, SchedaOggetti = 1, SchedaVerbi = 2, SchedaMessaggi = 3, SchedaGioco = 4;

    /// <summary>Indici delle schede del pannello inferiore.</summary>
    public const int SchedaAvvisi = 0, SchedaOutput = 1;

    private readonly IDialoghi _dialoghi;
    private readonly ImpostazioniUtente _impostazioni;
    private readonly string? _percorsoImpostazioni;
    private readonly Func<TimeSpan, Action, IDisposable> _pianificatore;
    private readonly ServizioCompilazione _compilazione = new();
    private IDisposable? _validazionePendente;

    /// <summary>Documento aperto.</summary>
    [ObservableProperty] private DocumentoViewModel _documento;

    /// <summary>Scheda attiva: 0 Mappa, 1 Oggetti, 2 Verbi, 3 Messaggi, 4 Gioco.</summary>
    [ObservableProperty] private int _schedaAttiva;

    /// <summary>Scheda del pannello inferiore: 0 Avvisi, 1 Output.</summary>
    [ObservableProperty] private int _schedaInferiore;

    /// <summary>Indica se il pannello inferiore (avvisi/output) è espanso.</summary>
    [ObservableProperty] private bool _pannelloInferioreAperto = true;

    /// <summary>Vero mentre il Builder è in esecuzione.</summary>
    [ObservableProperty] private bool _compilazioneInCorso;

    /// <summary>Nome base dei file generati; null per usare quello predefinito accanto al JSON.</summary>
    [ObservableProperty] private string? _outputBase;

    /// <summary>Elemento selezionato (stanza, porta, oggetto, verbo, messaggio o documento).</summary>
    [ObservableProperty] private object? _elementoSelezionato;

    /// <summary>Crea il ViewModel con un documento nuovo nella lingua dell'interfaccia.</summary>
    /// <param name="dialoghi">Dialoghi dell'interfaccia.</param>
    /// <param name="impostazioni">Impostazioni utente (recenti, ultima cartella di output).</param>
    /// <param name="percorsoImpostazioni">File in cui salvare le impostazioni; null per quello predefinito.</param>
    /// <param name="pianificatore">
    /// Esegue un'azione dopo un ritardo e restituisce un handle per annullarla;
    /// null per un timer del dispatcher Avalonia.
    /// </param>
    public MainWindowViewModel(IDialoghi dialoghi, ImpostazioniUtente impostazioni,
        string? percorsoImpostazioni = null, Func<TimeSpan, Action, IDisposable>? pianificatore = null)
    {
        _dialoghi = dialoghi;
        _impostazioni = impostazioni;
        _percorsoImpostazioni = percorsoImpostazioni;
        _pianificatore = pianificatore ?? ((ritardo, azione) => DispatcherTimer.RunOnce(azione, ritardo));
        foreach (var f in impostazioni.FileRecenti) Recenti.Add(f);

        var lingua = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "it"
            ? TemplateLanguage.Italiano
            : TemplateLanguage.English;
        _documento = ServizioFile.Nuovo(lingua);
        Iscrivi(_documento);
        ValidaOra();
    }

    /// <summary>Titolo della finestra: nome del file e asterisco se ci sono modifiche non salvate.</summary>
    public string Titolo =>
        "IFBuilder64 Editor - "
        + (Documento.Percorso is null ? Testi.SenzaTitolo : Path.GetFileName(Documento.Percorso))
        + (Documento.Modificato ? "*" : string.Empty);

    /// <summary>Avvisi ed errori dell'ultima validazione.</summary>
    public ObservableCollection<Avviso> Avvisi { get; } = [];

    /// <summary>Log dell'ultima compilazione seguito dai file generati.</summary>
    public ObservableCollection<string> LogCompilazione { get; } = [];

    /// <summary>File aperti di recente, dal più recente.</summary>
    public ObservableCollection<string> Recenti { get; } = [];

    /// <summary>Esegue subito la validazione, annullando quella pianificata.</summary>
    public void ValidaOra()
    {
        _validazionePendente?.Dispose();
        _validazionePendente = null;
        var lingua = ServizioFile.RilevaLingua(Documento.ToGame()) ?? TemplateLanguage.English;
        var avvisi = Validatore.Valida(Documento, ServizioFile.MessaggiTemplate(lingua).Count);
        Avvisi.Clear();
        foreach (var a in avvisi) Avvisi.Add(a);
    }

    /// <summary>Chiede se salvare le modifiche prima di chiudere; false se l'utente annulla.</summary>
    public Task<bool> PuoChiudereAsync() => ConfermaSostituzioneAsync();

    [RelayCommand]
    private async Task Nuovo()
    {
        if (!await ConfermaSostituzioneAsync()) return;
        if (await _dialoghi.ScegliTemplateAsync() is not { } lingua) return;
        ImpostaDocumento(ServizioFile.Nuovo(lingua));
    }

    [RelayCommand]
    private async Task Apri()
    {
        if (!await ConfermaSostituzioneAsync()) return;
        if (await _dialoghi.ApriFileAsync() is not { } percorso) return;
        await ApriPercorsoAsync(percorso);
    }

    [RelayCommand]
    private async Task ApriRecente(string? percorso)
    {
        if (string.IsNullOrEmpty(percorso)) return;
        if (!await ConfermaSostituzioneAsync()) return;
        await ApriPercorsoAsync(percorso);
    }

    [RelayCommand]
    private Task Salva() => SalvaDocumentoAsync();

    [RelayCommand]
    private Task SalvaConNome() => SalvaConNomeAsync();

    [RelayCommand]
    private async Task Compila()
    {
        if (!await SalvaDocumentoAsync()) return;
        // SalvaDocumentoAsync ha già validato
        if (Validatore.HaErrori(Avvisi.ToList()))
        {
            LogCompilazione.Clear();
            MostraPannello(SchedaAvvisi);
            return;
        }

        CompilazioneInCorso = true;
        LogCompilazione.Clear();
        try
        {
            var risultato = await _compilazione.CompilaAsync(
                Documento, OutputBase ?? ServizioCompilazione.OutputPredefinito(Documento.Percorso!));
            foreach (var riga in risultato.Log) LogCompilazione.Add(riga);
            foreach (var file in risultato.FileGenerati) LogCompilazione.Add(file);
        }
        catch (Exception ex)
        {
            LogCompilazione.Add(ex.Message);
        }
        finally
        {
            CompilazioneInCorso = false;
        }
        MostraPannello(SchedaOutput);
    }

    [RelayCommand]
    private async Task OpzioniCompilazione()
    {
        var corrente = OutputBase
            ?? (Documento.Percorso is null
                ? _impostazioni.UltimaCartellaOutput ?? string.Empty
                : ServizioCompilazione.OutputPredefinito(Documento.Percorso));
        if (await _dialoghi.OpzioniCompilazioneAsync(corrente) is not { } scelto) return;
        if (string.IsNullOrWhiteSpace(scelto))
        {
            OutputBase = null;
            return;
        }
        OutputBase = scelto;
        _impostazioni.UltimaCartellaOutput = Path.GetDirectoryName(Path.GetFullPath(scelto));
        await SalvaImpostazioniAsync();
    }

    [RelayCommand]
    private void VaiAdAvviso(Avviso? avviso)
    {
        if (avviso?.Elemento is not { } elemento) return;
        var scheda = elemento switch
        {
            StanzaViewModel or PortaViewModel => SchedaMappa,
            OggettoViewModel => SchedaOggetti,
            VerboViewModel => SchedaVerbi,
            MessaggioViewModel => SchedaMessaggi,
            DocumentoViewModel => SchedaGioco,
            _ => SchedaAttiva
        };
        SchedaAttiva = scheda;
        ElementoSelezionato = elemento;
    }

    [RelayCommand]
    private void Correggi(Avviso? avviso)
    {
        if (avviso?.Correzione is null) return;
        avviso.Correzione();
        ValidaOra();
    }

    /// <summary>
    /// Prima di sostituire il documento chiede se salvare le modifiche.
    /// Restituisce false se l'utente annulla o il salvataggio non riesce.
    /// </summary>
    private async Task<bool> ConfermaSostituzioneAsync()
    {
        if (!Documento.Modificato) return true;
        return await _dialoghi.ConfermaSalvataggioAsync() switch
        {
            true => await SalvaDocumentoAsync(),
            false => true,
            null => false
        };
    }

    private async Task ApriPercorsoAsync(string percorso)
    {
        DocumentoViewModel doc;
        try
        {
            doc = ServizioFile.Apri(percorso);
        }
        catch (ErroreApertura ex)
        {
            await _dialoghi.MostraErroreAsync(ex.Message);
            return;
        }
        ImpostaDocumento(doc);
        await AggiungiRecenteAsync(percorso);
    }

    /// <summary>Salva sul percorso corrente o, se manca, chiede il nome. False se annullato o in errore.</summary>
    private Task<bool> SalvaDocumentoAsync() =>
        Documento.Percorso is null ? SalvaConNomeAsync() : SalvaSuAsync(Documento.Percorso);

    private async Task<bool> SalvaConNomeAsync()
    {
        if (await _dialoghi.SalvaFileAsync(Documento.Percorso) is not { } percorso) return false;
        if (!await SalvaSuAsync(percorso)) return false;
        await AggiungiRecenteAsync(percorso);
        return true;
    }

    private async Task<bool> SalvaSuAsync(string percorso)
    {
        try
        {
            ServizioFile.Salva(Documento, percorso);
        }
        catch (Exception ex)
        {
            await _dialoghi.MostraErroreAsync(ex.Message);
            return false;
        }
        ValidaOra();
        return true;
    }

    private void ImpostaDocumento(DocumentoViewModel doc)
    {
        Documento = doc;
        OutputBase = null;
        ElementoSelezionato = null;
        LogCompilazione.Clear();
        ValidaOra();
    }

    partial void OnDocumentoChanged(DocumentoViewModel? oldValue, DocumentoViewModel newValue)
    {
        if (oldValue is not null)
        {
            oldValue.ContenutoCambiato -= DocumentoCambiato;
            oldValue.PropertyChanged -= DocumentoProprietaCambiata;
        }
        Iscrivi(newValue);
        OnPropertyChanged(nameof(Titolo));
    }

    private void Iscrivi(DocumentoViewModel doc)
    {
        doc.ContenutoCambiato += DocumentoCambiato;
        doc.PropertyChanged += DocumentoProprietaCambiata;
    }

    private void DocumentoCambiato(object? sender, EventArgs e)
    {
        _validazionePendente?.Dispose();
        _validazionePendente = _pianificatore(RitardoValidazione, ValidaOra);
    }

    private void DocumentoProprietaCambiata(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentoViewModel.Modificato) or nameof(DocumentoViewModel.Percorso))
            OnPropertyChanged(nameof(Titolo));
    }

    private void MostraPannello(int scheda)
    {
        PannelloInferioreAperto = true;
        SchedaInferiore = scheda;
    }

    private async Task AggiungiRecenteAsync(string percorso)
    {
        _impostazioni.AggiungiRecente(percorso);
        Recenti.Clear();
        foreach (var f in _impostazioni.FileRecenti) Recenti.Add(f);
        await SalvaImpostazioniAsync();
    }

    private async Task SalvaImpostazioniAsync()
    {
        try
        {
            _impostazioni.Salva(_percorsoImpostazioni);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _dialoghi.MostraErroreAsync(ex.Message);
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/OggettiViewModel.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Voce del menu a tendina della posizione di un oggetto.</summary>
/// <param name="Etichetta">Testo mostrato.</param>
/// <param name="Posizione">Valore di <c>location</c> (stanza o "player"), oppure null.</param>
/// <param name="DentroA">Valore di <c>initem</c> (Id del contenitore), oppure null.</param>
public record VocePosizione(string Etichetta, string? Posizione, string? DentroA);

/// <summary>Scheda Oggetti: elenco filtrabile degli oggetti con le operazioni su posizione, tipo, creazione ed eliminazione.</summary>
public partial class OggettiViewModel : ObservableObject, IDisposable
{
    private const string StanzaGiocatore = "player";

    private readonly IDialoghi _dialoghi;
    private readonly ObservableCollection<OggettoViewModel> _filtrati = [];

    /// <summary>Filtro per stanza: null per tutte.</summary>
    [ObservableProperty] private string? _filtroStanza;

    /// <summary>Filtro per tipo: null tutti, true solo statici, false solo mobili.</summary>
    [ObservableProperty] private bool? _filtroStatico;

    /// <summary>Oggetto selezionato.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DuplicaCommand), nameof(EliminaCommand))]
    private OggettoViewModel? _selezionato;

    private IReadOnlyList<string> _stanzeFiltro = [];

    /// <summary>Crea la scheda sul documento.</summary>
    public OggettiViewModel(DocumentoViewModel doc, IDialoghi dialoghi)
    {
        Documento = doc;
        _dialoghi = dialoghi;
        Riaggiorna();
        doc.ContenutoCambiato += DocumentoCambiato;
    }

    /// <summary>Documento mostrato.</summary>
    public DocumentoViewModel Documento { get; }

    /// <summary>Oggetti che soddisfano i filtri, nell'ordine del documento.</summary>
    public IEnumerable<OggettoViewModel> OggettiFiltrati => _filtrati;

    /// <summary>Voce "tutte" del filtro per stanza.</summary>
    public static string TutteLeStanze => Testi.Oggetti_FiltroStanza_Tutte;

    /// <summary>"Tutte" seguito dalle stanze (esclusa "player"), per il filtro.</summary>
    public IReadOnlyList<string> StanzeFiltro
    {
        get => _stanzeFiltro;
        private set => SetProperty(ref _stanzeFiltro, value);
    }

    /// <summary>Stanza scelta nel filtro; il ComboBox propone null quando cambia l'elenco e va ignorato.</summary>
    public string FiltroStanzaScelta
    {
        get => FiltroStanza ?? TutteLeStanze;
        set
        {
            if (value is null) return;
            FiltroStanza = value == TutteLeStanze ? null : value;
        }
    }

    /// <summary>Indice del filtro per tipo: 0 tutti, 1 statici, 2 mobili.</summary>
    public int FiltroTipoIndice
    {
        get => FiltroStatico switch { null => 0, true => 1, false => 2 };
        set
        {
            if (value < 0) return;
            FiltroStatico = value switch { 1 => true, 2 => false, _ => null };
        }
    }

    /// <summary>Voci della posizione dell'oggetto: solo stanze per gli statici; per gli altri anche nessuna, giocatore e contenitori.</summary>
    public IReadOnlyList<VocePosizione> PosizioniPer(OggettoViewModel o)
    {
        var voci = new List<VocePosizione>();
        var statico = o.Statico == true;
        if (!statico) voci.Add(new VocePosizione(Testi.Posizione_Nessuna, null, null));
        if (!statico) voci.Add(new VocePosizione(Testi.Posizione_Giocatore, StanzaGiocatore, null));
        foreach (var s in Documento.Stanze.Where(s => !s.EGiocatore))
            voci.Add(new VocePosizione(s.Nome, s.Nome, null));
        if (!statico)
            foreach (var c in Documento.Oggetti.Where(c => c != o && c.EContenitore == true && !ContenutoIn(c, o)))
                voci.Add(new VocePosizione(string.Format(Testi.Posizione_Dentro, c.Id), null, c.Id));

        // Un valore non più offerto (stanza o contenitore mancante) resta visibile finché non lo si cambia.
        var corrente = VoceCorrente(o);
        if (corrente is not null && !voci.Contains(corrente)) voci.Add(corrente);
        return voci;
    }

    /// <summary>Voce che rappresenta la posizione attuale dell'oggetto, oppure null se non ne ha.</summary>
    public VocePosizione? VoceCorrente(OggettoViewModel o)
    {
        if (o.DentroA is not null)
            return new VocePosizione(string.Format(Testi.Posizione_Dentro, o.DentroA), o.Posizione, o.DentroA);
        if (o.Posizione is null) return null;
        var etichetta = o.Posizione == StanzaGiocatore ? Testi.Posizione_Giocatore : o.Posizione;
        return new VocePosizione(etichetta, o.Posizione, null);
    }

    /// <summary>Sposta l'oggetto nella posizione scelta.</summary>
    public void ImpostaPosizione(OggettoViewModel o, VocePosizione v) =>
        Documento.SpostaOggetto(o, v.Posizione, v.DentroA);

    /// <summary>
    /// Cambia il tipo dell'oggetto. Diventando statico perde il contenitore e, se non era in una stanza, la posizione.
    /// </summary>
    public void ImpostaTipo(OggettoViewModel o, bool statico)
    {
        if ((o.Statico == true) == statico) return;
        if (statico)
        {
            var inStanza = Documento.TrovaStanza(o.Posizione) is { EGiocatore: false };
            // Mobile: l'Id non dipende dalla posizione, quindi nessun riferimento da aggiornare qui.
            Documento.SpostaOggetto(o, inStanza ? o.Posizione : null, null);
        }
        Documento.ImpostaStatico(o, statico);
    }

    /// <summary>Crea un oggetto mobile con nome univoco in coda all'elenco e lo seleziona.</summary>
    [RelayCommand]
    private void Nuovo()
    {
        var nuovo = new OggettoViewModel
        {
            Nome = Documento.NomeUnivoco(Testi.PrefissoOggetto, Documento.Oggetti.Select(o => o.Nome)),
            Descrizione = string.Empty
        };
        Documento.Oggetti.Add(nuovo);
        Seleziona(nuovo);
    }

    /// <summary>Duplica l'oggetto selezionato subito dopo l'originale e seleziona la copia.</summary>
    [RelayCommand(CanExecute = nameof(HaSelezione))]
    private void Duplica()
    {
        if (Selezionato is not { } o) return;
        var copia = OggettoViewModel.Da(o.ToItem());
        // La copia non eredita l'id personalizzato: prende quello automatico dal nuovo nome.
        copia.IdPersonalizzato = null;
        copia.Nome = Documento.NomeUnivoco(o.Nome, Documento.Oggetti.Select(x => x.Nome));
        Documento.Oggetti.Insert(Documento.Oggetti.IndexOf(o) + 1, copia);
        Seleziona(copia);
    }

    /// <summary>Elimina l'oggetto selezionato; se è riferito chiede conferma elencando i riferimenti.</summary>
    [RelayCommand(CanExecute = nameof(HaSelezione))]
    private async Task Elimina()
    {
        if (Selezionato is not { } o) return;
        var riferimenti = Documento.TrovaRiferimenti(o);
        if (riferimenti.Count > 0)
        {
            var elenco = string.Join(Environment.NewLine, riferimenti.Select(r => "- " + MappaViewModel.Descrivi(r)));
            if (!await _dialoghi.ConfermaAsync(Testi.Oggetti_Elimina_Titolo,
                    string.Format(Testi.Oggetti_Elimina_Messaggio, o.Id, elenco)))
                return;
        }
        Documento.Elimina(o);
    }

    /// <inheritdoc />
    public void Dispose() => Documento.ContenutoCambiato -= DocumentoCambiato;

    /// <summary>Seleziona l'oggetto appena creato; se i filtri lo nasconderebbero li azzera.</summary>
    private void Seleziona(OggettoViewModel o)
    {
        if (!Passa(o))
        {
            FiltroStanza = null;
            FiltroStatico = null;
        }
        Selezionato = o;
    }

    private bool HaSelezione() => Selezionato is not null;

    partial void OnFiltroStanzaChanged(string? value)
    {
        OnPropertyChanged(nameof(FiltroStanzaScelta));
        Riaggiorna();
    }

    partial void OnFiltroStaticoChanged(bool? value)
    {
        OnPropertyChanged(nameof(FiltroTipoIndice));
        Riaggiorna();
    }

    private void DocumentoCambiato(object? sender, EventArgs e) => Riaggiorna();

    /// <summary>Vero se <paramref name="c"/> è contenuto, direttamente o a catena, in <paramref name="o"/>.</summary>
    private bool ContenutoIn(OggettoViewModel c, OggettoViewModel o)
    {
        var visitati = new HashSet<OggettoViewModel>();
        for (OggettoViewModel? x = c; x is not null && visitati.Add(x); x = Documento.TrovaOggettoPerId(x.DentroA))
            if (x.DentroA is not null && Documento.TrovaOggettoPerId(x.DentroA) == o) return true;
        return false;
    }

    /// <summary>Ricalcola l'elenco filtrato e le stanze del filtro, toccando le collezioni solo se cambiano.</summary>
    private void Riaggiorna()
    {
        var stanze = new[] { TutteLeStanze }
            .Concat(Documento.Stanze.Where(s => !s.EGiocatore).Select(s => s.Nome)).ToList();
        if (FiltroStanza is not null && !stanze.Contains(FiltroStanza))
        {
            // La stanza filtrata non esiste più (eliminata o rinominata): torna a "tutte".
            FiltroStanza = null; // richiama Riaggiorna
            return;
        }
        if (!stanze.SequenceEqual(StanzeFiltro))
        {
            StanzeFiltro = stanze;
            OnPropertyChanged(nameof(FiltroStanzaScelta));
        }

        var nuovi = Documento.Oggetti.Where(Passa).ToList();
        if (!_filtrati.SequenceEqual(nuovi))
        {
            // Aggiornamento puntuale per non perdere la selezione della griglia
            for (var i = _filtrati.Count - 1; i >= 0; i--)
                if (!nuovi.Contains(_filtrati[i])) _filtrati.RemoveAt(i);
            for (var i = 0; i < nuovi.Count; i++)
            {
                if (i < _filtrati.Count && _filtrati[i] == nuovi[i]) continue;
                var attuale = _filtrati.IndexOf(nuovi[i]);
                if (attuale >= 0) _filtrati.Move(attuale, i);
                else _filtrati.Insert(i, nuovi[i]);
            }
        }

        if (Selezionato is not null && !_filtrati.Contains(Selezionato)) Selezionato = null;
    }

    private bool Passa(OggettoViewModel o) =>
        (FiltroStanza is null || o.Posizione == FiltroStanza)
        && (FiltroStatico is null || (o.Statico == true) == FiltroStatico);
}

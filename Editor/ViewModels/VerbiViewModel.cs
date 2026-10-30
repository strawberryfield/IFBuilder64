//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/VerbiViewModel.cs" company="Casasoft">
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

/// <summary>Scheda Verbi: elenco filtrabile dei verbi con comando e azione modificabili in linea.</summary>
public partial class VerbiViewModel : ObservableObject, IDisposable
{
    private readonly ObservableCollection<VerboViewModel> _filtrati = [];

    /// <summary>Filtro per azione: null per tutte.</summary>
    [ObservableProperty] private AzioneMotore? _filtroAzione;

    /// <summary>Verbo selezionato.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EliminaCommand), nameof(AggiungiSinonimoCommand))]
    private VerboViewModel? _selezionato;

    /// <summary>Crea la scheda sul documento.</summary>
    public VerbiViewModel(DocumentoViewModel doc)
    {
        Documento = doc;
        VociFiltro = new[] { Testi.Verbi_Filtro_Tutte }.Concat(CatalogoAzioni.Tutte.Select(a => a.ToString())).ToList();
        Riaggiorna();
        doc.ContenutoCambiato += DocumentoCambiato;
    }

    /// <summary>Documento mostrato.</summary>
    public DocumentoViewModel Documento { get; }

    /// <summary>Verbi che soddisfano il filtro, nell'ordine del documento.</summary>
    public IEnumerable<VerboViewModel> VerbiFiltrati => _filtrati;

    /// <summary>Voci del filtro: "tutte" seguito dalle azioni del catalogo.</summary>
    public IReadOnlyList<string> VociFiltro { get; }

    /// <summary>Indice nel filtro: 0 tutte, altrimenti codice dell'azione + 1; il ComboBox può proporre -1 e va ignorato.</summary>
    public int FiltroIndice
    {
        get => FiltroAzione is null ? 0 : FiltroAzione.Codice + 1;
        set
        {
            if (value < 0) return;
            FiltroAzione = value == 0 ? null : CatalogoAzioni.Trova((byte)(value - 1));
        }
    }

    /// <summary>
    /// Azioni offerte per il verbo: il catalogo e, per un codice fuori catalogo,
    /// una voce "? (n)" solo per quella riga.
    /// </summary>
    public IReadOnlyList<AzioneMotore> AzioniPer(VerboViewModel v)
    {
        var voci = CatalogoAzioni.Tutte.ToList();
        if (!CatalogoAzioni.Esiste(v.Azione)) voci.Add(new AzioneMotore(v.Azione, "?"));
        return voci;
    }

    /// <summary>Voce che rappresenta l'azione attuale del verbo tra quelle di <see cref="AzioniPer"/>.</summary>
    public AzioneMotore VoceCorrente(VerboViewModel v) => AzioniPer(v).First(a => a.Codice == v.Azione);

    /// <summary>
    /// Imposta l'azione scelta dall'utente. Il null del ComboBox e la voce "? (n)" non sono scelte
    /// e lasciano invariato il verbo.
    /// </summary>
    public void ImpostaAzione(VerboViewModel v, AzioneMotore? scelta)
    {
        if (scelta is null || !CatalogoAzioni.Esiste(scelta.Codice)) return;
        v.Azione = scelta.Codice;
    }

    /// <summary>Seleziona il verbo; se il filtro lo nasconderebbe lo azzera.</summary>
    public void Seleziona(VerboViewModel v)
    {
        if (!Passa(v)) FiltroAzione = null;
        Selezionato = v;
    }

    /// <summary>Aggiunge in coda un verbo con nome univoco e l'azione del filtro (o 0) e lo seleziona.</summary>
    [RelayCommand]
    private void Nuovo()
    {
        var nuovo = new VerboViewModel
        {
            Comando = Documento.NomeUnivoco(Testi.PrefissoVerbo, Documento.Verbi.Select(v => v.Comando)),
            Azione = FiltroAzione?.Codice ?? 0
        };
        Documento.Verbi.Add(nuovo);
        Seleziona(nuovo);
    }

    /// <summary>Elimina il verbo selezionato, senza conferma.</summary>
    [RelayCommand(CanExecute = nameof(HaSelezione))]
    private void Elimina()
    {
        if (Selezionato is { } v) Documento.Verbi.Remove(v);
    }

    /// <summary>Crea subito dopo il selezionato un verbo con la stessa azione e lo seleziona.</summary>
    [RelayCommand(CanExecute = nameof(HaSelezione))]
    private void AggiungiSinonimo()
    {
        if (Selezionato is not { } v) return;
        var sinonimo = new VerboViewModel
        {
            Comando = Documento.NomeUnivoco(v.Comando, Documento.Verbi.Select(x => x.Comando)),
            Azione = v.Azione
        };
        Documento.Verbi.Insert(Documento.Verbi.IndexOf(v) + 1, sinonimo);
        Seleziona(sinonimo);
    }

    /// <inheritdoc />
    public void Dispose() => Documento.ContenutoCambiato -= DocumentoCambiato;

    private bool HaSelezione() => Selezionato is not null;

    partial void OnFiltroAzioneChanged(AzioneMotore? value)
    {
        OnPropertyChanged(nameof(FiltroIndice));
        Riaggiorna();
    }

    private void DocumentoCambiato(object? sender, EventArgs e) => Riaggiorna();

    private bool Passa(VerboViewModel v) => FiltroAzione is null || v.Azione == FiltroAzione.Codice;

    /// <summary>Ricalcola l'elenco filtrato toccando la collezione solo se cambia.</summary>
    private void Riaggiorna()
    {
        var nuovi = Documento.Verbi.Where(Passa).ToList();
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
}

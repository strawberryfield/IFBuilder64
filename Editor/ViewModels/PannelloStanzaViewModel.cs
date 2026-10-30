//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/PannelloStanzaViewModel.cs" company="Casasoft">
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

/// <summary>Uscita di una stanza mostrata (in sola lettura) nel pannello; il clic seleziona la stanza di arrivo.</summary>
/// <param name="Etichetta">Nome della direzione.</param>
/// <param name="Destinazione">Nome della stanza di arrivo, oppure null.</param>
/// <param name="Seleziona">Comando che seleziona la stanza di arrivo.</param>
public record UscitaPannello(string Etichetta, string? Destinazione, IRelayCommand Seleziona)
{
    /// <summary>Testo da mostrare: direzione e destinazione.</summary>
    public string Testo => $"{Etichetta}: {Destinazione ?? Testi.Stanza_NessunaUscita}";
}

/// <summary>Pannello proprietà di una stanza (mai la stanza "player").</summary>
public partial class PannelloStanzaViewModel : ObservableObject, IDisposable
{
    private readonly DocumentoViewModel _documento;
    private readonly MappaViewModel _mappa;
    private readonly IDialoghi _dialoghi;
    private readonly Action<object?> _seleziona;

    /// <summary>Crea il pannello della stanza.</summary>
    /// <param name="documento">Documento che contiene la stanza.</param>
    /// <param name="stanza">Stanza mostrata.</param>
    /// <param name="mappa">Mappa, per impostare la stanza iniziale.</param>
    /// <param name="dialoghi">Dialoghi dell'interfaccia.</param>
    /// <param name="seleziona">Seleziona un elemento (usata per raggiungere le stanze di arrivo).</param>
    public PannelloStanzaViewModel(DocumentoViewModel documento, StanzaViewModel stanza, MappaViewModel mappa,
        IDialoghi dialoghi, Action<object?> seleziona)
    {
        _documento = documento;
        Stanza = stanza;
        _mappa = mappa;
        _dialoghi = dialoghi;
        _seleziona = seleziona;
        Nome = new CampoNomeViewModel(() => stanza.Nome, nuovo => documento.RinominaStanza(stanza, nuovo));
        Aggiorna();
        documento.ContenutoCambiato += DocumentoCambiato;
    }

    /// <summary>Stanza mostrata.</summary>
    public StanzaViewModel Stanza { get; }

    /// <summary>Campo del nome.</summary>
    public CampoNomeViewModel Nome { get; }

    /// <summary>Le sei uscite, in sola lettura.</summary>
    public ObservableCollection<UscitaPannello> Uscite { get; } = [];

    /// <summary>Oggetti statici presenti nella stanza.</summary>
    public ObservableCollection<OggettoViewModel> Statici { get; } = [];

    /// <summary>Oggetti mobili presenti nella stanza.</summary>
    public ObservableCollection<OggettoViewModel> Mobili { get; } = [];

    /// <summary>Vero se la stanza non è già quella iniziale.</summary>
    public bool PuoImpostareIniziale => _documento.StanzaIniziale != Stanza.Nome;

    /// <summary>Imposta la stanza come stanza iniziale.</summary>
    [RelayCommand(CanExecute = nameof(PuoImpostareIniziale))]
    private void ImpostaIniziale() => _mappa.ImpostaIniziale(Stanza);

    /// <summary>Crea un oggetto mobile in questa stanza.</summary>
    [RelayCommand]
    private void NuovoOggettoQui() =>
        _documento.Oggetti.Add(new OggettoViewModel
        {
            Nome = _documento.NomeUnivoco(Testi.PrefissoOggetto, _documento.Oggetti.Select(o => o.Nome)),
            Posizione = Stanza.Nome,
            Descrizione = string.Empty
        });

    /// <summary>Chiede un oggetto non ancora presente nella stanza e lo sposta qui.</summary>
    [RelayCommand]
    private Task SpostaQui() => _mappa.EseguiAsync(async () =>
    {
        var candidati = _documento.Oggetti.Where(o => o.Posizione != Stanza.Nome).ToList();
        if (await _dialoghi.ScegliOggettoAsync(Testi.Stanza_ScegliOggetto, candidati) is { } scelto)
            _documento.SpostaOggetto(scelto, Stanza.Nome, null);
    });

    /// <inheritdoc />
    public void Dispose() => _documento.ContenutoCambiato -= DocumentoCambiato;

    private void DocumentoCambiato(object? sender, EventArgs e) => Aggiorna();

    private void Aggiorna()
    {
        Uscite.Clear();
        var nomi = _documento.Direzioni.Count == 6 ? _documento.Direzioni.Select(m => m.Testo).ToList() : null;
        foreach (var d in Enum.GetValues<Direzione>())
        {
            var arrivo = _documento.TrovaStanza(Stanza.Uscita(d));
            var comando = new RelayCommand(() => _seleziona(arrivo), () => arrivo is { EGiocatore: false });
            Uscite.Add(new UscitaPannello(nomi?[(int)d] ?? d.ToString(), Stanza.Uscita(d), comando));
        }

        Riempi(Statici, _documento.Oggetti.Where(o => o.Posizione == Stanza.Nome && o.Statico == true));
        Riempi(Mobili, _documento.Oggetti.Where(o => o.Posizione == Stanza.Nome && o.Statico != true));
        OnPropertyChanged(nameof(PuoImpostareIniziale));
        ImpostaInizialeCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Sostituisce il contenuto solo se cambia, per non perdere la selezione nelle liste.</summary>
    private static void Riempi(ObservableCollection<OggettoViewModel> lista, IEnumerable<OggettoViewModel> nuovi)
    {
        var elenco = nuovi.ToList();
        if (lista.SequenceEqual(elenco)) return;
        lista.Clear();
        foreach (var o in elenco) lista.Add(o);
    }
}

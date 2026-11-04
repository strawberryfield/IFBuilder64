//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/DocumentoViewModel.cs" company="Casasoft">
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
using System.Collections.Specialized;
using System.ComponentModel;
using Casasoft.IFBuilder64.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Documento aperto nell'editor: contenuto del gioco, percorso del file e stato di modifica.</summary>
public partial class DocumentoViewModel : ObservableObject
{
    [ObservableProperty] private string? _percorso;
    [ObservableProperty] private bool _modificato;
    [ObservableProperty] private string _intro = string.Empty;
    [ObservableProperty] private string _stanzaIniziale = string.Empty;
    [ObservableProperty] private byte _caricoMassimo = 10;
    [ObservableProperty] private IFictionViewModel? _iFiction;

    /// <summary>Stanze, nell'ordine del file (la prima è "player").</summary>
    public ObservableCollection<StanzaViewModel> Stanze { get; } = [];

    /// <summary>Oggetti, nell'ordine del file.</summary>
    public ObservableCollection<OggettoViewModel> Oggetti { get; } = [];

    /// <summary>Porte, nell'ordine del file.</summary>
    public ObservableCollection<PortaViewModel> Porte { get; } = [];

    /// <summary>Verbi, nell'ordine del file.</summary>
    public ObservableCollection<VerboViewModel> Verbi { get; } = [];

    /// <summary>Messaggi standard del motore.</summary>
    public ObservableCollection<MessaggioViewModel> MessaggiStandard { get; } = [];

    /// <summary>Messaggi personalizzati del gioco.</summary>
    public ObservableCollection<MessaggioViewModel> MessaggiUtente { get; } = [];

    /// <summary>Nomi delle sei direzioni.</summary>
    public ObservableCollection<MessaggioViewModel> Direzioni { get; } = [];

    /// <summary>Sollevato a ogni modifica a proprietà o collezioni annidate.</summary>
    public event EventHandler? ContenutoCambiato;

    /// <summary>Crea un documento vuoto.</summary>
    public DocumentoViewModel()
    {
        Sorveglia(Stanze);
        Sorveglia(Oggetti);
        Sorveglia(Porte);
        Sorveglia(Verbi);
        Sorveglia(MessaggiStandard);
        Sorveglia(MessaggiUtente);
        Sorveglia(Direzioni);
    }

    /// <summary>Crea il documento a partire dal modello JSON; il documento risulta non modificato.</summary>
    public static DocumentoViewModel DaGame(Game g)
    {
        var d = new DocumentoViewModel
        {
            Intro = g.intro,
            StanzaIniziale = g.initialroom,
            CaricoMassimo = g.maxload,
            IFiction = g.ifiction is null ? null : IFictionViewModel.Da(g.ifiction)
        };
        foreach (var r in g.rooms) d.Stanze.Add(StanzaViewModel.Da(r));
        foreach (var i in g.items) d.Oggetti.Add(OggettoViewModel.Da(i));
        foreach (var p in g.doors) d.Porte.Add(PortaViewModel.Da(p));
        foreach (var v in g.verbs) d.Verbi.Add(VerboViewModel.Da(v));
        Riempi(d.MessaggiStandard, g.messages);
        Riempi(d.MessaggiUtente, g.usermessages);
        Riempi(d.Direzioni, g.directions);
        d.NormalizzaSblocchi();
        d.Modificato = false;
        return d;
    }

    /// <summary>
    /// Converte in id gli sblocchi (di oggetti e porte) scritti come nome: uno sblocco che non
    /// corrisponde ad alcun id ma corrisponde a un nome diventa l'id del primo oggetto con quel nome
    /// nell'ordine usato dal Builder (<c>Game.SortItems</c>), cioè la stessa scelta del Builder.
    /// </summary>
    private void NormalizzaSblocchi()
    {
        var ordinati = new Game { items = Oggetti.Select(o => o.ToItem()).ToList() };
        ordinati.SortItems();
        string? Normalizza(string? sblocco)
        {
            if (string.IsNullOrEmpty(sblocco) || Oggetti.Any(o => o.Id == sblocco)) return sblocco;
            var indice = ordinati.getItemIndexByName(sblocco);
            return indice == 255 ? sblocco : ordinati.items[indice].id;
        }
        foreach (var o in Oggetti) o.OggettoSblocco = Normalizza(o.OggettoSblocco);
        foreach (var p in Porte) p.OggettoSblocco = Normalizza(p.OggettoSblocco);
    }

    /// <summary>Converte il documento nel modello JSON.</summary>
    public Game ToGame() => new()
    {
        intro = Intro,
        initialroom = StanzaIniziale,
        maxload = CaricoMassimo,
        ifiction = IFiction?.ToIFiction(),
        rooms = Stanze.Select(s => s.ToRoom()).ToList(),
        items = Oggetti.Select(o => o.ToItem()).ToList(),
        doors = Porte.Select(p => p.ToDoor()).ToList(),
        verbs = Verbi.Select(v => v.ToVerb()).ToList(),
        messages = MessaggiStandard.Select(m => m.Testo).ToList(),
        usermessages = MessaggiUtente.Select(m => m.Testo).ToList(),
        directions = Direzioni.Select(m => m.Testo).ToList()
    };

    /// <summary>Trova una stanza per nome (null se assente).</summary>
    public StanzaViewModel? TrovaStanza(string? nome) =>
        nome is null ? null : Stanze.FirstOrDefault(s => s.Nome == nome);

    /// <summary>Trova un oggetto per identificatore (<c>nome</c> o <c>nome@posizione</c>).</summary>
    public OggettoViewModel? TrovaOggettoPerId(string? id) =>
        id is null ? null : Oggetti.FirstOrDefault(o => o.Id == id);

    /// <summary>Restituisce prefisso + N, con N minimo (da 1) non presente tra i nomi esistenti.</summary>
    public string NomeUnivoco(string prefisso, IEnumerable<string> esistenti)
    {
        var usati = new HashSet<string>(esistenti, StringComparer.OrdinalIgnoreCase);
        var n = 1;
        while (usati.Contains(prefisso + n)) n++;
        return prefisso + n;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        // Percorso e Modificato non sono contenuto; IFiction è gestito da OnIFictionChanged.
        if (e.PropertyName is nameof(Modificato) or nameof(Percorso) or nameof(IFiction)) return;
        SegnaModificato();
    }

    partial void OnIFictionChanged(IFictionViewModel? oldValue, IFictionViewModel? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ElementoCambiato;
        if (newValue is not null) newValue.PropertyChanged += ElementoCambiato;
        SegnaModificato();
    }

    private static void Riempi(ObservableCollection<MessaggioViewModel> destinazione, List<string> testi)
    {
        for (var i = 0; i < testi.Count; i++)
            destinazione.Add(new MessaggioViewModel { Indice = i, Testo = testi[i] });
    }

    private void SegnaModificato()
    {
        Modificato = true;
        ContenutoCambiato?.Invoke(this, EventArgs.Empty);
    }

    private void ElementoCambiato(object? sender, PropertyChangedEventArgs e) => SegnaModificato();

    /// <summary>Segue la collezione e i suoi elementi, anche quelli aggiunti dopo.</summary>
    private void Sorveglia<T>(ObservableCollection<T> collezione) where T : INotifyPropertyChanged
    {
        var iscritti = new List<T>();
        collezione.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var t in iscritti) t.PropertyChanged -= ElementoCambiato;
                iscritti.Clear();
                foreach (var t in collezione) Iscrivi(t);
            }
            else
            {
                if (e.OldItems is not null)
                    foreach (T t in e.OldItems) { t.PropertyChanged -= ElementoCambiato; iscritti.Remove(t); }
                if (e.NewItems is not null)
                    foreach (T t in e.NewItems) Iscrivi(t);
            }
            SegnaModificato();
        };

        void Iscrivi(T t)
        {
            t.PropertyChanged += ElementoCambiato;
            iscritti.Add(t);
        }
    }
}

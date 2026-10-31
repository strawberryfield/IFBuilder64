//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/PannelloOggettoViewModel.cs" company="Casasoft">
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
using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Pannello proprietà di un oggetto: nome, descrizioni e oggetto di sblocco.</summary>
public partial class PannelloOggettoViewModel : ObservableObject, IDisposable, IConfermabile
{
    private readonly DocumentoViewModel _documento;
    private IReadOnlyList<string> _oggettiSblocco = [];

    /// <summary>Crea il pannello dell'oggetto.</summary>
    public PannelloOggettoViewModel(DocumentoViewModel documento, OggettoViewModel oggetto)
    {
        _documento = documento;
        Oggetto = oggetto;
        Nome = new CampoNomeViewModel(() => oggetto.Nome, nuovo => documento.RinominaOggetto(oggetto, nuovo));
        AggiornaOggetti();
        documento.ContenutoCambiato += DocumentoCambiato;
    }

    /// <summary>Oggetto mostrato.</summary>
    public OggettoViewModel Oggetto { get; }

    /// <summary>Campo del nome.</summary>
    public CampoNomeViewModel Nome { get; }

    /// <summary>Voce che rappresenta l'assenza di oggetto di sblocco.</summary>
    public static string Nessuno => Testi.Nessuno;

    /// <summary>"Nessuno" seguito dai nomi degli altri oggetti (distinti, in ordine alfabetico); include lo sblocco corrente anche se non esiste più.</summary>
    public IReadOnlyList<string> OggettiSblocco
    {
        get => _oggettiSblocco;
        private set => SetProperty(ref _oggettiSblocco, value);
    }

    /// <summary>Oggetto di sblocco scelto; "Nessuno" se l'oggetto non ne ha.</summary>
    public string SbloccoSelezionato
    {
        get => Oggetto.OggettoSblocco ?? Nessuno;
        set
        {
            // Il ComboBox propone null quando cambia l'elenco: va ignorato, solo la voce "Nessuno" cancella lo sblocco.
            if (value is null) return;
            var nuovo = value == Nessuno ? null : value;
            if (nuovo == Oggetto.OggettoSblocco) return;
            Oggetto.OggettoSblocco = nuovo;
            OnPropertyChanged();
        }
    }

    /// <summary>Descrizione da aperto; il testo vuoto equivale all'assenza.</summary>
    public string DescrizioneAperto
    {
        get => Oggetto.DescrizioneAperto ?? string.Empty;
        set
        {
            var nuovo = string.IsNullOrEmpty(value) ? null : value;
            if (nuovo == Oggetto.DescrizioneAperto) return;
            Oggetto.DescrizioneAperto = nuovo;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public void ConfermaModifiche() => Nome.ConfermaSeModificato();

    /// <inheritdoc />
    public void Dispose() => _documento.ContenutoCambiato -= DocumentoCambiato;

    private void DocumentoCambiato(object? sender, EventArgs e) => AggiornaOggetti();

    private void AggiornaOggetti()
    {
        var nomi = _documento.Oggetti.Where(o => o != Oggetto).Select(o => o.Nome).Append(Oggetto.OggettoSblocco)
            .Where(n => !string.IsNullOrEmpty(n)).Select(n => n!).Distinct().Order(StringComparer.Ordinal);
        var elenco = new[] { Nessuno }.Concat(nomi).ToList();
        if (elenco.SequenceEqual(OggettiSblocco)) return;
        OggettiSblocco = elenco;
        OnPropertyChanged(nameof(SbloccoSelezionato));
    }
}

//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/PannelloPortaViewModel.cs" company="Casasoft">
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
using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Pannello proprietà di una porta.</summary>
public partial class PannelloPortaViewModel : ObservableObject, IDisposable, IConfermabile
{
    private readonly DocumentoViewModel _documento;
    private IReadOnlyList<string> _oggettiSblocco = [];

    /// <summary>Crea il pannello della porta.</summary>
    public PannelloPortaViewModel(DocumentoViewModel documento, PortaViewModel porta)
    {
        _documento = documento;
        Porta = porta;
        Nome = new CampoNomeViewModel(() => porta.Nome, nuovo => documento.RinominaPorta(porta, nuovo));
        AggiornaOggetti();
        documento.ContenutoCambiato += DocumentoCambiato;
    }

    /// <summary>Porta mostrata.</summary>
    public PortaViewModel Porta { get; }

    /// <summary>Campo del nome.</summary>
    public CampoNomeViewModel Nome { get; }

    /// <summary>Voce che rappresenta l'assenza di oggetto di sblocco.</summary>
    public static string Nessuno => Testi.Nessuno;

    /// <summary>"Nessuno" seguito dai nomi degli oggetti (distinti, in ordine alfabetico); include lo sblocco corrente anche se non esiste più.</summary>
    public IReadOnlyList<string> OggettiSblocco
    {
        get => _oggettiSblocco;
        private set => SetProperty(ref _oggettiSblocco, value);
    }

    /// <summary>Oggetto di sblocco scelto; "Nessuno" se la porta non ne ha.</summary>
    public string SbloccoSelezionato
    {
        get => Porta.OggettoSblocco ?? Nessuno;
        set
        {
            // Il ComboBox propone null quando cambia l'elenco: va ignorato, solo la voce "Nessuno" cancella lo sblocco.
            if (value is null) return;
            var nuovo = value == Nessuno ? null : value;
            if (nuovo == Porta.OggettoSblocco) return;
            Porta.OggettoSblocco = nuovo;
            OnPropertyChanged();
        }
    }

    /// <summary>Messaggio di apertura negata; il testo vuoto equivale all'assenza.</summary>
    public string MessaggioNegato
    {
        get => Porta.MessaggioNegato ?? string.Empty;
        set
        {
            var nuovo = string.IsNullOrEmpty(value) ? null : value;
            if (nuovo == Porta.MessaggioNegato) return;
            Porta.MessaggioNegato = nuovo;
            OnPropertyChanged();
        }
    }

    /// <summary>Stato iniziale: aperta. Cambiarlo scrive o rimuove le uscite nelle due stanze.</summary>
    public bool Aperta
    {
        get => Porta.Aperta == true;
        set
        {
            if (value == Aperta) return;
            Collegamenti.ImpostaAperta(_documento, Porta, value);
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
        var nomi = _documento.Oggetti.Select(o => o.Id).Append(Porta.OggettoSblocco)
            .Where(n => !string.IsNullOrEmpty(n)).Select(n => n!).Distinct().Order(StringComparer.Ordinal);
        var elenco = new[] { Nessuno }.Concat(nomi).ToList();
        if (elenco.SequenceEqual(OggettiSblocco)) return;
        OggettiSblocco = elenco;
        OnPropertyChanged(nameof(SbloccoSelezionato));
    }
}

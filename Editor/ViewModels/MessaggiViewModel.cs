//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/MessaggiViewModel.cs" company="Casasoft">
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

using System.ComponentModel;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>
/// Scheda Messaggi: messaggi standard (solo modifica e ripristino del testo del template)
/// e messaggi personalizzati (aggiunta, eliminazione e riordino).
/// </summary>
public partial class MessaggiViewModel : ObservableObject, IDisposable
{
    private readonly IDialoghi _dialoghi;

    /// <summary>Messaggio standard selezionato.</summary>
    [ObservableProperty] private MessaggioViewModel? _selezionatoStandard;

    /// <summary>Messaggio personalizzato selezionato.</summary>
    [ObservableProperty] private MessaggioViewModel? _selezionatoUtente;

    /// <summary>Crea la scheda sul documento.</summary>
    public MessaggiViewModel(DocumentoViewModel doc, IDialoghi dialoghi)
    {
        Documento = doc;
        _dialoghi = dialoghi;
        foreach (var m in doc.MessaggiStandard) m.PropertyChanged += StandardCambiato;
        doc.MessaggiUtente.CollectionChanged += (_, _) => AggiornaComandiOrdine();
    }

    /// <summary>Documento mostrato.</summary>
    public DocumentoViewModel Documento { get; }

    /// <summary>Seleziona un messaggio (standard o personalizzato) nell'elenco corrispondente.</summary>
    public void Seleziona(MessaggioViewModel m)
    {
        if (Documento.MessaggiUtente.Contains(m))
        {
            SelezionatoStandard = null;
            SelezionatoUtente = m;
        }
        else if (Documento.MessaggiStandard.Contains(m))
        {
            SelezionatoUtente = null;
            SelezionatoStandard = m;
        }
    }

    /// <summary>Riporta il messaggio standard al testo del template.</summary>
    [RelayCommand(CanExecute = nameof(PuoRipristinare))]
    private void Ripristina(MessaggioViewModel? m)
    {
        if (m is { TestoTemplate: { } t }) m.Testo = t;
    }

    /// <summary>Aggiunge in coda un messaggio personalizzato vuoto, senza conferma.</summary>
    [RelayCommand]
    private void Aggiungi()
    {
        var nuovo = new MessaggioViewModel { Indice = Documento.MessaggiUtente.Count, Testo = string.Empty };
        Documento.MessaggiUtente.Add(nuovo);
        Seleziona(nuovo);
    }

    /// <summary>Elimina il messaggio personalizzato dopo conferma e rinumera i successivi.</summary>
    [RelayCommand]
    private async Task EliminaAsync(MessaggioViewModel? m)
    {
        if (m is null || !Documento.MessaggiUtente.Contains(m)) return;
        if (!await _dialoghi.ConfermaAsync(Testi.Messaggi_Elimina_Titolo, string.Format(Testi.Messaggi_Elimina_Messaggio, m.Indice))) return;
        Documento.MessaggiUtente.Remove(m);
        Rinumera();
    }

    /// <summary>Sposta il messaggio personalizzato di una posizione verso l'inizio, dopo conferma.</summary>
    [RelayCommand(CanExecute = nameof(PuoSalire))]
    private Task SuAsync(MessaggioViewModel? m) => SpostaAsync(m, -1);

    /// <summary>Sposta il messaggio personalizzato di una posizione verso la fine, dopo conferma.</summary>
    [RelayCommand(CanExecute = nameof(PuoScendere))]
    private Task GiuAsync(MessaggioViewModel? m) => SpostaAsync(m, 1);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var m in Documento.MessaggiStandard) m.PropertyChanged -= StandardCambiato;
    }

    private async Task SpostaAsync(MessaggioViewModel? m, int passo)
    {
        if (m is null) return;
        var da = Documento.MessaggiUtente.IndexOf(m);
        var a = da + passo;
        if (da < 0 || a < 0 || a >= Documento.MessaggiUtente.Count) return;
        if (!await _dialoghi.ConfermaAsync(Testi.Messaggi_Sposta_Titolo, string.Format(Testi.Messaggi_Sposta_Messaggio, da, a))) return;
        Documento.MessaggiUtente.Move(da, a);
        Rinumera();
        Seleziona(m);
    }

    private void Rinumera()
    {
        for (var i = 0; i < Documento.MessaggiUtente.Count; i++) Documento.MessaggiUtente[i].Indice = i;
    }

    private static bool PuoRipristinare(MessaggioViewModel? m) => m is { TestoTemplate: { } t } && m.Testo != t;

    private bool PuoSalire(MessaggioViewModel? m) => m is not null && Documento.MessaggiUtente.IndexOf(m) > 0;

    private bool PuoScendere(MessaggioViewModel? m) =>
        m is not null && Documento.MessaggiUtente.IndexOf(m) is var i && i >= 0 && i < Documento.MessaggiUtente.Count - 1;

    private void StandardCambiato(object? sender, PropertyChangedEventArgs e) => RipristinaCommand.NotifyCanExecuteChanged();

    private void AggiornaComandiOrdine()
    {
        SuCommand.NotifyCanExecuteChanged();
        GiuCommand.NotifyCanExecuteChanged();
    }
}

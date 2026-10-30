//-----------------------------------------------------------------------
// <copyright file="Editor/Views/VerbiView.axaml.cs" company="Casasoft">
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
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Scheda Verbi: barra con azioni e filtro, e griglia dei verbi; il DataContext è <see cref="VerbiViewModel"/>.</summary>
public partial class VerbiView : UserControl
{
    private const int ColonnaAzione = 1;

    /// <summary>Crea la vista.</summary>
    public VerbiView()
    {
        InitializeComponent();
        ((DataGridTemplateColumn)Griglia.Columns[ColonnaAzione]).CellTemplate =
            new FuncDataTemplate<VerboViewModel>((v, _) => v is null ? null : CreaCellaAzione(v));
    }

    /// <summary>Menu a tendina dell'azione della riga; un codice fuori catalogo resta selezionato come "? (n)".</summary>
    private Control? CreaCellaAzione(VerboViewModel v)
    {
        if (DataContext is not VerbiViewModel vm) return null;
        var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        var aggiornando = false;
        IReadOnlyList<AzioneMotore> voci = [];

        void Aggiorna()
        {
            aggiornando = true;
            var nuove = vm.AzioniPer(v);
            if (!nuove.SequenceEqual(voci))
            {
                voci = nuove;
                combo.ItemsSource = voci;
            }
            combo.SelectedItem = vm.VoceCorrente(v);
            aggiornando = false;
        }

        combo.SelectionChanged += (_, _) =>
        {
            // Il null arriva quando cambia l'elenco o la riga viene riciclata: non è una scelta dell'utente.
            if (aggiornando) return;
            vm.ImpostaAzione(v, combo.SelectedItem as AzioneMotore);
        };
        Aggiorna();

        void Gestore(object? sender, EventArgs e) => Aggiorna();
        combo.AttachedToVisualTree += (_, _) =>
        {
            vm.Documento.ContenutoCambiato -= Gestore;
            vm.Documento.ContenutoCambiato += Gestore;
            Aggiorna();
        };
        combo.DetachedFromVisualTree += (_, _) => vm.Documento.ContenutoCambiato -= Gestore;
        return combo;
    }
}

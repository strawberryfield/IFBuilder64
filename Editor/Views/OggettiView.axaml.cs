//-----------------------------------------------------------------------
// <copyright file="Editor/Views/OggettiView.axaml.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Scheda Oggetti: barra con azioni e filtri, e griglia degli oggetti; il DataContext è <see cref="OggettiViewModel"/>.</summary>
public partial class OggettiView : UserControl
{
    private const int ColonnaTipo = 1, ColonnaPosizione = 2;

    /// <summary>Crea la vista.</summary>
    public OggettiView()
    {
        InitializeComponent();
        ((DataGridTemplateColumn)Griglia.Columns[ColonnaTipo]).CellTemplate = new FuncDataTemplate<OggettoViewModel>((o, _) => o is null ? null : CreaCellaTipo(o));
        ((DataGridTemplateColumn)Griglia.Columns[ColonnaPosizione]).CellTemplate = new FuncDataTemplate<OggettoViewModel>((o, _) => o is null ? null : CreaCellaPosizione(o));
    }

    /// <summary>Menu a tendina Statico/Mobile della riga.</summary>
    private Control? CreaCellaTipo(OggettoViewModel o)
    {
        if (DataContext is not OggettiViewModel vm) return null;
        var combo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { Testi.Oggetti_Tipo_Statico, Testi.Oggetti_Tipo_Mobile }
        };
        var aggiornando = false;

        void Aggiorna()
        {
            aggiornando = true;
            combo.SelectedIndex = o.Statico == true ? 0 : 1;
            aggiornando = false;
        }

        combo.SelectionChanged += (_, _) =>
        {
            if (aggiornando || combo.SelectedIndex < 0) return;
            vm.ImpostaTipo(o, combo.SelectedIndex == 0);
        };
        Aggiorna();
        SeguiDocumento(combo, vm, Aggiorna);
        return combo;
    }

    /// <summary>Menu a tendina della posizione della riga (stanze, giocatore, contenitori).</summary>
    private Control? CreaCellaPosizione(OggettoViewModel o)
    {
        if (DataContext is not OggettiViewModel vm) return null;
        var combo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<VocePosizione>((v, _) => new TextBlock { Text = v?.Etichetta })
        };
        var aggiornando = false;
        IReadOnlyList<VocePosizione> voci = [];

        void Aggiorna()
        {
            aggiornando = true;
            var nuove = vm.PosizioniPer(o);
            if (!nuove.SequenceEqual(voci))
            {
                voci = nuove;
                combo.ItemsSource = voci;
            }
            var corrente = vm.VoceCorrente(o);
            combo.SelectedItem = corrente is null ? null : voci.FirstOrDefault(v => v == corrente);
            aggiornando = false;
        }

        combo.SelectionChanged += (_, _) =>
        {
            // Il null arriva quando cambia l'elenco: non è una scelta dell'utente.
            if (aggiornando || combo.SelectedItem is not VocePosizione scelta) return;
            vm.ImpostaPosizione(o, scelta);
        };
        Aggiorna();
        SeguiDocumento(combo, vm, Aggiorna);
        return combo;
    }

    /// <summary>Richiama <paramref name="aggiorna"/> a ogni modifica del documento finché il controllo è nell'albero visuale.</summary>
    private static void SeguiDocumento(Control controllo, OggettiViewModel vm, Action aggiorna)
    {
        void Gestore(object? sender, EventArgs e) => aggiorna();
        controllo.AttachedToVisualTree += (_, _) =>
        {
            vm.Documento.ContenutoCambiato -= Gestore;
            vm.Documento.ContenutoCambiato += Gestore;
            aggiorna();
        };
        controllo.DetachedFromVisualTree += (_, _) => vm.Documento.ContenutoCambiato -= Gestore;
    }
}

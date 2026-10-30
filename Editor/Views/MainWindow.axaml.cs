//-----------------------------------------------------------------------
// <copyright file="Editor/Views/MainWindow.axaml.cs" company="Casasoft">
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

using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>
/// Finestra principale dell'editor: menu, schede, pannello proprietà e pannello avvisi/output.
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;
    private bool _chiusuraConfermata;

    /// <summary>Crea la finestra; il ViewModel va assegnato a <see cref="StyledElement.DataContext"/>.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += Finestra_Closing;
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_vm is not null) _vm.Recenti.CollectionChanged -= Recenti_CollectionChanged;
        _vm = DataContext as MainWindowViewModel;
        if (_vm is not null) _vm.Recenti.CollectionChanged += Recenti_CollectionChanged;
        AggiornaLingue();
    }

    /// <summary>Costruisce il sottomenu delle lingue dell'interfaccia, con quella corrente spuntata.</summary>
    private void AggiornaLingue()
    {
        MenuLingue.Items.Clear();
        if (_vm is null) return;
        foreach (var lingua in _vm.LingueDisponibili)
            MenuLingue.Items.Add(new MenuItem
            {
                Header = lingua.Nome,
                ToggleType = MenuItemToggleType.Radio,
                GroupName = "LinguaInterfaccia",
                IsChecked = string.Equals(lingua.Codice, _vm.LinguaCorrente, StringComparison.OrdinalIgnoreCase),
                Command = _vm.CambiaLinguaCommand,
                CommandParameter = lingua.Codice
            });
    }

    private void Recenti_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => AggiornaRecenti();

    /// <summary>Ricostruisce il sottomenu dei file recenti.</summary>
    private void AggiornaRecenti()
    {
        MenuRecenti.Items.Clear();
        if (_vm is not null)
        {
            foreach (var percorso in _vm.Recenti)
                MenuRecenti.Items.Add(new MenuItem
                {
                    Header = percorso,
                    Command = _vm.ApriRecenteCommand,
                    CommandParameter = percorso
                });
        }
        MenuRecenti.IsEnabled = MenuRecenti.Items.Count > 0;
    }

    private void Esci_Click(object? sender, RoutedEventArgs e) => Close();

    private void ListaAvvisi_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_vm is not null && ListaAvvisi.SelectedItem is Avviso avviso)
            _vm.VaiAdAvvisoCommand.Execute(avviso);
    }

    /// <summary>Con modifiche non salvate annulla la chiusura, chiede conferma e poi richiude.</summary>
    private async void Finestra_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_chiusuraConfermata || _vm is null) return;
        var puoChiudere = _vm.PuoChiudereAsync();
        if (puoChiudere.IsCompleted)
        {
            e.Cancel = !puoChiudere.Result;
            return;
        }
        e.Cancel = true;
        if (await puoChiudere)
        {
            _chiusuraConfermata = true;
            Close();
        }
    }
}

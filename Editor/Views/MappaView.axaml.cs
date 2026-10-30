//-----------------------------------------------------------------------
// <copyright file="Editor/Views/MappaView.axaml.cs" company="Casasoft">
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
using Avalonia.Interactivity;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Scheda Mappa: barra degli strumenti e <see cref="Controlli.MappaControl"/>.</summary>
public partial class MappaView : UserControl
{
    /// <summary>Crea la vista; il DataContext è un <see cref="MappaViewModel"/>.</summary>
    public MappaView()
    {
        InitializeComponent();
    }

    private void LivelloSopra_Click(object? sender, RoutedEventArgs e) =>
        (DataContext as MappaViewModel)?.AggiungiLivello(sopra: true);

    private void LivelloSotto_Click(object? sender, RoutedEventArgs e) =>
        (DataContext as MappaViewModel)?.AggiungiLivello(sopra: false);
}

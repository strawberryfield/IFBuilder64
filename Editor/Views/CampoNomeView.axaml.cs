//-----------------------------------------------------------------------
// <copyright file="Editor/Views/CampoNomeView.axaml.cs" company="Casasoft">
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
using Avalonia.Input;
using Avalonia.Interactivity;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Campo del nome di un elemento, con conferma e annullamento da tastiera.</summary>
public partial class CampoNomeView : UserControl
{
    /// <summary>Crea la vista; il DataContext è un <see cref="CampoNomeViewModel"/>.</summary>
    public CampoNomeView()
    {
        InitializeComponent();
    }

    private void Campo_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CampoNomeViewModel vm) return;
        if (e.Key == Key.Enter)
        {
            vm.Conferma();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.Annulla();
            e.Handled = true;
        }
    }

    private void Campo_LostFocus(object? sender, RoutedEventArgs e) =>
        (DataContext as CampoNomeViewModel)?.Conferma();
}

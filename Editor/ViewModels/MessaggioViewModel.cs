//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/MessaggioViewModel.cs" company="Casasoft">
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

using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Messaggio (standard o personalizzato) o direzione: testo con posizione nell'elenco.</summary>
public partial class MessaggioViewModel : ObservableObject
{
    /// <summary>Posizione nell'elenco di appartenenza.</summary>
    [ObservableProperty] private int _indice;

    [ObservableProperty] private string _testo = string.Empty;

    /// <summary>Testo originale del template, se noto (non fa parte del JSON).</summary>
    [ObservableProperty] private string? _testoTemplate;
}

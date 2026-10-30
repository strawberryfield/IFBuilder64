//-----------------------------------------------------------------------
// <copyright file="Editor/Views/PannelloPorta.axaml.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Pannello proprietà di una porta; il DataContext è <see cref="ViewModels.PannelloPortaViewModel"/>.</summary>
public partial class PannelloPorta : UserControl
{
    /// <summary>Crea la vista.</summary>
    public PannelloPorta()
    {
        InitializeComponent();
    }
}

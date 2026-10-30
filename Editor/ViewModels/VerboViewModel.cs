//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/VerboViewModel.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Verbo del gioco: comando e codice azione del motore.</summary>
public partial class VerboViewModel : ObservableObject
{
    [ObservableProperty] private string _comando = string.Empty;
    [ObservableProperty] private byte _azione;

    /// <summary>Crea il ViewModel a partire dal modello JSON.</summary>
    public static VerboViewModel Da(Verb v) => new() { Comando = v.command, Azione = v.action };

    /// <summary>Converte nel modello JSON.</summary>
    public Verb ToVerb() => new() { command = Comando, action = Azione };
}

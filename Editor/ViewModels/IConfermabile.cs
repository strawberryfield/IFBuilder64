//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/IConfermabile.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>
/// Pannello con modifiche digitate ma non ancora confermate (ad esempio il nome in <see cref="CampoNomeViewModel"/>).
/// </summary>
public interface IConfermabile
{
    /// <summary>
    /// Conferma le modifiche in sospeso prima di salvare, compilare o chiudere.
    /// Se la conferma non riesce l'errore resta visibile nel pannello e l'operazione prosegue.
    /// </summary>
    void ConfermaModifiche();
}

//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/Avviso.cs" company="Casasoft">
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


namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>Gravità di un problema rilevato dal validatore.</summary>
public enum Gravita
{
    /// <summary>Blocca la compilazione.</summary>
    Errore,

    /// <summary>Non blocca la compilazione.</summary>
    Avviso
}

/// <summary>Problema rilevato dal validatore.</summary>
/// <param name="Gravita">Errore o avviso.</param>
/// <param name="Messaggio">Descrizione del problema, nella lingua corrente.</param>
/// <param name="Elemento">ViewModel interessato (stanza, oggetto, porta, verbo) o il documento per le regole globali.</param>
/// <param name="Correzione">Azione che risolve il problema, se esiste una correzione automatica.</param>
public record Avviso(Gravita Gravita, string Messaggio, object? Elemento, Action? Correzione = null);

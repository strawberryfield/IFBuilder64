//-----------------------------------------------------------------------
// <copyright file="Builder/BuildResult.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Builder;

/// <summary>
/// Esito di una compilazione eseguita da <see cref="GameBuilder"/>.
/// </summary>
public class BuildResult
{
    /// <summary>
    /// Inizializza un nuovo esito di compilazione.
    /// </summary>
    /// <param name="successo">True se la compilazione è terminata senza errori.</param>
    /// <param name="fileGenerati">Percorsi dei file scritti.</param>
    /// <param name="log">Messaggi prodotti durante la compilazione.</param>
    public BuildResult(bool successo, IReadOnlyList<string> fileGenerati, IReadOnlyList<string> log)
    {
        Successo = successo;
        FileGenerati = fileGenerati;
        Log = log;
    }

    /// <summary>True se la compilazione è terminata senza errori.</summary>
    public bool Successo { get; }

    /// <summary>Percorsi dei file scritti.</summary>
    public IReadOnlyList<string> FileGenerati { get; }

    /// <summary>Messaggi prodotti durante la compilazione (in caso di errore, il messaggio dell'eccezione).</summary>
    public IReadOnlyList<string> Log { get; }
}

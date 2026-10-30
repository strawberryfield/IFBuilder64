//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/ServizioCompilazione.cs" company="Casasoft">
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


using Casasoft.IFBuilder64.Builder;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>Compila il documento con il Builder, senza bloccare l'interfaccia (spec. 6.4).</summary>
public class ServizioCompilazione
{
    /// <summary>Base dei nomi dei file generati: stessa regola della CLI (percorso senza estensione).</summary>
    public static string OutputPredefinito(string percorsoJson) => Path.ChangeExtension(percorsoJson, "");

    /// <summary>
    /// Compila il documento. Gli errori del Builder sono nel risultato, non eccezioni.
    /// </summary>
    /// <param name="doc">Documento da compilare; non viene modificato.</param>
    /// <param name="outputBase">Percorso base dei file generati.</param>
    public Task<BuildResult> CompilaAsync(DocumentoViewModel doc, string outputBase)
    {
        // I ViewModel sono oggetti dell'interfaccia: la conversione resta sul thread chiamante.
        var game = doc.ToGame();
        return Task.Run(() => new GameBuilder().Build(game, outputBase));
    }
}

//-----------------------------------------------------------------------
// <copyright file="Confronto.cs" company="Casasoft">
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


using System.Text;
using System.Text.RegularExpressions;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Confronto dei file generati dal Builder con i riferimenti.</summary>
internal static class Confronto
{
    /// <summary>
    /// Byte del file; per *ab.seq azzera la quinta riga (data, separatore '\r'),
    /// per *iFiction.xml svuota <releasedate> e <compilerversion>.
    /// </summary>
    public static string Normalizza(string percorso)
    {
        var byteFile = File.ReadAllBytes(percorso);
        var nome = Path.GetFileName(percorso);
        if (nome.EndsWith("ab.seq", StringComparison.Ordinal))
        {
            var righe = Encoding.Latin1.GetString(byteFile).Split('\r');
            righe[4] = "";
            return string.Join('\r', righe);
        }
        if (nome.EndsWith("iFiction.xml", StringComparison.Ordinal))
        {
            var testo = Encoding.UTF8.GetString(byteFile);
            // Elementi esclusi perché dipendono dalla data o dalla build:
            // releasedate, originated e serial usano DateTime.Now al momento della compilazione;
            // compilerversion e generatorversion contengono versione e data di build dell'assembly Builder.
            foreach (var elemento in new[] { "releasedate", "originated", "serial", "compilerversion", "generatorversion" })
            {
                testo = Regex.Replace(testo, $"(<{elemento}>)[^<]*(</{elemento}>)", "$1$2");
            }
            return testo;
        }
        return Convert.ToHexString(byteFile);
    }
}

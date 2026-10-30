//-----------------------------------------------------------------------
// <copyright file="Editor/Program.cs" company="Casasoft">
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

using Avalonia;
using Casasoft.IFBuilder64.Editor.Servizi;
using System.Globalization;

namespace Casasoft.IFBuilder64.Editor;

/// <summary>
/// Punto di ingresso dell'editor grafico.
/// </summary>
internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var impostazioni = ImpostazioniUtente.Carica();
        if (!string.IsNullOrEmpty(impostazioni.Lingua))
        {
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(impostazioni.Lingua);
            }
            catch (CultureNotFoundException)
            {
                // lingua non valida nelle impostazioni: si segue quella di sistema
            }
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Configura l'applicazione Avalonia.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

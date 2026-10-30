//-----------------------------------------------------------------------
// <copyright file="Tests/AppTest.cs" company="Casasoft">
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
using Avalonia.Headless;
using Casasoft.IFBuilder64.Editor;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Applicazione dell'editor configurata per i test di interfaccia senza schermo.</summary>
public class AppTest
{
    /// <summary>Usato da <c>Avalonia.Headless.XUnit</c> per avviare l'applicazione.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

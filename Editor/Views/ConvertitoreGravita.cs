//-----------------------------------------------------------------------
// <copyright file="Editor/Views/ConvertitoreGravita.cs" company="Casasoft">
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

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Converte la <see cref="Gravita"/> di un avviso nel testo localizzato o nel colore.</summary>
public sealed class ConvertitoreGravita : IValueConverter
{
    private readonly bool _colore;

    private ConvertitoreGravita(bool colore) => _colore = colore;

    /// <summary>Testo localizzato ("Errore", "Avviso").</summary>
    public static ConvertitoreGravita Testo { get; } = new(false);

    /// <summary>Colore: rosso per gli errori, arancio per gli avvisi.</summary>
    public static ConvertitoreGravita Colore { get; } = new(true);

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var errore = value is Gravita.Errore;
        if (_colore) return errore ? Brushes.Red : Brushes.DarkOrange;
        return errore ? Testi.Gravita_Errore : Testi.Gravita_Avviso;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

//-----------------------------------------------------------------------
// <copyright file="Editor/Views/ConvertitoriGioco.cs" company="Casasoft">
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

using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Casasoft.IFBuilder64.Editor.Risorse;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Convertitori della scheda Gioco.</summary>
public static class ConvertitoriGioco
{
    /// <summary>Byte della scheda come valore decimale di un NumericUpDown; un valore vuoto non scrive nulla.</summary>
    public static readonly IValueConverter ByteDecimale = new ConvertitoreNumerico<byte>(byte.MaxValue);

    /// <summary>Intero facoltativo come valore decimale di un NumericUpDown; un valore vuoto diventa null.</summary>
    public static readonly IValueConverter IntFacoltativo = new ConvertitoreFacoltativo();

    /// <summary>
    /// Testo con a capo C64: nel TextBox il ritorno carrello (CR) è mostrato come a capo (LF) e un LF
    /// viene riscritto come CR (a capo del C64).
    /// </summary>
    public static readonly IValueConverter ACapo = new ConvertitoreACapo();

    /// <summary>Indice di direzione (0-5) nel suo nome localizzato.</summary>
    public static readonly IValueConverter NomeDirezione = new ConvertitoreDirezione();

    private sealed class ConvertitoreNumerico<T>(T massimo) : IValueConverter where T : struct, IConvertible
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is T t ? System.Convert.ToDecimal(t) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not decimal d) return BindingOperations.DoNothing;
            var limitato = Math.Clamp(Math.Round(d, MidpointRounding.AwayFromZero), 0, System.Convert.ToDecimal(massimo));
            return System.Convert.ChangeType(limitato, typeof(T), CultureInfo.InvariantCulture);
        }
    }

    private sealed class ConvertitoreFacoltativo : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int i ? System.Convert.ToDecimal(i) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is decimal d ? (int?)Math.Clamp(Math.Round(d, MidpointRounding.AwayFromZero), 0, int.MaxValue) : null;
    }

    private sealed class ConvertitoreACapo : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            (value as string)?.Replace("\r\n", "\n").Replace('\r', '\n');

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            (value as string)?.Replace("\r\n", "\r").Replace('\n', '\r');
    }

    private sealed class ConvertitoreDirezione : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int i ? Testi.ResourceManager.GetString($"Direzione_{i}", Testi.Culture) ?? $"Direzione_{i}" : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            BindingOperations.DoNothing;
    }
}

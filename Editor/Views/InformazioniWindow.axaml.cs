//-----------------------------------------------------------------------
// <copyright file="Editor/Views/InformazioniWindow.axaml.cs" company="Casasoft">
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

using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Casasoft.IFBuilder64.Editor.Risorse;

namespace Casasoft.IFBuilder64.Editor.Views;

/// <summary>Finestra Informazioni: prodotto, versione, licenza e avvisi di terze parti.</summary>
public partial class InformazioniWindow : Window
{
    /// <summary>Nome del file con gli avvisi di terze parti, accanto all'eseguibile.</summary>
    public const string NomeFileTerzeParti = "THIRD-PARTY-NOTICES.txt";

    /// <summary>Crea la finestra.</summary>
    public InformazioniWindow()
    {
        InitializeComponent();
        TestoVersione.Text = string.Format(Testi.Informazioni_Versione, VersioneAssembly());
        var avvisi = LeggiAvvisi();
        SezioneTerzeParti.IsVisible = avvisi is not null;
        TestoTerzeParti.IsVisible = avvisi is not null;
        TestoTerzeParti.Text = avvisi;
    }

    /// <summary>Versione informativa dell'assembly (senza i metadati dopo il +), oppure la versione di file.</summary>
    public static string VersioneAssembly()
    {
        var assembly = typeof(InformazioniWindow).Assembly;
        var informativa = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informativa)) return informativa.Split('+')[0];
        return FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion ?? "?";
    }

    /// <summary>Contenuto di THIRD-PARTY-NOTICES.txt, o null se il file non esiste o non è leggibile.</summary>
    public static string? LeggiAvvisi()
    {
        try
        {
            var percorso = Path.Combine(AppContext.BaseDirectory, NomeFileTerzeParti);
            return File.Exists(percorso) ? File.ReadAllText(percorso) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void Chiudi_Click(object? sender, RoutedEventArgs e) => Close();
}

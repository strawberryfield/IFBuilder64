//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/ImpostazioniUtente.cs" company="Casasoft">
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

using System.Text.Encodings.Web;
using System.Text.Json;

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>
/// Impostazioni utente dell'editor, salvate in JSON nella cartella di configurazione
/// dell'utente (spec. 4.4).
/// </summary>
public class ImpostazioniUtente
{
    /// <summary>Numero massimo di file recenti ricordati.</summary>
    public const int MassimoRecenti = 10;

    private static readonly JsonSerializerOptions Opzioni = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Lingua dell'interfaccia (nome di cultura); null per seguire quella di sistema.</summary>
    public string? Lingua { get; set; }

    /// <summary>File aperti di recente, dal più recente.</summary>
    public List<string> FileRecenti { get; set; } = [];

    /// <summary>Ultima cartella usata per l'output della compilazione.</summary>
    public string? UltimaCartellaOutput { get; set; }

    /// <summary>Percorso predefinito: ApplicationData/IFBuilder64/editor.json.</summary>
    public static string PercorsoPredefinito =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IFBuilder64", "editor.json");

    /// <summary>
    /// Carica le impostazioni. File mancante, illeggibile o corrotto
    /// producono un'istanza con i valori predefiniti.
    /// </summary>
    public static ImpostazioniUtente Carica(string? percorso = null)
    {
        try
        {
            var json = File.ReadAllText(percorso ?? PercorsoPredefinito);
            var impostazioni = JsonSerializer.Deserialize<ImpostazioniUtente>(json, Opzioni) ?? new ImpostazioniUtente();
            impostazioni.FileRecenti ??= [];
            return impostazioni;
        }
        catch (Exception)
        {
            return new ImpostazioniUtente();
        }
    }

    /// <summary>Salva le impostazioni, creando la cartella se manca.</summary>
    public void Salva(string? percorso = null)
    {
        percorso ??= PercorsoPredefinito;
        var cartella = Path.GetDirectoryName(percorso);
        if (!string.IsNullOrEmpty(cartella)) Directory.CreateDirectory(cartella);
        File.WriteAllText(percorso, JsonSerializer.Serialize(this, Opzioni));
    }

    /// <summary>Aggiunge un file in testa ai recenti, senza duplicati e al massimo 10 voci.</summary>
    public void AggiungiRecente(string percorso)
    {
        FileRecenti.RemoveAll(f => string.Equals(f, percorso, StringComparison.OrdinalIgnoreCase));
        FileRecenti.Insert(0, percorso);
        if (FileRecenti.Count > MassimoRecenti) FileRecenti.RemoveRange(MassimoRecenti, FileRecenti.Count - MassimoRecenti);
    }
}

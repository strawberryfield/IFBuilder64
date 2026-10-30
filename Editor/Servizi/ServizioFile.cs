//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/ServizioFile.cs" company="Casasoft">
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
using System.Text.Json.Serialization;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>Errore nell'apertura di un file di gioco (JSON non valido, file illeggibile o vuoto).</summary>
public class ErroreApertura(string messaggio) : Exception(messaggio);

/// <summary>Creazione, apertura e salvataggio dei file di gioco JSON.</summary>
public static class ServizioFile
{
    /// <summary>Opzioni di serializzazione: indentato, senza campi nulli, accenti in chiaro.</summary>
    public static JsonSerializerOptions Opzioni { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Crea un documento nuovo dal template della lingua indicata.</summary>
    public static DocumentoViewModel Nuovo(TemplateLanguage lingua)
    {
        var doc = DocumentoViewModel.DaGame(new Game(lingua));
        RiempiTestoTemplate(doc, lingua);
        doc.Modificato = false;
        return doc;
    }

    /// <summary>Apre un file di gioco; in caso di problemi solleva <see cref="ErroreApertura"/>.</summary>
    public static DocumentoViewModel Apri(string percorso)
    {
        Game? game;
        try
        {
            game = JsonSerializer.Deserialize<Game>(File.ReadAllText(percorso), Opzioni);
        }
        catch (JsonException ex)
        {
            var posizione = ex.LineNumber is { } riga
                ? $" (riga {riga}, posizione {ex.BytePositionInLine?.ToString() ?? "?"}; line {riga})"
                : string.Empty;
            throw new ErroreApertura(ex.Message + posizione);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ErroreApertura(ex.Message);
        }
        if (game is null) throw new ErroreApertura("Il file non contiene un gioco valido.");

        var doc = DocumentoViewModel.DaGame(game);
        doc.Percorso = percorso;
        if (RilevaLingua(game) is { } lingua) RiempiTestoTemplate(doc, lingua);
        // Dal Task 8: DisposizioneAutomatica.Disponi(doc, tutte: false).
        doc.Modificato = false;
        return doc;
    }

    /// <summary>Salva il documento; aggiorna <see cref="DocumentoViewModel.Percorso"/> e azzera la modifica.</summary>
    public static void Salva(DocumentoViewModel doc, string percorso)
    {
        File.WriteAllText(percorso, JsonSerializer.Serialize(doc.ToGame(), Opzioni));
        doc.Percorso = percorso;
        doc.Modificato = false;
    }

    /// <summary>Deduce la lingua del template da <c>ifiction.language</c> o dalle direzioni; null se ignota.</summary>
    public static TemplateLanguage? RilevaLingua(Game g)
    {
        var codice = g.ifiction?.language?.Trim();
        if (!string.IsNullOrEmpty(codice))
        {
            if (codice.StartsWith("it", StringComparison.OrdinalIgnoreCase)) return TemplateLanguage.Italiano;
            if (codice.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return TemplateLanguage.English;
        }
        foreach (var lingua in new[] { TemplateLanguage.Italiano, TemplateLanguage.English })
            if (g.directions.SequenceEqual(new Game(lingua).directions)) return lingua;
        return null;
    }

    /// <summary>Messaggi standard del template nella lingua indicata.</summary>
    public static IReadOnlyList<string> MessaggiTemplate(TemplateLanguage lingua) => new Game(lingua).messages;

    private static void RiempiTestoTemplate(DocumentoViewModel doc, TemplateLanguage lingua)
    {
        var template = MessaggiTemplate(lingua);
        foreach (var m in doc.MessaggiStandard)
            m.TestoTemplate = m.Indice < template.Count ? template[m.Indice] : null;
    }
}

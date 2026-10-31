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

using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Casasoft.IFBuilder64.Editor.Risorse;
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
            // LineNumber e BytePositionInLine sono a base 0: si mostrano a base 1.
            var modello = Testi.ResourceManager.GetString("ErroreApertura_Json", Testi.Culture) ?? "{0}, {1}: {2}";
            throw new ErroreApertura(string.Format(modello, (ex.LineNumber ?? 0) + 1, (ex.BytePositionInLine ?? 0) + 1, ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ErroreApertura(ex.Message);
        }
        if (game is null) throw new ErroreApertura(Testi.ErroreApertura_GiocoNonValido);

        // Un JSON valido può contenere collezioni null: si trattano come vuote
        // e sarà il validatore a segnalare ciò che manca.
        game.rooms ??= [];
        game.items ??= [];
        game.doors ??= [];
        game.verbs ??= [];
        game.messages ??= [];
        game.usermessages ??= [];
        game.directions ??= [];
        game.intro ??= string.Empty;
        game.initialroom ??= string.Empty;

        try
        {
            var doc = DocumentoViewModel.DaGame(game);
            doc.Percorso = percorso;
            if (RilevaLingua(game) is { } lingua) RiempiTestoTemplate(doc, lingua);
            DisposizioneAutomatica.Disponi(doc, tutte: false);
            doc.Modificato = false;
            return doc;
        }
        catch (Exception ex)
        {
            // Elementi null o incompleti nelle collezioni: il documento corrente non va toccato.
            throw new ErroreApertura($"{Testi.ErroreApertura_GiocoNonValido} {ex.Message}");
        }
    }

    /// <summary>
    /// Salva il documento; aggiorna <see cref="DocumentoViewModel.Percorso"/> e azzera la modifica.
    /// Scrive prima su <c>percorso.tmp</c> e poi lo sostituisce al file, così un errore
    /// di scrittura non lascia il file troncato.
    /// </summary>
    public static void Salva(DocumentoViewModel doc, string percorso)
    {
        var testo = JsonSerializer.Serialize(doc.ToGame(), Opzioni);
        var temporaneo = percorso + ".tmp";
        try
        {
            File.WriteAllText(temporaneo, testo);
            File.Move(temporaneo, percorso, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temporaneo);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // il file temporaneo non si può rimuovere: resta l'errore originale
            }
            throw;
        }
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
            if (DirezioniTemplate(lingua) is { } direzioni && g.directions.SequenceEqual(direzioni)) return lingua;
        return null;
    }

    private static readonly ConcurrentDictionary<TemplateLanguage, IReadOnlyList<string>> _direzioniTemplate = new();

    /// <summary>Direzioni del template, lette una sola volta per lingua; null se il template non si legge.</summary>
    private static IReadOnlyList<string>? DirezioniTemplate(TemplateLanguage lingua)
    {
        try
        {
            return _direzioniTemplate.GetOrAdd(lingua, l => new Game(l).directions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
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

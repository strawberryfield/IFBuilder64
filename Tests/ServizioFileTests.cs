//-----------------------------------------------------------------------
// <copyright file="Tests/ServizioFileTests.cs" company="Casasoft">
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
using System.Text.Json.Nodes;
using Casasoft.Commodore;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

[Collection("CartellaCorrente")]
public class ServizioFileTests
{
    private static string Esempio => Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json");

    private static string Temporaneo() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public void Apri_Salva_ConservaAccentiEControlliComeTag()
    {
        var uscita = Temporaneo();
        try
        {
            ServizioFile.Salva(ServizioFile.Apri(Esempio), uscita);
            var testo = File.ReadAllText(uscita);
            Assert.Contains("è", testo);
            Assert.DoesNotContain("\\u00E8", testo);
            Assert.Contains("[red]", testo);
            Assert.Contains("[blue]", testo);
            Assert.DoesNotContain("\\u001C", testo, StringComparison.OrdinalIgnoreCase);

            // l'originale, con i codici di controllo resi come tag, deve coincidere col salvato
            var originale = InTag(JsonNode.Parse(File.ReadAllText(Esempio))!);
            var salvato = JsonNode.Parse(testo)!;
            foreach (var stanza in salvato["rooms"]!.AsArray()) stanza!.AsObject().Remove("map");
            foreach (var oggetto in salvato["items"]!.AsArray()) oggetto!.AsObject().Remove("id");
            Assert.True(JsonNode.DeepEquals(originale, salvato));
        }
        finally { File.Delete(uscita); }
    }

    private static JsonNode InTag(JsonNode nodo)
    {
        switch (nodo)
        {
            case JsonObject o:
                foreach (var (chiave, valore) in o.ToList())
                    if (valore is not null) o[chiave] = InTag(valore.DeepClone());
                return o;
            case JsonArray a:
                for (int i = 0; i < a.Count; i++)
                    if (a[i] is { } valore) a[i] = InTag(valore.DeepClone());
                return a;
            case JsonValue v when v.TryGetValue<string>(out var s):
                return JsonValue.Create(Charset.ControlCodesToTags(s));
            default:
                return nodo;
        }
    }

    [Fact]
    public void CodiciControlloInTag_TestiConvertitiNomiIntatti()
    {
        var game = new Game
        {
            intro = "\x1cIntro\x90",
            rooms = [new Room { name = "sala", description = "La \x1cporta\x90 e \x95muro\x90." }],
            items = [new Item { name = "libro", description = "\x9cTitolo\x90", opendescription = "\u001Fblu\u0090", location = "sala" }],
            doors = [new Door { name = "porta", description = "\x12rvs\x92", denyopen = null }],
            messages = ["\u0005bianco"],
            usermessages = [],
            verbs = [],
            directions = []
        };
        ServizioFile.CodiciControlloInTag(game);
        Assert.Equal("[red]Intro[black]", game.intro);
        Assert.Equal("La [red]porta[black] e [brown]muro[black].", game.rooms[0].description);
        Assert.Equal("[purple]Titolo[black]", game.items[0].description);
        Assert.Equal("[blue]blu[black]", game.items[0].opendescription);
        Assert.Equal("[rvson]rvs[rvsoff]", game.doors[0].description);
        Assert.Null(game.doors[0].denyopen);
        Assert.Equal("[white]bianco", game.messages[0]);
    }

    [Fact]
    public void Charset_Tag_RiconvertitiInCodici()
    {
        Assert.Equal(new byte[] { 0x1C, (byte)'A', 0x90 }, Charset.PETSCIIBytes("[RED]a[black]", true));
        // i tag sconosciuti restano testo
        Assert.Equal(Charset.PETSCIIBytes("[", true).Concat(Charset.PETSCIIBytes("x]", true)), Charset.PETSCIIBytes("[x]", true));
    }

    [Fact]
    public void Apri_JsonNonValido_ErroreConPosizione()
    {
        var f = Temporaneo();
        // Il difetto e' alla riga 3 (la graffa chiusa al posto di un valore).
        File.WriteAllText(f, "{\n  \"rooms\": [\n  }");
        var precedente = Testi.Culture;
        try
        {
            Testi.Culture = new CultureInfo("it");
            var ex = Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f));
            Assert.StartsWith("Riga 3, posizione ", ex.Message);
        }
        finally
        {
            Testi.Culture = precedente;
            File.Delete(f);
        }
    }

    [Fact]
    public void Apri_FileNull_ErroreApertura()
    {
        var f = Temporaneo();
        File.WriteAllText(f, "null");
        try { Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f)); }
        finally { File.Delete(f); }
    }

    [Fact]
    public void Apri_FileMancante_ErroreApertura() =>
        Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(Temporaneo()));

    [Fact]
    public void Apri_RiempieTestoTemplate()
    {
        var doc = ServizioFile.Apri(Esempio);
        Assert.Equal(ServizioFile.MessaggiTemplate(TemplateLanguage.Italiano)[0], doc.MessaggiStandard[0].TestoTemplate);
    }

    [Fact]
    public void RilevaLingua_PrefissoSenzaMaiuscole()
    {
        Assert.Equal(TemplateLanguage.Italiano, ServizioFile.RilevaLingua(new Game { ifiction = new Casasoft.IFBuilder64.Common.Json.IFiction { title = "t", author = "a", language = "IT-it" } }));
        Assert.Equal(TemplateLanguage.English, ServizioFile.RilevaLingua(new Game { ifiction = new Casasoft.IFBuilder64.Common.Json.IFiction { title = "t", author = "a", language = "en" } }));
        Assert.Null(ServizioFile.RilevaLingua(new Game { directions = ["a", "b"] }));
    }

    [Fact]
    public void Apri_FileSenzaMappa_DisponeLeStanze()
    {
        var doc = ServizioFile.Apri(Esempio);
        Assert.All(doc.Stanze.Where(s => s.Nome != "player"), s => Assert.True(s.HaPosizione));
    }

    [Fact]
    public void Nuovo_Italiano_HaStanzaPlayerEModificatoFalse()
    {
        var doc = ServizioFile.Nuovo(TemplateLanguage.Italiano);
        Assert.Equal("player", doc.Stanze[0].Nome);
        Assert.False(doc.Modificato);
        Assert.Null(doc.Percorso);
        Assert.NotNull(doc.MessaggiStandard[0].TestoTemplate);
    }

    [Fact]
    public void Salva_AzzeraModificatoEImpostaPercorso()
    {
        var f = Temporaneo();
        try
        {
            var doc = ServizioFile.Nuovo(TemplateLanguage.English);
            doc.Intro = "ciao";
            Assert.True(doc.Modificato);
            ServizioFile.Salva(doc, f);
            Assert.False(doc.Modificato);
            Assert.Equal(f, doc.Percorso);
            Assert.True(File.Exists(f));
        }
        finally { File.Delete(f); }
    }

    /// <summary>
    /// Una collezione null nel JSON viene trattata come vuota: si apre un documento valido
    /// (sarà il validatore a segnalare ciò che manca) invece di un'eccezione non gestita.
    /// </summary>
    [Theory]
    [InlineData("rooms")]
    [InlineData("items")]
    [InlineData("doors")]
    [InlineData("verbs")]
    [InlineData("messages")]
    [InlineData("usermessages")]
    [InlineData("directions")]
    public void Apri_CollezioneNull_DocumentoConCollezioneVuota(string campo)
    {
        var json = JsonNode.Parse(File.ReadAllText(Esempio))!.AsObject();
        json[campo] = null;
        var f = Temporaneo();
        File.WriteAllText(f, json.ToJsonString());
        try
        {
            var game = ServizioFile.Apri(f).ToGame();
            System.Collections.ICollection collezione = campo switch
            {
                "rooms" => game.rooms,
                "items" => game.items,
                "doors" => game.doors,
                "verbs" => game.verbs,
                "messages" => game.messages,
                "usermessages" => game.usermessages,
                _ => game.directions
            };
            Assert.Empty(collezione);
        }
        finally { File.Delete(f); }
    }

    [Theory]
    [InlineData("{\"rooms\":[null]}")]
    [InlineData("{\"items\":[null]}")]
    [InlineData("{\"doors\":[null]}")]
    [InlineData("{\"verbs\":[null]}")]
    public void Apri_ElementoNull_ErroreApertura(string contenuto)
    {
        var f = Temporaneo();
        File.WriteAllText(f, contenuto);
        try { Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f)); }
        finally { File.Delete(f); }
    }

    [Fact]
    public void Apri_FileNull_MessaggioLocalizzato()
    {
        var f = Temporaneo();
        File.WriteAllText(f, "null");
        var precedente = Testi.Culture;
        try
        {
            Testi.Culture = new CultureInfo("it");
            Assert.Equal("Il file non contiene un gioco valido.", Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f)).Message);
            Testi.Culture = new CultureInfo("en");
            Assert.Equal("The file does not contain a valid game.", Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f)).Message);
        }
        finally
        {
            Testi.Culture = precedente;
            File.Delete(f);
        }
    }

    [Fact]
    public void Salva_SuFileEsistente_ScriveIlContenutoSenzaLasciareTmp()
    {
        var f = Temporaneo();
        File.WriteAllText(f, new string('x', 100_000));
        try
        {
            var doc = ServizioFile.Nuovo(TemplateLanguage.English);
            doc.Intro = "nuovo";
            ServizioFile.Salva(doc, f);
            Assert.Equal("nuovo", JsonNode.Parse(File.ReadAllText(f))!["intro"]!.GetValue<string>());
            Assert.False(File.Exists(f + ".tmp"));
        }
        finally { File.Delete(f); }
    }

    [Fact]
    public void Salva_DestinazioneNonScrivibile_EccezioneSenzaTmp()
    {
        // La destinazione è una cartella: il file temporaneo si scrive, lo spostamento fallisce.
        var cartella = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var doc = ServizioFile.Nuovo(TemplateLanguage.English);
            Assert.ThrowsAny<Exception>(() => ServizioFile.Salva(doc, cartella));
            Assert.False(File.Exists(cartella + ".tmp"));
            Assert.Null(doc.Percorso);
        }
        finally
        {
            Directory.Delete(cartella, true);
            File.Delete(cartella + ".tmp");
        }
    }
}

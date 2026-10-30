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

using System.Text.Json.Nodes;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

public class ServizioFileTests
{
    private static string Esempio => Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json");

    private static string Temporaneo() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public void Apri_Salva_ConservaAccentiEControlli()
    {
        var uscita = Temporaneo();
        try
        {
            ServizioFile.Salva(ServizioFile.Apri(Esempio), uscita);
            var testo = File.ReadAllText(uscita);
            Assert.Contains("è", testo);
            Assert.DoesNotContain("\\u00E8", testo);

            var originale = JsonNode.Parse(File.ReadAllText(Esempio))!;
            var salvato = JsonNode.Parse(testo)!;
            foreach (var stanza in salvato["rooms"]!.AsArray()) stanza!.AsObject().Remove("map");
            foreach (var oggetto in salvato["items"]!.AsArray()) oggetto!.AsObject().Remove("id");
            Assert.True(JsonNode.DeepEquals(originale, salvato));
        }
        finally { File.Delete(uscita); }
    }

    [Fact]
    public void Apri_JsonNonValido_ErroreConPosizione()
    {
        var f = Temporaneo();
        File.WriteAllText(f, "{ \"rooms\": [ }");
        try
        {
            var ex = Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f));
            Assert.Contains("line", ex.Message);
        }
        finally { File.Delete(f); }
    }

    [Fact]
    public void Apri_FileNullOMancante_ErroreApertura()
    {
        var f = Temporaneo();
        File.WriteAllText(f, "null");
        try { Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f)); }
        finally { File.Delete(f); }
        Assert.Throws<ErroreApertura>(() => ServizioFile.Apri(f));
    }

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

    [Fact(Skip = "Task 8")]
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
}

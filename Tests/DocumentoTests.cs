//-----------------------------------------------------------------------
// <copyright file="Tests/DocumentoTests.cs" company="Casasoft">
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

using System.Text.Json;
using System.Text.Json.Nodes;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>Test della mappatura Game e ViewModel del documento.</summary>
public class DocumentoTests
{
    private static bool JsonUguale(Game a, Game b) =>
        JsonNode.DeepEquals(JsonSerializer.SerializeToNode(a), JsonSerializer.SerializeToNode(b));

    private static Game Carica(string sorgente) => sorgente switch
    {
        "it" => new Game(TemplateLanguage.Italiano),
        "en" => new Game(TemplateLanguage.English),
        _ => JsonSerializer.Deserialize<Game>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json")))!
    };

    [Theory]
    [InlineData("it")]
    [InlineData("en")]
    [InlineData("esempio")]
    public void AndataERitorno_JsonEquivalente(string sorgente)
    {
        var g = Carica(sorgente);
        var ritorno = DocumentoViewModel.DaGame(g).ToGame();
        Assert.True(JsonUguale(g, ritorno));
    }

    [Fact]
    public void AndataERitorno_IFictionCompleta()
    {
        var g = Carica("it");
        g.ifiction = new Casasoft.IFBuilder64.Common.Json.IFiction
        {
            title = "t", author = "a", ifid = "X", headline = "h", genre = "g",
            firstpublished = 1985, version = 3, description = "d", language = "it", url = "u", email = "e"
        };
        var doc = DocumentoViewModel.DaGame(g);
        Assert.Equal(1985, doc.IFiction!.PrimaPubblicazione);
        Assert.Equal(3, doc.IFiction.Versione);
        Assert.True(JsonUguale(g, doc.ToGame()));
    }

    [Fact]
    public void IFictionNulla_RestaNulla()
    {
        var doc = DocumentoViewModel.DaGame(Carica("it"));
        Assert.Null(doc.IFiction);
        Assert.Null(doc.ToGame().ifiction);
    }

    [Fact]
    public void FlagNullRestanoNull()
    {
        var g = Carica("it");
        g.doors.Add(new Door { name = "d", description = "x" });
        var ritorno = DocumentoViewModel.DaGame(g).ToGame();
        Assert.Null(ritorno.items[0].openable);
        Assert.Null(ritorno.items[0].notportable);
        Assert.Null(ritorno.doors[0].open);
    }

    [Fact]
    public void OrdinePreservato()
    {
        var g = Carica("esempio");
        var ritorno = DocumentoViewModel.DaGame(g).ToGame();
        Assert.Equal(g.rooms.Select(r => r.name), ritorno.rooms.Select(r => r.name));
        Assert.Equal(g.items.Select(i => i.name), ritorno.items.Select(i => i.name));
        Assert.Equal(g.verbs.Select(v => v.command), ritorno.verbs.Select(v => v.command));
    }

    [Fact]
    public void StanzaPlayer_RestaInIndiceZero()
    {
        var doc = DocumentoViewModel.DaGame(Carica("esempio"));
        Assert.True(doc.Stanze[0].EGiocatore);
        Assert.Equal("player", doc.ToGame().rooms[0].name);
        Assert.Single(doc.Stanze, s => s.EGiocatore);
    }

    [Fact]
    public void StanzaSenzaMappa_DopoPosizionamento_SalvaLeCoordinate()
    {
        var doc = DocumentoViewModel.DaGame(Carica("it"));
        Assert.Null(doc.ToGame().rooms[0].map);
        doc.Stanze[0].HaPosizione = true;
        doc.Stanze[0].X = 3;
        Assert.Equal(3, doc.ToGame().rooms[0].map!.x);
    }

    [Fact]
    public void OggettoId_SegueLaRegolaDiItem()
    {
        var g = Carica("esempio");
        var doc = DocumentoViewModel.DaGame(g);
        Assert.Equal(g.items.Select(i => i.id), doc.Oggetti.Select(o => o.Id));
        Assert.Contains(doc.Oggetti, o => o.Id.Contains('@'));

        var o = new OggettoViewModel { Nome = "x", Posizione = "p" };
        Assert.Equal("x", o.Id);
        o.Statico = true;
        Assert.Equal("x@p", o.Id);
        o.Posizione = "";
        Assert.Equal("x", o.Id);
        Assert.Same(doc.Oggetti[0], doc.TrovaOggettoPerId(doc.Oggetti[0].Id));
    }

    [Fact]
    public void ModificaAnnidata_ImpostaModificato()
    {
        var doc = DocumentoViewModel.DaGame(Carica("it"));
        Assert.False(doc.Modificato);
        var eventi = 0;
        doc.ContenutoCambiato += (_, _) => eventi++;

        doc.Oggetti[0].Descrizione = "x";
        Assert.True(doc.Modificato);
        Assert.Equal(1, eventi);

        doc.Modificato = false;
        doc.Stanze.Add(new StanzaViewModel { Nome = "nuova" });
        Assert.True(doc.Modificato);

        // elemento aggiunto dopo DaGame
        doc.Modificato = false;
        doc.Stanze[^1].Descrizione = "y";
        Assert.True(doc.Modificato);

        // elemento rimosso: non notifica più
        var rimossa = doc.Stanze[^1];
        doc.Stanze.Remove(rimossa);
        doc.Modificato = false;
        rimossa.Descrizione = "z";
        Assert.False(doc.Modificato);

        doc.Intro = "intro";
        Assert.True(doc.Modificato);
    }

    [Fact]
    public void Percorso_NonImpostaModificato()
    {
        var doc = DocumentoViewModel.DaGame(Carica("it"));
        doc.Percorso = "c:\\a.json";
        Assert.False(doc.Modificato);
    }

    [Fact]
    public void NomeUnivoco_PrimoLibero() =>
        Assert.Equal("stanza2", new DocumentoViewModel().NomeUnivoco("stanza", ["stanza1", "stanza3"]));

    private static Item It(string name, string? id = null, bool? notportable = null, string? location = null, string? unlockitem = null) =>
        new() { name = name, description = "", id = id, notportable = notportable, location = location, unlockitem = unlockitem };

    private static Game GiocoCon(List<Item> items, List<Door>? doors = null)
    {
        var g = new Game(TemplateLanguage.Italiano);
        g.items = items;
        g.doors = doors ?? [];
        return g;
    }

    [Fact]
    public void Carica_IdUgualeAllaRegola_Automatico()
    {
        var o = DocumentoViewModel.DaGame(GiocoCon([It("chiave", "chiave")])).Oggetti[0];
        Assert.Null(o.IdPersonalizzato);
        Assert.Equal("chiave", o.Id);
    }

    [Fact]
    public void Carica_IdConSpazi_RipulitoEAutomatico()
    {
        var d = DocumentoViewModel.DaGame(GiocoCon([It("chiave", "chiave "), It("key", " k ")]));
        Assert.Null(d.Oggetti[0].IdPersonalizzato);
        Assert.Equal("k", d.Oggetti[1].IdPersonalizzato);
    }

    [Fact]
    public void Carica_IdDiverso_Personalizzato()
    {
        var d = DocumentoViewModel.DaGame(GiocoCon([It("key", "chiave")]));
        var o = d.Oggetti[0];
        Assert.Equal("chiave", o.IdPersonalizzato);
        Assert.Equal("chiave", o.Id);
        var i = d.ToGame().items[0];
        Assert.Equal("chiave", i.id);
        Assert.Equal("key", i.name);
    }

    [Fact]
    public void OggettoAutomatico_IdSegueIlNome()
    {
        var o = DocumentoViewModel.DaGame(GiocoCon([It("chiave")])).Oggetti[0];
        var notificate = new List<string?>();
        o.PropertyChanged += (_, e) => notificate.Add(e.PropertyName);
        o.Nome = "x";
        Assert.Equal("x", o.Id);
        Assert.Contains(nameof(OggettoViewModel.Id), notificate);
        Assert.Contains(nameof(OggettoViewModel.IdAutomatico), notificate);
    }

    [Fact]
    public void Carica_SbloccoPerNome_NormalizzatoInId()
    {
        var porta = new Door { name = "porta", description = "", unlockitem = "baule" };
        var d = DocumentoViewModel.DaGame(GiocoCon(
            [It("baule", notportable: true, location: "torre"), It("baule", notportable: true, location: "cantina"), It("forziere", unlockitem: "baule")],
            [porta]));
        Assert.Equal("baule@torre", d.Oggetti[2].OggettoSblocco);
        Assert.Equal("baule@torre", d.Porte[0].OggettoSblocco);
        Assert.False(d.Modificato);
    }

    [Fact]
    public void Carica_SbloccoPerNomeAmbiguo_StessaSceltaDelBuilder()
    {
        // Il Builder ordina gli oggetti (indice 0 fisso, poi statici e posizione) prima di risolvere gli sblocchi.
        var d = DocumentoViewModel.DaGame(GiocoCon(
            [It("chiave"), It("forziere", unlockitem: "leva"),
             It("leva", notportable: true, location: "torre"), It("leva", notportable: true, location: "cantina")]));
        Assert.Equal("leva@cantina", d.Oggetti[1].OggettoSblocco);
    }

    [Fact]
    public void Carica_SbloccoGiaId_Invariato()
    {
        var d = DocumentoViewModel.DaGame(GiocoCon([It("chiave"), It("forziere", unlockitem: "chiave")]));
        Assert.Equal("chiave", d.Oggetti[1].OggettoSblocco);
    }

    [Fact]
    public void EsempioApertoESalvato_BuildIdentico()
    {
        var g = Casasoft.IFBuilder64.Editor.Servizi.ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json")).ToGame();
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var r = new Casasoft.IFBuilder64.Builder.GameBuilder().Build(g, Path.Combine(dir, "esempio"));
            Assert.True(r.Successo, string.Join(Environment.NewLine, r.Log));
            var riferimenti = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Riferimenti"));
            Assert.Equal(9, riferimenti.Length);
            foreach (var rif in riferimenti)
            {
                Assert.Equal(Confronto.Normalizza(rif), Confronto.Normalizza(Path.Combine(dir, Path.GetFileName(rif))));
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="Tests/CatalogoAzioniTests.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using System.Globalization;

namespace Casasoft.IFBuilder64.Tests;

/// <summary>
/// Test del catalogo dei codici azione del motore.
/// I test che cambiano la cultura delle risorse non possono girare in parallelo.
/// </summary>
[Collection("CartellaCorrente")]
public class CatalogoAzioniTests
{
    [Fact]
    public void Catalogo_Ha26AzioniDa0a25() =>
        Assert.Equal(Enumerable.Range(0, 26).Select(i => (byte)i), CatalogoAzioni.Tutte.Select(a => a.Codice));

    [Fact]
    public void Catalogo_26NonEsiste() => Assert.False(CatalogoAzioni.Esiste(26));

    [Fact]
    public void Catalogo_25Esiste() => Assert.True(CatalogoAzioni.Esiste(25));

    [Fact]
    public void Catalogo_TrovaInesistente_RestituisceNull() => Assert.Null(CatalogoAzioni.Trova(26));

    [Fact]
    public void Catalogo_NomiLocalizzati()
    {
        var precedente = Testi.Culture;
        try
        {
            Testi.Culture = new CultureInfo("it");
            Assert.Equal("prendi", CatalogoAzioni.Trova(8)!.Nome);
            Assert.Equal("guarda/esamina", CatalogoAzioni.Trova(7)!.Nome);
            Testi.Culture = new CultureInfo("en");
            Assert.Equal("take", CatalogoAzioni.Trova(8)!.Nome);
            Assert.Equal("look/examine", CatalogoAzioni.Trova(7)!.Nome);
        }
        finally
        {
            Testi.Culture = precedente;
        }
    }

    [Fact]
    public void Azione_ToString_NomeECodice()
    {
        var precedente = Testi.Culture;
        try
        {
            Testi.Culture = new CultureInfo("en");
            Assert.Equal("take (8)", CatalogoAzioni.Trova(8)!.ToString());
        }
        finally
        {
            Testi.Culture = precedente;
        }
    }

    [Fact]
    public void Catalogo_TuttiINomiSonoValorizzati_InEntrambeLeLingue()
    {
        var precedente = Testi.Culture;
        try
        {
            foreach (var lingua in new[] { "it", "en" })
            {
                Testi.Culture = new CultureInfo(lingua);
                Assert.All(CatalogoAzioni.Tutte, a => Assert.False(string.IsNullOrWhiteSpace(a.Nome)));
            }
        }
        finally
        {
            Testi.Culture = precedente;
        }
    }
}

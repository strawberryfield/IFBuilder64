//-----------------------------------------------------------------------
// <copyright file="Tests/CampoNomeTests.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Json;

namespace Casasoft.IFBuilder64.Tests;

public class CampoNomeTests
{
    private static (DocumentoViewModel Doc, StanzaViewModel A, StanzaViewModel B, CampoNomeViewModel Campo) Prepara()
    {
        var d = ServizioFile.Nuovo(TemplateLanguage.Italiano);
        var a = new StanzaViewModel { Nome = "a", HaPosizione = true };
        var b = new StanzaViewModel { Nome = "b", HaPosizione = true, X = 1 };
        d.Stanze.Add(a);
        d.Stanze.Add(b);
        return (d, a, b, new CampoNomeViewModel(() => a.Nome, nuovo => d.RinominaStanza(a, nuovo)));
    }

    [Fact]
    public void Conferma_NomeValido_Rinomina()
    {
        var (_, a, _, campo) = Prepara();
        campo.Testo = "  atrio ";

        campo.Conferma();

        Assert.Equal("atrio", a.Nome);
        Assert.Equal("atrio", campo.Testo);
        Assert.Null(campo.Errore);
    }

    [Fact]
    public void Conferma_Duplicato_ImpostaErroreENonRinomina()
    {
        var (_, a, _, campo) = Prepara();
        campo.Testo = "b";

        campo.Conferma();

        Assert.Equal("a", a.Nome);
        Assert.Equal("b", campo.Testo);
        Assert.NotNull(campo.Errore);
        Assert.Contains("b", campo.Errore);
    }

    [Fact]
    public void Conferma_DopoErrore_Riuscita_CancellaErrore()
    {
        var (_, a, _, campo) = Prepara();
        campo.Testo = "b";
        campo.Conferma();

        campo.Testo = "c";
        campo.Conferma();

        Assert.Equal("c", a.Nome);
        Assert.Null(campo.Errore);
    }

    [Fact]
    public void Annulla_RipristinaTesto()
    {
        var (_, a, _, campo) = Prepara();
        campo.Testo = "b";
        campo.Conferma();
        Assert.NotNull(campo.Errore);

        campo.Annulla();

        Assert.Equal("a", campo.Testo);
        Assert.Null(campo.Errore);
        Assert.Equal("a", a.Nome);
    }
}

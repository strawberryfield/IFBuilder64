//-----------------------------------------------------------------------
// <copyright file="Tests/GiocoTests.cs" company="Casasoft">
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

using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;
using Casasoft.IFBuilder64.Editor.Views;

namespace Casasoft.IFBuilder64.Tests;

public sealed class GiocoTests : IDisposable
{
    private readonly string _cartella = Directory.CreateTempSubdirectory().FullName;
    private readonly DialoghiFinti _dialoghi = new();
    private readonly ImpostazioniUtente _impostazioni = new();

    // Niente DispatcherTimer reale: i [Fact] girano fuori dal thread UI di Avalonia,
    // in parallelo con i test headless delle altre classi.
    private readonly PianificatoreFinto _pianificatore = new();

    public void Dispose() => Directory.Delete(_cartella, true);

    private string PercorsoImpostazioni => Path.Combine(_cartella, "editor.json");

    private MainWindowViewModel CreaVm() => new(_dialoghi, _impostazioni, PercorsoImpostazioni, _pianificatore.Pianifica);

    private static DocumentoViewModel Esempio() =>
        ServizioFile.Apri(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"));

    [Fact]
    public void GeneraIfid_FormatoGuidMaiuscolo()
    {
        var f = new IFictionViewModel();

        f.GeneraIfidCommand.Execute(null);

        Assert.NotNull(f.Ifid);
        Assert.Matches(new Regex("^[0-9A-F]{8}(-[0-9A-F]{4}){3}-[0-9A-F]{12}$"), f.Ifid);
    }

    [Fact]
    public void GeneraIfid_ConIfidEsistente_ChiedeConferma()
    {
        var vm = CreaVm();
        vm.CreaIFictionCommand.Execute(null);
        var f = vm.Documento.IFiction!;
        f.Ifid = "ORIGINALE";

        _dialoghi.RispostaConferma = false;
        f.GeneraIfidCommand.Execute(null);
        Assert.Equal("ORIGINALE", f.Ifid);
        Assert.Contains(nameof(IDialoghi.ConfermaAsync), _dialoghi.Chiamate);

        _dialoghi.RispostaConferma = true;
        f.GeneraIfidCommand.Execute(null);
        Assert.NotEqual("ORIGINALE", f.Ifid);
    }

    [Fact]
    public void CambiaLingua_SalvaImpostazione()
    {
        var vm = CreaVm();

        vm.CambiaLinguaCommand.Execute("en");

        Assert.Equal("en", _impostazioni.Lingua);
        Assert.Equal("en", ImpostazioniUtente.Carica(PercorsoImpostazioni).Lingua);
        Assert.Contains(nameof(IDialoghi.InformaAsync), _dialoghi.Chiamate);
    }

    [Fact]
    public void LingueDisponibili_ContieneItEEn()
    {
        var vm = CreaVm();

        Assert.Contains(vm.LingueDisponibili, l => l.Codice == "en");
        Assert.Contains(vm.LingueDisponibili, l => l.Codice == "it");
        Assert.All(vm.LingueDisponibili, l => Assert.False(string.IsNullOrEmpty(l.Nome)));
    }

    [Fact]
    public void CreaIFiction_ProduceIFictionValida()
    {
        var vm = CreaVm();
        Assert.Null(vm.Documento.IFiction);

        vm.CreaIFictionCommand.Execute(null);

        var g = vm.Documento.ToGame();
        Assert.NotNull(g.ifiction);
        Assert.Equal(string.Empty, g.ifiction.title);
        Assert.Equal(string.Empty, g.ifiction.author);
        Assert.Equal(new Casasoft.IFBuilder64.Common.Json.IFiction { title = "", author = "" }.headline, g.ifiction.headline);
        Assert.Equal(new Casasoft.IFBuilder64.Common.Json.IFiction { title = "", author = "" }.genre, g.ifiction.genre);
    }

    [Fact]
    public void CaricoMassimo_RoundTripAttraversoToGame()
    {
        var d = Esempio();

        d.CaricoMassimo = 42;

        Assert.Equal(42, d.ToGame().maxload);
        Assert.Equal(42, DocumentoViewModel.DaGame(d.ToGame()).CaricoMassimo);
    }

    [AvaloniaFact]
    public async Task SchedaGioco_MostraTitoloIFiction()
    {
        var vm = CreaVm();
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        var percorso = Path.Combine(_cartella, "esempio.json");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Dati", "esempio.it.json"), percorso, true);
        _dialoghi.RispostaApriFile = percorso;
        await vm.ApriCommand.ExecuteAsync(null);
        vm.SchedaAttiva = MainWindowViewModel.SchedaGioco;
        Dispatcher.UIThread.RunJobs();

        var vista = Assert.Single(finestra.GetVisualDescendants().OfType<GiocoView>());
        var titoli = vista.GetVisualDescendants().OfType<TextBox>().Select(t => t.Text).ToList();
        Assert.NotNull(vm.Documento.IFiction);
        Assert.Contains(vm.Documento.IFiction.Titolo, titoli);
        finestra.Close();
    }

    [Fact]
    public void ConvertitoriGioco_CaricoVuotoNonScriveENormalizzaACapo()
    {
        var cultura = System.Globalization.CultureInfo.InvariantCulture;

        Assert.Same(Avalonia.Data.BindingOperations.DoNothing,
            ConvertitoriGioco.ByteDecimale.ConvertBack(null, typeof(byte), null, cultura));
        Assert.Equal((byte)255, ConvertitoriGioco.ByteDecimale.ConvertBack(999m, typeof(byte), null, cultura));
        Assert.Equal((byte)7, ConvertitoriGioco.ByteDecimale.ConvertBack(7.4m, typeof(byte), null, cultura));
        Assert.Null(ConvertitoriGioco.IntFacoltativo.ConvertBack(null, typeof(int?), null, cultura));
        Assert.Equal(1984, ConvertitoriGioco.IntFacoltativo.ConvertBack(1984m, typeof(int?), null, cultura));
        Assert.Equal("a\nb", ConvertitoriGioco.ACapo.Convert("a\rb", typeof(string), null, cultura));
        Assert.Equal("a\rb", ConvertitoriGioco.ACapo.ConvertBack("a\r\nb", typeof(string), null, cultura));
    }

    [Fact]
    public void IFictionVuota_CampiOpzionaliVuotiDiventanoNull()
    {
        var f = new IFictionViewModel { Titolo = "t", Autore = "a", Ifid = "X", Descrizione = "d", Lingua = "it", Url = "u", Email = "e" };

        f.Ifid = "";
        f.Descrizione = "   ";
        f.Lingua = "";
        f.Url = "";
        f.Email = "";
        var m = f.ToIFiction();

        Assert.Null(m.ifid);
        Assert.Null(m.description);
        Assert.Null(m.language);
        Assert.Null(m.url);
        Assert.Null(m.email);
    }

    [Fact]
    public void LinguaCorrente_NonDisponibile_RipiegaSuInglese()
    {
        _impostazioni.Lingua = "de";
        Assert.Equal("en", CreaVm().LinguaCorrente);

        _impostazioni.Lingua = "it";
        Assert.Equal("it", CreaVm().LinguaCorrente);
    }

    [AvaloniaFact]
    public void MenuRecenti_ConFileRecenti_SiPopolaAllAvvio()
    {
        _impostazioni.FileRecenti.Add(@"C:\giochi\uno.json");
        _impostazioni.FileRecenti.Add(@"C:\giochi\due.json");
        var vm = CreaVm();
        var finestra = new MainWindow { DataContext = vm };
        finestra.Show();
        Dispatcher.UIThread.RunJobs();

        var menu = finestra.FindControl<MenuItem>("MenuRecenti");
        Assert.NotNull(menu);
        Assert.Equal(2, menu.Items.Count);
        Assert.True(menu.IsEnabled);
        finestra.Close();
    }
}
//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/DocumentoViewModel.Riferimenti.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Riferimento a un elemento da parte di un altro elemento del documento.</summary>
/// <param name="Elemento">Elemento che contiene il riferimento (o il documento stesso per la stanza iniziale).</param>
/// <param name="Campo">Nome del campo che contiene il riferimento.</param>
public record Riferimento(object Elemento, string Campo);

public partial class DocumentoViewModel
{
    private static readonly Direzione[] _direzioni = Enum.GetValues<Direzione>();

    private static string Testo(string chiave) =>
        Testi.ResourceManager.GetString(chiave, Testi.Culture) ?? chiave;

    /// <summary>Rinomina una stanza aggiornando uscite, porte, oggetti e stanza iniziale. Restituisce null se riuscita, altrimenti l'errore.</summary>
    public string? RinominaStanza(StanzaViewModel s, string nuovo)
    {
        nuovo = nuovo?.Trim() ?? string.Empty;
        if (nuovo.Length == 0) return Testo("Errore_NomeVuoto");
        if (s.EGiocatore) return Testo("Errore_StanzaGiocatore");
        if (nuovo == s.Nome) return null;
        if (Stanze.Any(x => x != s && x.Nome == nuovo))
            return string.Format(Testi.Culture, Testo("Errore_NomeDuplicato"), nuovo);

        var vecchio = s.Nome;
        // Id (prima della rinomina) degli oggetti collocati nella stanza.
        var idVecchi = Oggetti.Where(o => o.Posizione == vecchio).Select(o => (o, id: o.Id)).ToList();

        s.Nome = nuovo;
        foreach (var d in Stanze.Cast<IConUscite>().Concat(Porte))
            foreach (var dir in _direzioni)
                if (d.Uscita(dir) == vecchio) d.ImpostaUscita(dir, nuovo);
        foreach (var o in Oggetti)
            if (o.Posizione == vecchio) o.Posizione = nuovo;
        if (StanzaIniziale == vecchio) StanzaIniziale = nuovo;
        foreach (var (o, id) in idVecchi)
            AggiornaDentroA(id, o.Id, o);
        return null;
    }

    /// <summary>Rinomina un oggetto aggiornando sblocchi e contenitori. Restituisce null se riuscita, altrimenti l'errore.</summary>
    public string? RinominaOggetto(OggettoViewModel o, string nuovo)
    {
        nuovo = nuovo?.Trim() ?? string.Empty;
        if (nuovo.Length == 0) return Testo("Errore_NomeVuoto");
        if (nuovo == o.Nome) return null;
        var idNuovo = o.Statico == true && !string.IsNullOrEmpty(o.Posizione) ? $"{nuovo}@{o.Posizione}" : nuovo;
        if (Oggetti.Any(x => x != o && x.Id == idNuovo))
            return string.Format(Testi.Culture, Testo("Errore_NomeDuplicato"), idNuovo);

        var vecchio = o.Nome;
        var idVecchio = o.Id;
        o.Nome = nuovo;
        foreach (var x in Oggetti)
            if (x.OggettoSblocco == vecchio) x.OggettoSblocco = nuovo;
        foreach (var p in Porte)
            if (p.OggettoSblocco == vecchio) p.OggettoSblocco = nuovo;
        AggiornaDentroA(idVecchio, o.Id, o);
        return null;
    }

    /// <summary>Rinomina una porta. Restituisce null se riuscita, altrimenti l'errore.</summary>
    public string? RinominaPorta(PortaViewModel p, string nuovo)
    {
        nuovo = nuovo?.Trim() ?? string.Empty;
        if (nuovo.Length == 0) return Testo("Errore_NomeVuoto");
        if (nuovo == p.Nome) return null;
        if (Porte.Any(x => x != p && x.Nome == nuovo))
            return string.Format(Testi.Culture, Testo("Errore_NomeDuplicato"), nuovo);
        p.Nome = nuovo;
        return null;
    }

    /// <summary>Sposta un oggetto; gli oggetti che lo contengono (<c>initem</c>) seguono il nuovo Id.</summary>
    public void SpostaOggetto(OggettoViewModel o, string? posizione, string? dentroA)
    {
        var idVecchio = o.Id;
        o.Posizione = posizione;
        o.DentroA = dentroA;
        AggiornaDentroA(idVecchio, o.Id, o);
    }

    /// <summary>Imposta il flag statico; l'Id cambia e gli <c>initem</c> che lo puntano vengono aggiornati.</summary>
    public void ImpostaStatico(OggettoViewModel o, bool statico)
    {
        var idVecchio = o.Id;
        o.Statico = statico;
        AggiornaDentroA(idVecchio, o.Id, o);
    }

    /// <summary>Elenca i riferimenti all'elemento (stanza, oggetto o porta).</summary>
    public IReadOnlyList<Riferimento> TrovaRiferimenti(object elemento)
    {
        var r = new List<Riferimento>();
        switch (elemento)
        {
            case StanzaViewModel s:
                foreach (var d in Stanze.Cast<IConUscite>().Concat(Porte))
                    foreach (var dir in _direzioni)
                        if (d.Uscita(dir) == s.Nome) r.Add(new Riferimento(d, dir.ToString()));
                foreach (var o in Oggetti)
                {
                    if (o.Posizione == s.Nome) r.Add(new Riferimento(o, nameof(OggettoViewModel.Posizione)));
                    if (o.DentroA is not null && o.DentroA.EndsWith("@" + s.Nome, StringComparison.Ordinal))
                        r.Add(new Riferimento(o, nameof(OggettoViewModel.DentroA)));
                }
                if (StanzaIniziale == s.Nome) r.Add(new Riferimento(this, nameof(StanzaIniziale)));
                break;
            case OggettoViewModel g:
                foreach (var o in Oggetti)
                {
                    if (o.OggettoSblocco == g.Nome) r.Add(new Riferimento(o, nameof(OggettoViewModel.OggettoSblocco)));
                    if (o.DentroA == g.Id) r.Add(new Riferimento(o, nameof(OggettoViewModel.DentroA)));
                }
                foreach (var p in Porte)
                    if (p.OggettoSblocco == g.Nome) r.Add(new Riferimento(p, nameof(PortaViewModel.OggettoSblocco)));
                break;
        }
        return r;
    }

    /// <summary>Svuota i riferimenti all'elemento e lo rimuove. La stanza "player" non è eliminabile.</summary>
    public void Elimina(object elemento)
    {
        if (elemento is StanzaViewModel { EGiocatore: true })
            throw new InvalidOperationException(Testo("Errore_StanzaGiocatore"));

        foreach (var r in TrovaRiferimenti(elemento))
        {
            switch (r.Elemento)
            {
                case DocumentoViewModel:
                    StanzaIniziale = string.Empty;
                    break;
                case IConUscite u when Enum.TryParse<Direzione>(r.Campo, out var dir):
                    u.ImpostaUscita(dir, null);
                    break;
                case OggettoViewModel o when r.Campo == nameof(OggettoViewModel.Posizione):
                    o.Posizione = null;
                    break;
                case OggettoViewModel o when r.Campo == nameof(OggettoViewModel.DentroA):
                    o.DentroA = null;
                    break;
                case OggettoViewModel o:
                    o.OggettoSblocco = null;
                    break;
                case PortaViewModel p:
                    p.OggettoSblocco = null;
                    break;
            }
        }

        switch (elemento)
        {
            case StanzaViewModel s: Stanze.Remove(s); break;
            case OggettoViewModel o: Oggetti.Remove(o); break;
            case PortaViewModel p: Porte.Remove(p); break;
        }
    }

    /// <summary>Sostituisce i <c>DentroA</c> uguali al vecchio Id con il nuovo (escluso l'oggetto stesso).</summary>
    private void AggiornaDentroA(string idVecchio, string idNuovo, OggettoViewModel escluso)
    {
        if (idVecchio == idNuovo) return;
        foreach (var x in Oggetti)
            if (x != escluso && x.DentroA == idVecchio) x.DentroA = idNuovo;
    }
}

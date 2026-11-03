//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/Collegamenti.cs" company="Casasoft">
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
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>Collegamento tra due stanze, attraverso un'uscita o una porta.</summary>
/// <param name="Da">Stanza di partenza.</param>
/// <param name="Direzione">Direzione in cui si esce da <paramref name="Da"/>.</param>
/// <param name="A">Stanza di arrivo.</param>
/// <param name="Porta">Porta attraversata, o null per un'uscita libera.</param>
public record Collegamento(StanzaViewModel Da, Direzione Direzione, StanzaViewModel A, PortaViewModel? Porta);

/// <summary>
/// Operazioni sui collegamenti tra stanze. Una porta con due lati (d1 -> R1, d2 -> R2) collega
/// R1 verso R2 in direzione d1 e R2 verso R1 in direzione d2: ogni lato indica la stanza da cui
/// si attraversa la porta andando in quella direzione.
/// </summary>
public static class Collegamenti
{
    private static readonly Direzione[] _direzioni = Enum.GetValues<Direzione>();

    /// <summary>
    /// Tutti i collegamenti del documento: uno per ogni uscita di stanza verso una stanza esistente
    /// (Porta = null) e due per ogni porta con esattamente due lati su stanze esistenti.
    /// Una porta aperta con le uscite dichiarate anche nelle stanze produce entrambi i collegamenti
    /// (uscita e porta): chi li disegna (ad esempio la mappa) deve eliminare i doppioni.
    /// </summary>
    public static IEnumerable<Collegamento> Tutti(DocumentoViewModel doc)
    {
        foreach (var s in doc.Stanze)
            foreach (var d in _direzioni)
                if (doc.TrovaStanza(s.Uscita(d)) is { } a)
                    yield return new Collegamento(s, d, a, null);

        foreach (var p in doc.Porte)
            if (Lati(doc, p) is { } l)
            {
                yield return new Collegamento(l.s1, l.d1, l.s2, p);
                yield return new Collegamento(l.s2, l.d2, l.s1, p);
            }
    }

    /// <summary>Scrive l'uscita da <paramref name="da"/> verso <paramref name="a"/> e, se richiesto, l'uscita opposta di ritorno.</summary>
    public static void Collega(DocumentoViewModel doc, StanzaViewModel da, Direzione dir, StanzaViewModel a, bool senzaRitorno)
    {
        da.ImpostaUscita(dir, a.Nome);
        if (!senzaRitorno) a.ImpostaUscita(dir.Opposta(), da.Nome);
    }

    /// <summary>Rimuove l'uscita di <paramref name="da"/> e l'uscita opposta della destinazione se punta a <paramref name="da"/>.</summary>
    public static void Elimina(DocumentoViewModel doc, StanzaViewModel da, Direzione dir)
    {
        var destinazione = da.Uscita(dir);
        da.ImpostaUscita(dir, null);
        if (doc.TrovaStanza(destinazione) is { } a && a.Uscita(dir.Opposta()) == da.Nome)
            a.ImpostaUscita(dir.Opposta(), null);
    }

    /// <summary>
    /// Sostituisce l'uscita con una porta chiusa a due lati (anche se l'uscita era a senso unico).
    /// Le uscite nelle stanze vengono rimosse, perché le scrive la porta quando si apre.
    /// </summary>
    public static PortaViewModel TrasformaInPorta(DocumentoViewModel doc, StanzaViewModel da, Direzione dir)
    {
        var destinazione = da.Uscita(dir);
        if (string.IsNullOrEmpty(destinazione))
            throw new InvalidOperationException(string.Format(Testi.Errore_NessunaUscita, da.Nome, dir.NomeLocalizzato()));

        var porta = new PortaViewModel
        {
            Nome = doc.NomeUnivoco(Testi.ResourceManager.GetString("PrefissoPorta", Testi.Culture) ?? "porta",
                doc.Porte.Select(p => p.Nome)),
            Aperta = false
        };
        porta.ImpostaUscita(dir, da.Nome);
        porta.ImpostaUscita(dir.Opposta(), destinazione);
        Elimina(doc, da, dir);
        doc.Porte.Add(porta);
        return porta;
    }

    /// <summary>Elimina la porta e scrive le due uscite libere tra le stanze che collegava.</summary>
    public static void TrasformaInPassaggio(DocumentoViewModel doc, PortaViewModel porta)
    {
        var l = Lati(doc, porta) ?? throw new InvalidOperationException(
            string.Format(Testi.Errore_PortaSenzaDueLati, porta.Nome));
        doc.Elimina(porta);
        l.s1.ImpostaUscita(l.d1, l.s2.Nome);
        l.s2.ImpostaUscita(l.d2, l.s1.Nome);
    }

    /// <summary>
    /// Imposta lo stato iniziale della porta. Aperta: scrive le uscite nelle due stanze (il motore
    /// non le scrive per una porta aperta all'inizio). Chiusa: le rimuove, ma solo se puntano all'altra stanza.
    /// </summary>
    public static void ImpostaAperta(DocumentoViewModel doc, PortaViewModel porta, bool aperta)
    {
        porta.Aperta = aperta;
        if (Lati(doc, porta) is not { } l) return;
        if (aperta)
        {
            l.s1.ImpostaUscita(l.d1, l.s2.Nome);
            l.s2.ImpostaUscita(l.d2, l.s1.Nome);
        }
        else
        {
            if (l.s1.Uscita(l.d1) == l.s2.Nome) l.s1.ImpostaUscita(l.d1, null);
            if (l.s2.Uscita(l.d2) == l.s1.Nome) l.s2.ImpostaUscita(l.d2, null);
        }
    }

    /// <summary>
    /// Vero se la porta è chiusa (Aperta diverso da true), ha due lati su stanze esistenti e uno
    /// dei lati è <paramref name="stanza"/> in direzione <paramref name="dir"/>: aprendosi scriverà
    /// quell'uscita, quindi l'uscita dichiarata nella stanza è un doppione. Una porta con un solo
    /// lato (ad esempio una porta finta che non si apre mai) non scrive uscite e non duplica nulla.
    /// </summary>
    public static bool DichiaraUscita(DocumentoViewModel doc, PortaViewModel porta, StanzaViewModel stanza, Direzione dir) =>
        porta.Aperta != true && porta.Uscita(dir) == stanza.Nome && Lati(doc, porta) is not null;

    /// <summary>
    /// Rimuove l'uscita della stanza se esiste una porta chiusa a due lati che la dichiara allo
    /// stesso modo (vedi <see cref="DichiaraUscita"/>); altrimenti non cambia nulla.
    /// </summary>
    public static void CorreggiUscitaDoppia(DocumentoViewModel doc, StanzaViewModel stanza, Direzione dir)
    {
        if (doc.Porte.Any(p => DichiaraUscita(doc, p, stanza, dir)))
            stanza.ImpostaUscita(dir, null);
    }

    /// <summary>Lati di una porta con esattamente due lati su stanze esistenti, altrimenti null.</summary>
    private static (StanzaViewModel s1, Direzione d1, StanzaViewModel s2, Direzione d2)? Lati(DocumentoViewModel doc, PortaViewModel p)
    {
        var lati = _direzioni.Where(d => !string.IsNullOrEmpty(p.Uscita(d))).ToList();
        if (lati.Count != 2) return null;
        var s1 = doc.TrovaStanza(p.Uscita(lati[0]));
        var s2 = doc.TrovaStanza(p.Uscita(lati[1]));
        return s1 is null || s2 is null ? null : (s1, lati[0], s2, lati[1]);
    }
}

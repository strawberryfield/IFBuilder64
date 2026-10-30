//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/Validatore.cs" company="Casasoft">
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

/// <summary>Controlla la coerenza di un documento (spec. 6.3).</summary>
public static class Validatore
{
    /// <summary>Numero massimo di stanze, oggetti o porte (indici 0-254; 255 = "non trovato").</summary>
    private const int Massimo = 255;

    private const string NomeGiocatore = "player";

    private static readonly Direzione[] _direzioni = Enum.GetValues<Direzione>();

    /// <summary>Elenca errori e avvisi del documento.</summary>
    /// <param name="doc">Documento da controllare.</param>
    /// <param name="numeroMessaggiTemplate">Numero di messaggi standard del template della lingua del gioco.</param>
    public static IReadOnlyList<Avviso> Valida(DocumentoViewModel doc, int numeroMessaggiTemplate)
    {
        var r = new List<Avviso>();
        ControllaStanze(doc, r);
        ControllaDuplicati(doc, r);
        ControllaLimiti(doc, r);
        ControllaOggetti(doc, r);
        ControllaPorte(doc, r);
        ControllaRaggiungibilita(doc, r);
        ControllaVerbiEMessaggi(doc, numeroMessaggiTemplate, r);
        return r;
    }

    /// <summary>Indica se tra gli avvisi è presente almeno un errore.</summary>
    public static bool HaErrori(IReadOnlyList<Avviso> avvisi) => avvisi.Any(a => a.Gravita == Gravita.Errore);

    private static string Testo(string chiave, params object?[] argomenti) =>
        string.Format(Testi.ResourceManager.GetString(chiave, Testi.Culture) ?? chiave, argomenti);

    private static void Errore(List<Avviso> r, object? elemento, string chiave, params object?[] argomenti) =>
        r.Add(new Avviso(Gravita.Errore, Testo(chiave, argomenti), elemento));

    private static void Avvisa(List<Avviso> r, object? elemento, string chiave, params object?[] argomenti) =>
        r.Add(new Avviso(Gravita.Avviso, Testo(chiave, argomenti), elemento));

    private static void ControllaStanze(DocumentoViewModel doc, List<Avviso> r)
    {
        if (doc.Stanze.Count == 0) Errore(r, doc, "Val_NessunaStanza");
        else if (string.IsNullOrEmpty(doc.StanzaIniziale) || doc.TrovaStanza(doc.StanzaIniziale) is null)
            Errore(r, doc, "Val_StanzaInizialeNonValida");

        foreach (var s in doc.Stanze)
            foreach (var d in _direzioni)
                if (!string.IsNullOrEmpty(s.Uscita(d)) && doc.TrovaStanza(s.Uscita(d)) is null)
                    Avvisa(r, s, "Val_UscitaInesistente", s.Nome, s.Uscita(d));
    }

    /// <summary>Un errore per ogni nome o id già comparso in precedenza.</summary>
    private static void ControllaDuplicati(DocumentoViewModel doc, List<Avviso> r)
    {
        Duplicati(doc.Stanze, s => s.Nome, "Val_NomeStanzaDuplicato", r);
        Duplicati(doc.Porte, p => p.Nome, "Val_NomePortaDuplicato", r);
        Duplicati(doc.Oggetti, o => o.Id, "Val_IdOggettoDuplicato", r);
    }

    private static void Duplicati<T>(IEnumerable<T> elementi, Func<T, string> chiave, string messaggio, List<Avviso> r)
    {
        var visti = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in elementi)
            if (!visti.Add(chiave(e))) Errore(r, e, messaggio, chiave(e));
    }

    private static void ControllaLimiti(DocumentoViewModel doc, List<Avviso> r)
    {
        if (doc.Stanze.Count > Massimo) Errore(r, doc, "Val_TroppeStanze");
        if (doc.Oggetti.Count > Massimo) Errore(r, doc, "Val_TroppiOggetti");
        if (doc.Porte.Count > Massimo) Errore(r, doc, "Val_TroppePorte");
    }

    private static void ControllaOggetti(DocumentoViewModel doc, List<Avviso> r)
    {
        foreach (var o in doc.Oggetti)
        {
            if (o.Statico == true && string.IsNullOrEmpty(o.Posizione))
                Errore(r, o, "Val_StaticoSenzaStanza", o.Nome);

            if (!string.IsNullOrEmpty(o.Posizione) && o.Posizione != NomeGiocatore && doc.TrovaStanza(o.Posizione) is null)
                Avvisa(r, o, "Val_PosizioneOrfana", o.Nome, o.Posizione);
            if (!string.IsNullOrEmpty(o.DentroA) && doc.TrovaOggettoPerId(o.DentroA) is null)
                Avvisa(r, o, "Val_ContenitoreOrfano", o.Nome, o.DentroA);
            if (SbloccoOrfano(doc, o.OggettoSblocco))
                Avvisa(r, o, "Val_SbloccoOrfano", o.Nome, o.OggettoSblocco);
        }
    }

    private static bool SbloccoOrfano(DocumentoViewModel doc, string? nome) =>
        !string.IsNullOrEmpty(nome) && !doc.Oggetti.Any(o => o.Nome == nome);

    private static void ControllaPorte(DocumentoViewModel doc, List<Avviso> r)
    {
        foreach (var p in doc.Porte)
        {
            var lati = _direzioni.Where(d => !string.IsNullOrEmpty(p.Uscita(d))).ToList();
            foreach (var d in lati)
            {
                var stanza = doc.TrovaStanza(p.Uscita(d));
                if (stanza is null)
                    Avvisa(r, p, "Val_UscitaInesistente", p.Nome, p.Uscita(d));
                else if (p.Aperta != true && !string.IsNullOrEmpty(stanza.Uscita(d)))
                    r.Add(new Avviso(Gravita.Avviso, Testo("Val_UscitaDoppia", stanza.Nome, d), stanza,
                        () => Collegamenti.CorreggiUscitaDoppia(doc, stanza, d)));
            }

            if (SbloccoOrfano(doc, p.OggettoSblocco))
                Avvisa(r, p, "Val_SbloccoOrfano", p.Nome, p.OggettoSblocco);

            if (p.Aperta == true && lati.Count == 2
                && doc.TrovaStanza(p.Uscita(lati[0])) is { } s1 && doc.TrovaStanza(p.Uscita(lati[1])) is { } s2
                && (s1.Uscita(lati[0]) != s2.Nome || s2.Uscita(lati[1]) != s1.Nome))
                Avvisa(r, p, "Val_PortaApertaSenzaUscite", p.Nome);
        }
    }

    /// <summary>
    /// Stanze raggiungibili dalla stanza iniziale (compresa) seguendo uscite e porte;
    /// insieme vuoto se la stanza iniziale manca o non esiste.
    /// </summary>
    public static IReadOnlySet<StanzaViewModel> StanzeRaggiungibili(DocumentoViewModel doc)
    {
        var visitate = new HashSet<StanzaViewModel>();
        if (doc.TrovaStanza(doc.StanzaIniziale) is not { } iniziale) return visitate;

        var collegamenti = Collegamenti.Tutti(doc).ToLookup(c => c.Da);
        visitate.Add(iniziale);
        var coda = new Queue<StanzaViewModel>([iniziale]);
        while (coda.Count > 0)
            foreach (var c in collegamenti[coda.Dequeue()])
                if (visitate.Add(c.A)) coda.Enqueue(c.A);
        return visitate;
    }

    /// <summary>Avvisa per le stanze non raggiungibili dalla stanza iniziale; "player" non conta.</summary>
    private static void ControllaRaggiungibilita(DocumentoViewModel doc, List<Avviso> r)
    {
        if (string.IsNullOrEmpty(doc.StanzaIniziale) || doc.TrovaStanza(doc.StanzaIniziale) is null) return;

        var raggiungibili = StanzeRaggiungibili(doc);
        foreach (var s in doc.Stanze)
            if (!s.EGiocatore && !raggiungibili.Contains(s)) Avvisa(r, s, "Val_StanzaNonRaggiungibile", s.Nome);
    }

    private static void ControllaVerbiEMessaggi(DocumentoViewModel doc, int numeroMessaggiTemplate, List<Avviso> r)
    {
        foreach (var v in doc.Verbi)
            if (!CatalogoAzioni.Esiste(v.Azione)) Avvisa(r, v, "Val_VerboFuoriCatalogo", v.Comando, v.Azione);

        if (doc.MessaggiStandard.Count != numeroMessaggiTemplate)
            Avvisa(r, doc, "Val_NumeroMessaggi", doc.MessaggiStandard.Count, numeroMessaggiTemplate);
    }
}

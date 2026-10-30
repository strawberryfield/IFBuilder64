//-----------------------------------------------------------------------
// <copyright file="Editor/ViewModels/MappaViewModel.cs" company="Casasoft">
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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Casasoft.IFBuilder64.Editor.ViewModels;

/// <summary>Collegamento da disegnare sulla mappa; <see cref="DoppioSenso"/> se percorribile nei due versi.</summary>
public record CollegamentoMappa(Collegamento Collegamento, bool DoppioSenso);

/// <summary>
/// ViewModel della scheda Mappa: livello corrente, zoom, selezione e operazioni sulle stanze
/// e sui collegamenti. La stanza "player" non compare mai sulla mappa.
/// </summary>
public partial class MappaViewModel : ObservableObject
{
    /// <summary>Limiti dello zoom.</summary>
    public const double ZoomMinimo = 0.25, ZoomMassimo = 3;

    private readonly IDialoghi _dialoghi;
    private readonly HashSet<int> _livelliAggiunti = [];

    /// <summary>Livello (coordinata Z) mostrato.</summary>
    [ObservableProperty] private int _livelloCorrente;

    /// <summary>Fattore di zoom della mappa.</summary>
    [ObservableProperty] private double _zoom = 1;

    /// <summary>Stanza o porta selezionata, oppure null.</summary>
    [ObservableProperty] private object? _selezione;

    private IReadOnlyList<int> _livelli = [];
    private IReadOnlySet<StanzaViewModel> _nonRaggiungibili = new HashSet<StanzaViewModel>();

    /// <summary>Crea la mappa del documento, posizionata sul livello della stanza iniziale.</summary>
    public MappaViewModel(DocumentoViewModel doc, IDialoghi dialoghi)
    {
        Documento = doc;
        _dialoghi = dialoghi;
        if (doc.TrovaStanza(doc.StanzaIniziale) is { EGiocatore: false, HaPosizione: true } iniziale)
            _livelloCorrente = iniziale.Z;
        Aggiorna();
        doc.ContenutoCambiato += (_, _) => Aggiorna();
    }

    /// <summary>Livelli esistenti, dal più alto al più basso.</summary>
    public IReadOnlyList<int> Livelli
    {
        get => _livelli;
        private set => SetProperty(ref _livelli, value);
    }

    /// <summary>Stanze non raggiungibili dalla stanza iniziale (vuoto se la stanza iniziale non è valida).</summary>
    public IReadOnlySet<StanzaViewModel> NonRaggiungibili
    {
        get => _nonRaggiungibili;
        private set => SetProperty(ref _nonRaggiungibili, value);
    }

    /// <summary>Documento mostrato.</summary>
    public DocumentoViewModel Documento { get; }

    /// <summary>Sollevato quando cambia il contenuto del documento (la mappa va ridisegnata).</summary>
    public event EventHandler? Cambiata;

    /// <summary>Stanze posizionate sul livello corrente, esclusa "player".</summary>
    public IEnumerable<StanzaViewModel> StanzeDelLivello =>
        Documento.Stanze.Where(s => !s.EGiocatore && s.HaPosizione && s.Z == LivelloCorrente);

    /// <summary>
    /// Collegamenti orizzontali tra stanze del livello corrente, senza doppioni: una porta è
    /// riportata una volta sola e nasconde le uscite che la duplicano (porta aperta); un'uscita
    /// con il ritorno opposto è riportata una volta come doppio senso.
    /// </summary>
    public IReadOnlyList<CollegamentoMappa> CollegamentiDelLivello
    {
        get
        {
            var tutti = Collegamenti.Tutti(Documento)
                .Where(c => c.Direzione is not (Direzione.Su or Direzione.Giu) && c.Da != c.A
                    && SulLivello(c.Da) && SulLivello(c.A))
                .ToList();
            var diPorta = tutti.Where(c => c.Porta is not null).ToList();
            var risultato = new List<CollegamentoMappa>();

            foreach (var g in diPorta.GroupBy(c => c.Porta))
                risultato.Add(new CollegamentoMappa(g.First(), g.Count() > 1));

            var uscite = tutti
                .Where(c => c.Porta is null
                    && !diPorta.Any(p => p.Da == c.Da && p.Direzione == c.Direzione && p.A == c.A))
                .ToList();
            var disegnate = new HashSet<Collegamento>();
            foreach (var c in uscite)
            {
                if (disegnate.Contains(c)) continue;
                var ritorno = uscite.FirstOrDefault(r =>
                    r.Da == c.A && r.A == c.Da && r.Direzione == c.Direzione.Opposta() && !disegnate.Contains(r));
                disegnate.Add(c);
                if (ritorno is not null) disegnate.Add(ritorno);
                risultato.Add(new CollegamentoMappa(c, ritorno is not null));
            }
            return risultato;
        }
    }

    /// <summary>Stanza nella cella indicata del livello corrente, o null.</summary>
    public StanzaViewModel? StanzaIn(int x, int y) => StanzeDelLivello.FirstOrDefault(s => s.X == x && s.Y == y);

    /// <summary>Crea una stanza con nome provvisorio univoco nella cella del livello corrente e la seleziona.</summary>
    public StanzaViewModel CreaStanza(int x, int y)
    {
        var s = new StanzaViewModel
        {
            Nome = Documento.NomeUnivoco(Testi.PrefissoStanza, Documento.Stanze.Select(t => t.Nome)),
            HaPosizione = true,
            X = x,
            Y = y,
            Z = LivelloCorrente
        };
        Documento.Stanze.Add(s);
        Selezione = s;
        return s;
    }

    /// <summary>Sposta la stanza in un'altra cella dello stesso livello; false se la cella è occupata.</summary>
    public bool SpostaStanza(StanzaViewModel s, int x, int y)
    {
        if (Documento.Stanze.Any(t => t != s && !t.EGiocatore && t.HaPosizione && t.Z == s.Z && t.X == x && t.Y == y))
            return false;
        s.X = x;
        s.Y = y;
        s.HaPosizione = true;
        return true;
    }

    /// <summary>Aggiunge un livello vuoto sopra il più alto o sotto il più basso e lo rende corrente.</summary>
    public void AggiungiLivello(bool sopra)
    {
        var nuovo = Livelli.Count == 0 ? LivelloCorrente : sopra ? Livelli.Max() + 1 : Livelli.Min() - 1;
        _livelliAggiunti.Add(nuovo);
        AggiornaLivelli();
        LivelloCorrente = nuovo;
    }

    /// <summary>Stanze che si possono collegare verso l'alto o il basso: quelle del livello Z+1 o Z−1.</summary>
    public IReadOnlyList<StanzaViewModel> StanzeCollegabili(StanzaViewModel da, Direzione suOGiu)
    {
        var z = da.Z + DirezioneVerticale(suOGiu).Spostamento().dz;
        return Documento.Stanze.Where(s => !s.EGiocatore && s.HaPosizione && s.Z == z).ToList();
    }

    /// <summary>Collega due stanze in verticale scrivendo l'uscita e quella opposta di ritorno.</summary>
    public void CollegaVerticale(StanzaViewModel da, Direzione suOGiu, StanzaViewModel a) =>
        Collegamenti.Collega(Documento, da, DirezioneVerticale(suOGiu), a, senzaRitorno: false);

    /// <summary>Chiede la stanza del livello sopra o sotto e la collega.</summary>
    public async Task CollegaVerticaleAsync(StanzaViewModel da, Direzione suOGiu)
    {
        var titolo = suOGiu == Direzione.Su ? Testi.Mappa_CollegaSu : Testi.Mappa_CollegaGiu;
        if (await _dialoghi.ScegliStanzaAsync(titolo, StanzeCollegabili(da, suOGiu)) is { } a)
            CollegaVerticale(da, suOGiu, a);
    }

    /// <summary>
    /// Esegue un'azione richiesta dall'interfaccia (menu contestuali); un'eccezione viene mostrata
    /// con <see cref="IDialoghi.MostraErroreAsync"/> invece di far cadere l'applicazione.
    /// </summary>
    public async Task EseguiAsync(Func<Task> azione)
    {
        try
        {
            await azione();
        }
        catch (Exception ex)
        {
            await _dialoghi.MostraErroreAsync(ex.Message);
        }
    }

    /// <summary>Ridispone tutte le stanze, dopo conferma.</summary>
    [RelayCommand]
    private async Task DisponiTutto()
    {
        if (!await _dialoghi.ConfermaAsync(Testi.Mappa_DisponiTutto_Titolo, Testi.Mappa_DisponiTutto_Messaggio)) return;
        DisposizioneAutomatica.Disponi(Documento, tutte: true);
        if (Documento.TrovaStanza(Documento.StanzaIniziale) is { EGiocatore: false, HaPosizione: true } iniziale)
            LivelloCorrente = iniziale.Z;
    }

    /// <summary>Imposta la stanza come stanza iniziale del gioco.</summary>
    public void ImpostaIniziale(StanzaViewModel s) => Documento.StanzaIniziale = s.Nome;

    /// <summary>Elimina la stanza; se è riferita chiede conferma elencando i riferimenti.</summary>
    public async Task EliminaStanzaAsync(StanzaViewModel s)
    {
        var riferimenti = Documento.TrovaRiferimenti(s);
        if (riferimenti.Count > 0)
        {
            var elenco = string.Join(Environment.NewLine, riferimenti.Select(r => "- " + Descrivi(r)));
            if (!await _dialoghi.ConfermaAsync(Testi.Mappa_EliminaStanza_Titolo,
                    string.Format(Testi.Mappa_EliminaStanza_Messaggio, s.Nome, elenco)))
                return;
        }
        Documento.Elimina(s);
    }

    /// <summary>Sostituisce il passaggio con una porta chiusa e la seleziona.</summary>
    public void TrasformaInPorta(Collegamento c) =>
        Selezione = Collegamenti.TrasformaInPorta(Documento, c.Da, c.Direzione);

    /// <summary>Sostituisce la porta con un passaggio libero.</summary>
    public void TrasformaInPassaggio(PortaViewModel porta) => Collegamenti.TrasformaInPassaggio(Documento, porta);

    /// <summary>Elimina il collegamento: la porta, oppure l'uscita e il suo ritorno.</summary>
    public void EliminaCollegamento(Collegamento c)
    {
        if (c.Porta is not null) Documento.Elimina(c.Porta);
        else Collegamenti.Elimina(Documento, c.Da, c.Direzione);
    }

    partial void OnZoomChanged(double value)
    {
        var limitato = Math.Clamp(value, ZoomMinimo, ZoomMassimo);
        if (limitato != value) Zoom = limitato;
    }

    partial void OnLivelloCorrenteChanged(int value) => AggiornaLivelli();

    /// <summary>Selezionando una stanza o una porta di un altro livello la mappa passa a quel livello.</summary>
    partial void OnSelezioneChanged(object? value)
    {
        var stanza = value switch
        {
            StanzaViewModel s => s,
            PortaViewModel p => Enum.GetValues<Direzione>()
                .Select(d => Documento.TrovaStanza(p.Uscita(d)))
                .FirstOrDefault(s => s is { EGiocatore: false, HaPosizione: true }),
            _ => null
        };
        if (stanza is { EGiocatore: false, HaPosizione: true } && stanza.Z != LivelloCorrente)
            LivelloCorrente = stanza.Z;
    }

    private bool SulLivello(StanzaViewModel s) => !s.EGiocatore && s.HaPosizione && s.Z == LivelloCorrente;

    private void Aggiorna()
    {
        if (Selezione is StanzaViewModel s && !Documento.Stanze.Contains(s)
            || Selezione is PortaViewModel p && !Documento.Porte.Contains(p))
            Selezione = null;

        var raggiungibili = Validatore.StanzeRaggiungibili(Documento);
        NonRaggiungibili = raggiungibili.Count == 0
            ? new HashSet<StanzaViewModel>()
            : Documento.Stanze.Where(t => !t.EGiocatore && !raggiungibili.Contains(t)).ToHashSet();
        AggiornaLivelli();
        Cambiata?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ricalcola i livelli; sostituisce l'elenco solo se cambia.</summary>
    private void AggiornaLivelli()
    {
        var nuovi = Documento.Stanze.Where(s => !s.EGiocatore && s.HaPosizione).Select(s => s.Z)
            .Concat(_livelliAggiunti)
            .Append(LivelloCorrente)
            .Distinct()
            .OrderDescending()
            .ToList();
        if (nuovi.SequenceEqual(Livelli)) return;
        Livelli = nuovi;
        // Il selettore perde la selezione quando cambia l'elenco: la ripristina.
        OnPropertyChanged(nameof(LivelloCorrente));
    }

    private static Direzione DirezioneVerticale(Direzione d) =>
        d is Direzione.Su or Direzione.Giu ? d : throw new ArgumentException($"Direzione non verticale: {d}", nameof(d));

    private static string Descrivi(Riferimento r)
    {
        var nome = r.Elemento switch
        {
            StanzaViewModel s => s.Nome,
            PortaViewModel p => p.Nome,
            OggettoViewModel o => o.Nome,
            _ => string.Empty
        };
        return string.IsNullOrEmpty(nome) ? r.Campo : $"{nome} ({r.Campo})";
    }
}

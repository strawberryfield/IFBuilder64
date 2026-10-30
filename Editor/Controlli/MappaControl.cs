//-----------------------------------------------------------------------
// <copyright file="Editor/Controlli/MappaControl.cs" company="Casasoft">
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

using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Casasoft.IFBuilder64.Editor.Risorse;
using Casasoft.IFBuilder64.Editor.Servizi;
using Casasoft.IFBuilder64.Editor.ViewModels;

namespace Casasoft.IFBuilder64.Editor.Controlli;

/// <summary>
/// Mappa delle stanze di un livello su una griglia: disegno con <see cref="Render"/>, pan e zoom,
/// creazione e spostamento delle stanze, collegamenti trascinando dal bordo, menu contestuali.
/// </summary>
public class MappaControl : Control
{
    /// <summary>ViewModel della mappa.</summary>
    public static readonly StyledProperty<MappaViewModel?> MappaProperty =
        AvaloniaProperty.Register<MappaControl, MappaViewModel?>(nameof(Mappa));

    /// <summary>Lato della cella a zoom 1, in pixel.</summary>
    public const double LatoCella = 80;

    /// <summary>Distanza massima dal tracciato per colpire un collegamento, in pixel.</summary>
    private const double SogliaCollegamento = 6;

    /// <summary>Spostamento minimo perché un clic diventi trascinamento, in pixel.</summary>
    private const double SogliaTrascinamento = 4;

    private enum Stato { Nessuno, CellaVuota, Sposta, Collega, Pan }

    private Stato _stato;
    private Point _origine;
    private bool _origineDaCalcolare = true;
    private Point _puntoIniziale, _puntoCorrente, _origineIniziale;
    private StanzaViewModel? _stanzaTrascinata;
    private Direzione _direzioneCollegamento;
    private double _zoomPrecedente = 1;
    private Point? _perno;

    static MappaControl()
    {
        AffectsRender<MappaControl>(MappaProperty);
        FocusableProperty.OverrideDefaultValue<MappaControl>(true);
        ClipToBoundsProperty.OverrideDefaultValue<MappaControl>(true);
    }

    /// <summary>Crea il controllo; si ridisegna anche al cambio di tema chiaro/scuro.</summary>
    public MappaControl()
    {
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    /// <summary>ViewModel della mappa.</summary>
    public MappaViewModel? Mappa
    {
        get => GetValue(MappaProperty);
        set => SetValue(MappaProperty, value);
    }

    /// <summary>Geometria corrente (zoom e pan compresi).</summary>
    public GeometriaMappa Geometria
    {
        get
        {
            if (_origineDaCalcolare) CalcolaOrigine();
            return new GeometriaMappa(LatoCella, Mappa?.Zoom ?? 1, _origine);
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MappaProperty)
        {
            if (change.OldValue is MappaViewModel vecchia)
            {
                vecchia.PropertyChanged -= Mappa_PropertyChanged;
                vecchia.Cambiata -= Mappa_Cambiata;
            }
            if (change.NewValue is MappaViewModel nuova)
            {
                nuova.PropertyChanged += Mappa_PropertyChanged;
                nuova.Cambiata += Mappa_Cambiata;
                _zoomPrecedente = nuova.Zoom;
            }
            _stato = Stato.Nessuno;
            _origineDaCalcolare = true;
        }
    }

    private void Mappa_Cambiata(object? sender, EventArgs e) => InvalidateVisual();

    private void Mappa_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MappaViewModel.Zoom) && Mappa is not null)
        {
            // Lo zoom mantiene fermo il punto sotto il puntatore (rotella) o il centro del controllo
            var perno = _perno ?? new Point(Bounds.Width / 2, Bounds.Height / 2);
            var fattore = Mappa.Zoom / _zoomPrecedente;
            _origine = perno - (perno - _origine) * fattore;
            _zoomPrecedente = Mappa.Zoom;
        }
        InvalidateVisual();
    }

    /// <summary>Posiziona la vista in modo che la stanza più in alto a sinistra sia vicina all'angolo.</summary>
    private void CalcolaOrigine()
    {
        _origineDaCalcolare = false;
        var lato = LatoCella * (Mappa?.Zoom ?? 1);
        var stanze = Mappa?.Documento.Stanze.Where(s => !s.EGiocatore && s.HaPosizione).ToList() ?? [];
        var minX = stanze.Count == 0 ? 0 : stanze.Min(s => s.X);
        var minY = stanze.Count == 0 ? 0 : stanze.Min(s => s.Y);
        _origine = new Point(lato / 2 - minX * lato, lato / 2 - minY * lato);
    }

    #region Disegno

    /// <summary>Colori della mappa, scelti in base al tema chiaro o scuro.</summary>
    private sealed record Colori(IBrush Sfondo, IPen Griglia, IBrush Stanza, IPen BordoStanza, IBrush Testo,
        IBrush Linea, IPen Collegamento, IPen Avviso, IPen Selezione, IBrush Accento, IPen Anteprima);

    private Colori ScegliColori()
    {
        var scuro = ActualThemeVariant == ThemeVariant.Dark;
        var accento = this.TryFindResource("SystemAccentColor", ActualThemeVariant, out var r) && r is Color c
            ? c
            : Color.Parse("#0078D4");
        var accentoPennello = new SolidColorBrush(accento);
        var linea = new SolidColorBrush(Color.Parse(scuro ? "#B0B0B0" : "#505050"));
        var zoom = Mappa?.Zoom ?? 1;
        return new Colori(
            Sfondo: new SolidColorBrush(Color.Parse(scuro ? "#1E1E1E" : "#FAFAFA")),
            Griglia: new Pen(new SolidColorBrush(Color.Parse(scuro ? "#333333" : "#E4E4E4")), 1),
            Stanza: new SolidColorBrush(Color.Parse(scuro ? "#2D2D30" : "#FFFFFF")),
            BordoStanza: new Pen(new SolidColorBrush(Color.Parse(scuro ? "#909090" : "#606060")), 1.2),
            Testo: new SolidColorBrush(Color.Parse(scuro ? "#F0F0F0" : "#202020")),
            Linea: linea,
            Collegamento: new Pen(linea, Math.Max(1.5, 2 * zoom)),
            Avviso: new Pen(new SolidColorBrush(Color.Parse(scuro ? "#FF9800" : "#E07000")), 3,
                new DashStyle([2, 1], 0)),
            Selezione: new Pen(accentoPennello, 3),
            Accento: accentoPennello,
            Anteprima: new Pen(accentoPennello, 2, new DashStyle([3, 2], 0)));
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        var colori = ScegliColori();
        var area = new Rect(Bounds.Size);
        // Lo sfondo pieno rende il controllo sensibile al puntatore in ogni punto
        context.FillRectangle(colori.Sfondo, area);
        if (Mappa is not { } mappa) return;

        var g = Geometria;
        DisegnaGriglia(context, g, area, colori.Griglia);

        foreach (var c in mappa.CollegamentiDelLivello)
            DisegnaCollegamento(context, g, c, colori, mappa.Selezione);

        foreach (var s in mappa.StanzeDelLivello)
            DisegnaStanza(context, g, s, mappa, colori);

        DisegnaAnteprima(context, g, mappa, colori);
    }

    private static void DisegnaGriglia(DrawingContext context, GeometriaMappa g, Rect area, IPen penna)
    {
        if (g.Lato < 4) return;
        var inizioX = g.Origine.X - Math.Ceiling(g.Origine.X / g.Lato) * g.Lato;
        for (var x = inizioX; x <= area.Right; x += g.Lato)
            context.DrawLine(penna, new Point(x, 0), new Point(x, area.Bottom));
        var inizioY = g.Origine.Y - Math.Ceiling(g.Origine.Y / g.Lato) * g.Lato;
        for (var y = inizioY; y <= area.Bottom; y += g.Lato)
            context.DrawLine(penna, new Point(0, y), new Point(area.Right, y));
    }

    private static void DisegnaCollegamento(DrawingContext context, GeometriaMappa g, CollegamentoMappa cm,
        Colori colori, object? selezione)
    {
        var c = cm.Collegamento;
        var punti = g.Tracciato(CellaDi(c.Da), c.Direzione, CellaDi(c.A));
        for (var i = 1; i < punti.Count; i++)
            context.DrawLine(colori.Collegamento, punti[i - 1], punti[i]);

        if (!cm.DoppioSenso)
            DisegnaFreccia(context, punti[^2], punti[^1], g.Lato * 0.12, colori.Linea);

        if (c.Porta is { } porta)
        {
            var lato = g.Lato * 0.16;
            var medio = GeometriaMappa.PuntoMedio(punti);
            var rettangolo = new Rect(medio.X - lato / 2, medio.Y - lato / 2, lato, lato);
            var chiusa = porta.Aperta != true;
            var penna = ReferenceEquals(porta, selezione) ? colori.Selezione : colori.Collegamento;
            context.DrawRectangle(chiusa ? colori.Linea : colori.Sfondo, penna, rettangolo);
        }
    }

    private static void DisegnaFreccia(DrawingContext context, Point da, Point a, double lunghezza, IBrush pennello)
    {
        var v = a - da;
        var modulo = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        if (modulo == 0) return;
        var u = new Point(v.X / modulo, v.Y / modulo);
        var n = new Point(-u.Y, u.X);
        var base_ = a - u * lunghezza;
        DisegnaPoligono(context, pennello, null,
            a, base_ + n * (lunghezza / 2), base_ - n * (lunghezza / 2));
    }

    private static void DisegnaStanza(DrawingContext context, GeometriaMappa g, StanzaViewModel s,
        MappaViewModel mappa, Colori colori)
    {
        var r = g.RettangoloStanza(CellaDi(s));
        var raggio = g.Lato * 0.06;
        if (mappa.NonRaggiungibili.Contains(s))
            context.DrawRectangle(null, colori.Avviso, r.Inflate(3), raggio + 3, raggio + 3);
        var selezionata = ReferenceEquals(s, mappa.Selezione);
        context.DrawRectangle(colori.Stanza, selezionata ? colori.Selezione : colori.BordoStanza, r, raggio, raggio);

        var simbolo = g.Lato * 0.13;
        var margine = g.Lato * 0.05;
        if (s.Nome == mappa.Documento.StanzaIniziale)
            DisegnaStella(context, new Point(r.Left + margine + simbolo / 2, r.Top + margine + simbolo / 2),
                simbolo / 2, colori.Accento);
        if (!string.IsNullOrEmpty(s.Su))
            DisegnaPoligono(context, colori.Testo, null,
                new Point(r.Right - margine - simbolo / 2, r.Top + margine),
                new Point(r.Right - margine, r.Top + margine + simbolo),
                new Point(r.Right - margine - simbolo, r.Top + margine + simbolo));
        if (!string.IsNullOrEmpty(s.Giu))
            DisegnaPoligono(context, colori.Testo, null,
                new Point(r.Right - margine - simbolo, r.Bottom - margine - simbolo),
                new Point(r.Right - margine, r.Bottom - margine - simbolo),
                new Point(r.Right - margine - simbolo / 2, r.Bottom - margine));

        var testo = new FormattedText(s.Nome, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            Typeface.Default, Math.Max(7, 12 * mappa.Zoom), colori.Testo)
        {
            MaxTextWidth = Math.Max(1, r.Width - 2 * margine),
            MaxTextHeight = Math.Max(1, r.Height - 2 * margine),
            TextAlignment = TextAlignment.Center,
            Trimming = TextTrimming.CharacterEllipsis
        };
        using (context.PushClip(r))
            context.DrawText(testo, new Point(r.Left + margine, r.Center.Y - testo.Height / 2));
    }

    private static void DisegnaStella(DrawingContext context, Point centro, double raggio, IBrush pennello)
    {
        var punti = new Point[10];
        for (var i = 0; i < 10; i++)
        {
            var angolo = -Math.PI / 2 + i * Math.PI / 5;
            var rr = i % 2 == 0 ? raggio : raggio * 0.45;
            punti[i] = new Point(centro.X + rr * Math.Cos(angolo), centro.Y + rr * Math.Sin(angolo));
        }
        DisegnaPoligono(context, pennello, null, punti);
    }

    private static void DisegnaPoligono(DrawingContext context, IBrush? pennello, IPen? penna, params Point[] punti)
    {
        var geometria = new StreamGeometry();
        using (var ctx = geometria.Open())
        {
            ctx.BeginFigure(punti[0], true);
            foreach (var p in punti.Skip(1)) ctx.LineTo(p);
            ctx.EndFigure(true);
        }
        context.DrawGeometry(pennello, penna, geometria);
    }

    private void DisegnaAnteprima(DrawingContext context, GeometriaMappa g, MappaViewModel mappa, Colori colori)
    {
        if (_stanzaTrascinata is null || Distanza(_puntoCorrente, _puntoIniziale) < SogliaTrascinamento) return;
        switch (_stato)
        {
            case Stato.Sposta:
                var cella = g.CellaDa(_puntoCorrente);
                if (mappa.StanzaIn(cella.X, cella.Y) is null)
                    context.DrawRectangle(null, colori.Anteprima, g.RettangoloStanza(cella));
                break;
            case Stato.Collega:
                context.DrawLine(colori.Anteprima, g.Attacco(CellaDi(_stanzaTrascinata), _direzioneCollegamento),
                    _puntoCorrente);
                break;
        }
    }

    #endregion

    #region Input

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Mappa is not { } mappa) return;
        var p = e.GetPosition(this);
        var proprieta = e.GetCurrentPoint(this).Properties;
        var g = Geometria;
        var cella = g.CellaDa(p);
        var stanza = mappa.StanzaIn(cella.X, cella.Y);
        _puntoIniziale = _puntoCorrente = p;
        Focus();

        if (proprieta.IsMiddleButtonPressed)
        {
            _stato = Stato.Pan;
            _origineIniziale = _origine;
        }
        else if (proprieta.IsRightButtonPressed)
        {
            MostraMenu(stanza, stanza is null ? CollegamentoIn(p) : null);
            e.Handled = true;
            return;
        }
        else if (proprieta.IsLeftButtonPressed)
        {
            if (stanza is not null)
            {
                mappa.Selezione = stanza;
                _stanzaTrascinata = stanza;
                if (GeometriaMappa.DirezioneDi(g.BordoDa(p, cella)) is { } dir)
                {
                    _stato = Stato.Collega;
                    _direzioneCollegamento = dir;
                }
                else
                {
                    _stato = Stato.Sposta;
                }
            }
            else if (CollegamentoIn(p) is { } c)
            {
                mappa.Selezione = c.Porta;
                _stato = Stato.Nessuno;
            }
            else
            {
                _stato = Stato.CellaVuota;
            }
        }
        else
        {
            return;
        }
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_stato == Stato.Nessuno) return;
        _puntoCorrente = e.GetPosition(this);
        if (_stato == Stato.Pan) _origine = _origineIniziale + (_puntoCorrente - _puntoIniziale);
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Mappa is not { } mappa || _stato == Stato.Nessuno) return;
        var p = e.GetPosition(this);
        var g = Geometria;
        var cella = g.CellaDa(p);
        var trascinato = Distanza(p, _puntoIniziale) >= SogliaTrascinamento;
        try
        {
            switch (_stato)
            {
                case Stato.CellaVuota when !trascinato && mappa.StanzaIn(cella.X, cella.Y) is null:
                    mappa.CreaStanza(cella.X, cella.Y);
                    break;
                case Stato.Sposta when trascinato && _stanzaTrascinata is not null:
                    mappa.SpostaStanza(_stanzaTrascinata, cella.X, cella.Y);
                    break;
                case Stato.Collega when trascinato && _stanzaTrascinata is not null:
                    if (mappa.StanzaIn(cella.X, cella.Y) is { } arrivo && arrivo != _stanzaTrascinata)
                        Collegamenti.Collega(mappa.Documento, _stanzaTrascinata, _direzioneCollegamento, arrivo,
                            senzaRitorno: e.KeyModifiers.HasFlag(KeyModifiers.Control));
                    break;
            }
        }
        finally
        {
            _stato = Stato.Nessuno;
            _stanzaTrascinata = null;
            e.Pointer.Capture(null);
            InvalidateVisual();
        }
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _stato = Stato.Nessuno;
        _stanzaTrascinata = null;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Mappa is not { } mappa) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _perno = e.GetPosition(this);
            try
            {
                mappa.Zoom *= Math.Pow(1.1, e.Delta.Y);
            }
            finally
            {
                _perno = null;
            }
        }
        else
        {
            // Senza Ctrl la rotella scorre la vista (con Maiusc in orizzontale)
            var passo = LatoCella * mappa.Zoom / 2;
            var delta = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? new Point(e.Delta.Y * passo, 0)
                : new Point(e.Delta.X * passo, e.Delta.Y * passo);
            _origine += delta;
            InvalidateVisual();
        }
        e.Handled = true;
    }

    /// <summary>Collegamento disegnato vicino al punto (entro la soglia), o null.</summary>
    private Collegamento? CollegamentoIn(Point p)
    {
        if (Mappa is not { } mappa) return null;
        var g = Geometria;
        return mappa.CollegamentiDelLivello
            .Select(c => (c.Collegamento, Distanza: GeometriaMappa.Distanza(p,
                g.Tracciato(CellaDi(c.Collegamento.Da), c.Collegamento.Direzione, CellaDi(c.Collegamento.A)))))
            .Where(x => x.Distanza <= SogliaCollegamento)
            .OrderBy(x => x.Distanza)
            .Select(x => x.Collegamento)
            .FirstOrDefault();
    }

    /// <summary>Menu contestuale della stanza o del collegamento sotto il puntatore.</summary>
    private void MostraMenu(StanzaViewModel? stanza, Collegamento? collegamento)
    {
        if (Mappa is not { } mappa) return;
        var voci = new List<Control>();
        if (stanza is not null)
        {
            mappa.Selezione = stanza;
            voci.Add(Voce(Testi.Mappa_CollegaSu, async () => await mappa.CollegaVerticaleAsync(stanza, Direzione.Su),
                mappa.StanzeCollegabili(stanza, Direzione.Su).Count > 0));
            voci.Add(Voce(Testi.Mappa_CollegaGiu, async () => await mappa.CollegaVerticaleAsync(stanza, Direzione.Giu),
                mappa.StanzeCollegabili(stanza, Direzione.Giu).Count > 0));
            voci.Add(Voce(Testi.Mappa_ImpostaIniziale, () => mappa.ImpostaIniziale(stanza),
                mappa.Documento.StanzaIniziale != stanza.Nome));
            voci.Add(new Separator());
            voci.Add(Voce(Testi.Mappa_Elimina, async () => await mappa.EliminaStanzaAsync(stanza)));
        }
        else if (collegamento is not null)
        {
            mappa.Selezione = collegamento.Porta;
            voci.Add(collegamento.Porta is { } porta
                ? Voce(Testi.Mappa_TrasformaInPassaggio, () => mappa.TrasformaInPassaggio(porta))
                : Voce(Testi.Mappa_TrasformaInPorta, () => mappa.TrasformaInPorta(collegamento)));
            voci.Add(Voce(Testi.Mappa_Elimina, () => mappa.EliminaCollegamento(collegamento)));
        }
        if (voci.Count == 0) return;
        new ContextMenu { ItemsSource = voci }.Open(this);
    }

    private static MenuItem Voce(string testo, Action azione, bool attiva = true)
    {
        var voce = new MenuItem { Header = testo, IsEnabled = attiva };
        voce.Click += (_, _) => azione();
        return voce;
    }

    private static MenuItem Voce(string testo, Func<Task> azione, bool attiva = true)
    {
        var voce = new MenuItem { Header = testo, IsEnabled = attiva };
        voce.Click += async (_, _) => await azione();
        return voce;
    }

    #endregion

    private static Cella CellaDi(StanzaViewModel s) => new(s.X, s.Y);

    private static double Distanza(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}

//-----------------------------------------------------------------------
// <copyright file="Editor/Servizi/CatalogoAzioni.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Editor.Servizi;

/// <summary>
/// Azione del motore: codice numerico (0-25) e nome localizzato.
/// </summary>
/// <param name="Codice">Codice dell'azione nel motore.</param>
/// <param name="Nome">Nome localizzato dell'azione.</param>
public record AzioneMotore(byte Codice, string Nome)
{
    /// <inheritdoc />
    public override string ToString() => $"{Nome} ({Codice})";
}

/// <summary>
/// Catalogo dei codici azione riconosciuti dal motore (spec. 5.4).
/// I nomi sono letti dalle risorse alla chiave <c>Azione_N</c>, quindi seguono
/// la cultura corrente di <see cref="Testi"/>.
/// </summary>
public static class CatalogoAzioni
{
    /// <summary>Numero di azioni del motore (codici da 0 a 25).</summary>
    public const int NumeroAzioni = 26;

    /// <summary>Tutte le azioni, in ordine di codice, con i nomi nella lingua corrente.</summary>
    public static IReadOnlyList<AzioneMotore> Tutte =>
        Enumerable.Range(0, NumeroAzioni).Select(i => Crea((byte)i)).ToList();

    /// <summary>Indica se il codice è un'azione valida.</summary>
    public static bool Esiste(byte codice) => codice < NumeroAzioni;

    /// <summary>Restituisce l'azione con il codice indicato, o null se non esiste.</summary>
    public static AzioneMotore? Trova(byte codice) => Esiste(codice) ? Crea(codice) : null;

    private static AzioneMotore Crea(byte codice) =>
        new(codice, Testi.ResourceManager.GetString($"Azione_{codice}", Testi.Culture) ?? $"Azione_{codice}");
}

//-----------------------------------------------------------------------
// <copyright file="Game.cs" company="Casasoft">
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

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents the top-level JSON model for an interactive fiction game used by IFBuilder64.
/// </summary>
/// <remarks>
/// This partial class maps the primary collections and metadata present in the game's JSON
/// representation such as rooms, verbs, items and global messages. Instances are typically
/// (de)serialized using a JSON serializer to load or persist game data.
/// </remarks>
public partial class Game
{
    /// <summary>
    /// Gets or sets the introductory text shown to the player when the game starts.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing the game's intro narrative. May be empty but should not be null
    /// for a well-formed game document.
    /// </value>
    public string intro { get; set; }

    /// <summary>
    /// Gets or sets the collection of rooms that compose the game world.
    /// </summary>
    /// <value>
    /// A <see cref="List{Room}"/> containing the game's rooms. Each <see cref="Room"/> represents
    /// a location with its own description, connections and contained objects.
    /// </value>
    public List<Room> rooms { get; set; }

    /// <summary>
    /// Gets or sets the set of verbs (actions) recognised by the game engine.
    /// </summary>
    /// <value>
    /// A <see cref="List{Verb}"/> describing verbs and their behaviour. Verbs define
    /// how player input is mapped to game operations.
    /// </value>
    public List<Verb> verbs { get; set; }

    /// <summary>
    /// Gets or sets the catalogue of items available in the game.
    /// </summary>
    /// <value>
    /// A <see cref="List{Item}"/> of items that can appear in rooms, be carried by the player,
    /// or otherwise interact with the game state.
    /// </value>
    public List<Item> items { get; set; }

    /// <summary>
    /// Gets or sets global textual messages used by the game engine (for UI, errors, prompts, etc.).
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> where each entry is a message string referenced by index
    /// or key from other game model elements.
    /// </value>
    public List<string> messages { get; set; }
}

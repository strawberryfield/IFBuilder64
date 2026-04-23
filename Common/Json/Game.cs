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

using Casasoft.IFBuilder64.Common.Json;
using System.ComponentModel;

namespace Casasoft.IFBuilder64.Json;

/// <summary>
/// Represents the top-level JSON model for an interactive fiction game used by IFBuilder64.
/// </summary>
/// <remarks>
/// This partial class maps the primary collections and metadata present in the game's JSON
/// representation such as rooms, verbs, items and global messages. Instances are typically
/// (de)serialized using a JSON serializer to load or persist game data.
/// 
/// Notes:
/// - Instances are expected to be populated from JSON; callers should validate required
///   collections (for example, <see cref="rooms"/> and <see cref="items"/>) after deserialization.
/// - Several methods use the sentinel value <c>255</c> to denote "not found" because indices
///   are expressed as <see cref="byte"/> in other parts of the model.
/// </remarks>
[Description("Represents the top-level JSON model for an interactive fiction game used by IFBuilder64.")]
public partial class Game
{
    /// <summary>
    /// Gets or sets the introductory text shown to the player when the game starts.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> containing the game's intro narrative. May be empty but should not be null
    /// for a well-formed game document.
    /// </value>
    [Description("The introductory text shown to the player when the game starts.")]
    public string intro { get; set; }

    /// <summary>
    /// Gets or sets the collection of rooms that compose the game world.
    /// </summary>
    /// <value>
    /// A <see cref="List{Room}"/> containing the game's rooms. Each <see cref="Room"/> represents
    /// a location with its own description, connections and contained objects.
    /// </value>
    [Description("The collection of rooms that compose the game world.")]
    public List<Room> rooms { get; set; }

    /// <summary>
    /// Gets or sets the room in which the game starts.
    /// </summary>
    [Description("The name of the room in which the game starts.")]
    public string initialroom { get; set; }

    /// <summary>
    /// Gets or sets the set of verbs (actions) recognised by the game engine.
    /// </summary>
    /// <value>
    /// A <see cref="List{Verb}"/> describing verbs and their behaviour. Verbs define
    /// how player input is mapped to game operations.
    /// </value>
    [Description("The set of verbs (actions) recognised by the game engine.")]
    public List<Verb> verbs { get; set; }

    /// <summary>
    /// Gets or sets the catalogue of items available in the game.
    /// </summary>
    /// <value>
    /// A <see cref="List{Item}"/> of items that can appear in rooms, be carried by the player,
    /// or otherwise interact with the game state.
    /// </value>
    [Description("The catalogue of items available in the game.")]
    public List<Item> items { get; set; }


    /// <summary>
    /// Gets or sets the collection of doors used by the game.
    /// </summary>
    /// <value>
    /// A <see cref="List{Door}"/> describing door objects and their properties such as
    /// locked/unlocked state, linked rooms, and any custom behaviour. May be empty.
    /// </value>
    [Description("The collection of doors used by the game.")]
    public List<Door> doors { get; set; }

    /// <summary>
    /// Gets or sets global textual messages used by the game engine (for UI, errors, prompts, etc.).
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> where each entry is a message string referenced by index
    /// or key from other game model elements. Entries may be empty strings; callers should
    /// validate indices when resolving messages.
    /// </value>
    [Description("Global textual messages used by the game engine (UI, errors, prompts, etc.).")]
    public List<string> messages { get; set; }

    /// <summary>
    /// Gets or sets game specific textual messages used by the game engine (for UI, errors, prompts, etc.).
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> where each entry is a message string referenced by index
    /// or key from other game model elements. Entries may be empty strings; callers should
    /// validate indices when resolving messages.
    /// </value>
    [Description("Game-specific textual messages (user-defined messages).")]
    public List<string> usermessages { get; set; }

    /// <summary>
    /// Gets or sets the direction identifiers used by the engine.
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> enumerating textual direction names (for example "north", "south", ...)
    /// in the order the engine expects. Consumers should not assume a fixed set of values unless
    /// validated elsewhere in the codebase.
    /// </value>
    [Description("Direction identifiers used by the engine (e.g. \"north\", \"south\").")]
    public List<string> directions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items the player can carry.
    /// </summary>
    /// <value>
    /// A <see cref="byte"/> representing the player's carrying capacity (measured in item units).
    /// Defaults to <c>10</c>. Values are serialized to/from JSON and should be within 0..255.
    /// </value>
    /// <remarks>
    /// This property is part of the game model only and does not enforce behaviour by itself;
    /// the game engine should consult this value when applying inventory limits.
    /// </remarks>
    [Description("Maximum number of items the player can carry (byte, default 10).")]
    public byte maxload { get; set; } = 10;

    /// <summary>
    /// Gets or sets the IFiction backend metadata associated with the game.
    /// </summary>
    /// <value>
    /// An <see cref="IFiction"/> instance containing engine-specific settings,
    /// metadata and per-game configuration used by IFBuilder64. May be <c>null</c>
    /// when the JSON document does not include engine-specific sections.
    /// </value>
    /// <remarks>
    /// This property stores a model implementing the <see cref="IFiction"/> interface.
    /// Consumers should check for <c>null</c> before accessing runtime-specific fields.
    /// </remarks>
    [Description("IFiction backend metadata associated with the game.")]
    public IFiction? ifiction { get; set; }
}

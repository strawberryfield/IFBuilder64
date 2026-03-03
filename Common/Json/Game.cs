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
/// 
/// Notes:
/// - Instances are expected to be populated from JSON; callers should validate required
///   collections (for example, <see cref="rooms"/> and <see cref="items"/>) after deserialization.
/// - Several methods use the sentinel value <c>255</c> to denote "not found" because indices
///   are expressed as <see cref="byte"/> in other parts of the model.
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
    /// Gets or sets the number of the room in which the game starts.
    /// </summary>
    public byte initialroom { get; set; }

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
    /// Gets or sets the collection of doors used by the game.
    /// </summary>
    /// <value>
    /// A <see cref="List{Door}"/> describing door objects and their properties such as
    /// locked/unlocked state, linked rooms, and any custom behaviour. May be empty.
    /// </value>
    public List<Door> doors { get; set; }

    /// <summary>
    /// Gets or sets global textual messages used by the game engine (for UI, errors, prompts, etc.).
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> where each entry is a message string referenced by index
    /// or key from other game model elements. Entries may be empty strings; callers should
    /// validate indices when resolving messages.
    /// </value>
    public List<string> messages { get; set; }

    /// <summary>
    /// Gets or sets game specific textual messages used by the game engine (for UI, errors, prompts, etc.).
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> where each entry is a message string referenced by index
    /// or key from other game model elements. Entries may be empty strings; callers should
    /// validate indices when resolving messages.
    /// </value>
    public List<string> usermessages { get; set; }

    /// <summary>
    /// Gets or sets the direction identifiers used by the engine.
    /// </summary>
    /// <value>
    /// A <see cref="List{String}"/> enumerating textual direction names (for example "north", "south", ...)
    /// in the order the engine expects. Consumers should not assume a fixed set of values unless
    /// validated elsewhere in the codebase.
    /// </value>
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
    public byte maxload { get; set; } = 10;

    /// <summary>
    /// Looks up the zero-based index of a room by its name.
    /// </summary>
    /// <param name="roomName">The name of the room to search for. May be null or empty.</param>
    /// <returns>
    /// The zero-based index of the matching room when found; otherwise <c>255</c> to indicate "not found".
    /// </returns>
    /// <remarks>
    /// This method performs a linear search using <see cref="List{T}.FindIndex"/>. The sentinel value
    /// <c>255</c> is used because room indices are expressed as <see cref="byte"/> elsewhere in the model.
    /// </remarks>
    public byte getRoomIndexByName(string? roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            int index = rooms.FindIndex(r => r.name == roomName);
            return index >= 0 ? (byte)index : (byte)255;
        }
        return 255;
    }

    /// <summary>
    /// Looks up the zero-based index of an item by its name.
    /// </summary>
    /// <param name="itemName">The name of the item to search for. May be null or empty.</param>
    /// <returns>
    /// The zero-based index of the matching item when found; otherwise <c>255</c> to indicate "not found".
    /// </returns>
    /// <remarks>
    /// This method performs a linear search using <see cref="List{T}.FindIndex"/>. The sentinel value
    /// <c>255</c> is used because item indices are represented as <see cref="byte"/> values.
    /// </remarks>
    public byte getItemIndexByName(string? itemName)
    {
        if (!string.IsNullOrEmpty(itemName))
        {
            int index = items.FindIndex(i => i.name == itemName);
            return index >= 0 ? (byte)index : (byte)255;
        }
        return 255;
    }

    /// <summary>
    /// Returns the directional exit indices in a fixed order.
    /// </summary>
    /// <param name="room">The room-like object implementing <see cref="IExits"/> whose exits will be resolved.</param>
    /// <returns>
    /// A new <see cref="byte"/>[] with six elements containing the exit indices
    /// in the following order: north, south, east, west, up, down.
    /// Each element contains the zero-based index of the target room when an exit exists;
    /// a value of <c>255</c> indicates the absence of an exit (i.e. "not found") and should
    /// be handled by callers accordingly.
    /// </returns>
    /// <remarks>
    /// This method allocates and returns a new array on each call. If callers
    /// need to avoid repeated allocations (for example in hot paths), they
    /// should reuse a buffer or implement an alternative accessor that writes
    /// into a provided span or array.
    /// </remarks>
    public byte[] RoomExits(IExits room) =>
    [
        getRoomIndexByName(room.north),
        getRoomIndexByName(room.south),
        getRoomIndexByName(room.east),
        getRoomIndexByName(room.west),
        getRoomIndexByName(room.up),
        getRoomIndexByName(room.down)
    ];

    /// <summary>
    /// Resolves the door indices associated with the specified room for each direction.
    /// </summary>
    /// <param name="room">The room whose adjacent doors are being resolved.</param>
    /// <returns>
    /// A new <see cref="byte"/>[] with six elements containing the door indices in the following order:
    /// north, south, east, west, up, down. Each element contains the zero-based index of the matching door
    /// in the <see cref="doors"/> list; a value of <c>255</c> indicates that no door is present for that
    /// direction.
    /// </returns>
    /// <remarks>
    /// The method performs a separate <see cref="List{T}.FindIndex"/> call per direction, matching the
    /// door side name against <paramref name="room"/>.name. For performance-sensitive scenarios consider
    /// building a lookup map instead of repeated linear searches.
    /// </remarks>
    public byte[] RoomDoors(Room room) =>
    [
        doors.FindIndex(d => d.north == room.name) >= 0 ? (byte)doors.FindIndex(d => d.north == room.name) : (byte)255,
        doors.FindIndex(d => d.south == room.name) >= 0 ? (byte)doors.FindIndex(d => d.south == room.name) : (byte)255,
        doors.FindIndex(d => d.east == room.name) >= 0 ? (byte)doors.FindIndex(d => d.east == room.name) : (byte)255,
        doors.FindIndex(d => d.west == room.name) >= 0 ? (byte)doors.FindIndex(d => d.west == room.name) : (byte)255,
        doors.FindIndex(d => d.up == room.name) >= 0 ? (byte)doors.FindIndex(d => d.up == room.name) : (byte)255,
        doors.FindIndex(d => d.down == room.name) >= 0 ? (byte)doors.FindIndex(d => d.down == room.name) : (byte)255
    ];

    /// <summary>
    /// Counts how many doors reference the specified room across all directions.
    /// </summary>
    /// <param name="room">The room to inspect for attached doors.</param>
    /// <returns>
    /// The number of doors referencing <paramref name="room"/>.name cast to a <see cref="byte"/>.
    /// The count is computed by checking each door's directional fields (north, south, east, west, up, down).
    /// </returns>
    /// <remarks>
    /// The returned value is cast to <see cref="byte"/>; if the true count exceeds 255 it will wrap or be truncated.
    /// Callers that may encounter very large door collections should validate the result or use an alternative
    /// integer-returning API.
    /// </remarks>
    public byte RoomDoorsCount(Room room) =>
        (byte)doors.Count(d => d.north == room.name || d.south == room.name 
            || d.east == room.name || d.west == room.name 
            || d.up == room.name || d.down == room.name);

    /// <summary>
    /// Returns the game's item list sorted first by the `notportable` flag (non-portable items first)
    /// and then by the item's `location` ascending (nulls treated as empty strings).
    /// </summary>
    public void SortItems()
    {
        if (items is null || items.Count <= 1) return;

        var first = items[0];
        var tailSorted = items
            .Skip(1)
            .OrderBy(i => i.notportable)
            .ThenBy(i => i.location ?? string.Empty)
            .ToList();

        var rebuilt = new List<Item>(1 + tailSorted.Count) { first };
        rebuilt.AddRange(tailSorted);
        items = rebuilt;
    }
}

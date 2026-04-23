//-----------------------------------------------------------------------
// <copyright file="Game.Methods.cs" company="Casasoft">
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
/// Partial implementation of the <see cref="Game"/> model containing helper lookup,
/// sorting and aggregation methods used by other components when operating on the
/// in-memory JSON representation of a game.
/// </summary>
/// <remarks>
/// This partial class centralizes frequently used operations such as resolving room/item
/// indices by name or id, computing exits and doors for rooms, and producing small
/// per-room summaries (e.g. first non-portable item index, non-portable counts).
/// Many methods return <c>byte</c> sentinel values (<c>255</c>) to indicate "not found"
/// because indices are represented as <c>byte</c> elsewhere in the model.
/// </remarks>
public partial class Game
{
    /// <summary>
    /// Looks up the zero-based index of a room by its name.
    /// </summary>
    /// <param name="roomName">The name of the room to search for. May be null or empty.</param>
    /// <returns>
    /// The zero-based index of the matching room when found; otherwise <c>255</c> to indicate "not found".
    /// </returns>
    /// <remarks>
    /// This method performs a linear search using <see cref="System.Collections.Generic.List{T}.FindIndex(System.Predicate{T})"/>.
    /// The sentinel value <c>255</c> is used because room indices are expressed as <see cref="byte"/> elsewhere in the model.
    /// Callers should treat <c>255</c> as a missing-value indicator.
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
    /// This method performs a linear search using <see cref="System.Collections.Generic.List{T}.FindIndex(System.Predicate{T})"/>.
    /// The sentinel value <c>255</c> is used because item indices are represented as <see cref="byte"/> values.
    /// If the <c>items</c> collection is null the method returns <c>255</c>.
    /// </remarks>
    public byte getItemIndexByName(string? itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && items is not null)
        {
            int index = items.FindIndex(i => i.name == itemName);
            return index >= 0 ? (byte)index : (byte)255;
        }
        return 255;
    }

    /// <summary>
    /// Looks up the zero-based index of an item by its identifier.
    /// </summary>
    /// <param name="itemId">The identifier of the item to search for. May be null or empty.</param>
    /// <returns>
    /// The zero-based index of the matching item when found; otherwise <c>255</c> to indicate "not found".
    /// </returns>
    /// <remarks>
    /// Performs a linear search over the <c>items</c> collection matching <c>id</c>.
    /// Returns <c>255</c> if <c>items</c> is null or if no item with the given identifier exists.
    /// </remarks>
    public byte getItemIndexById(string? itemId)
    {
        if (!string.IsNullOrEmpty(itemId) && items is not null)
        {
            int index = items.FindIndex(i => i.id == itemId);
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
    /// The method performs a separate <see cref="System.Collections.Generic.List{T}.FindIndex(System.Predicate{T})"/>
    /// call per direction, matching the door side name against <paramref name="room"/>.name. For performance-sensitive
    /// scenarios consider building a lookup map instead of repeated linear searches.
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
    /// Returns the game's item list sorted first by the <c>notportable</c> flag (non-portable items first)
    /// and then by the item's <c>location</c> ascending (nulls treated as empty strings).
    /// </summary>
    /// <remarks>
    /// This method preserves the item at index 0 (if any) and sorts only the subsequent items.
    /// The operation replaces the <c>items</c> list with a newly built list to ensure stable ordering
    /// as produced by LINQ's <c>OrderBy</c>/<c>ThenBy</c>. If <c>items</c> is null or has one element,
    /// no action is taken.
    /// </remarks>
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

    /// <summary>
    /// Returns the game's room list sorted by the <c>name</c> property in ascending order.
    /// The first room (index 0) is preserved in place to maintain the initial room reference.
    /// </summary>
    /// <remarks>
    /// Only the tail of the <c>rooms</c> collection (elements after index 0) is sorted and replaced.
    /// No action is taken when <c>rooms</c> is null or contains a single element.
    /// </remarks>
    public void SortRooms()
    {
        if (rooms is null || rooms.Count <= 1) return;
        var first = rooms[0];
        var tailSorted = rooms
            .Skip(1)
            .OrderBy(r => r.name)
            .ToList();
        var rebuilt = new List<Room>(1 + tailSorted.Count) { first };
        rebuilt.AddRange(tailSorted);
        rooms = rebuilt;
    }

    /// <summary>
    /// Gets the zero-based index of the first item in each room that is marked as not portable.
    /// </summary>
    /// <returns>
    /// A new <see cref="byte"/>[] array where each element corresponds to a room in the
    /// <see cref="rooms"/> collection. Each value represents the zero-based index of the first
    /// not portable item in the respective room, or <c>255</c> if no such item exists.
    /// </returns>
    /// <remarks>
    /// Uses <c>255</c> as a sentinel value to indicate "not found", consistent with other lookup methods.
    /// This method is primarily useful for optimizing initial inventory checks or UI summaries.
    /// </remarks>
    public byte[] GetFirstNotPortableItemIndexPerRoom()
    {
        if (rooms is null || rooms.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var result = new byte[rooms.Count];

        if (items is null || items.Count == 0)
        {
            for (int i = 0; i < result.Length; i++)
                result[i] = 255;
            return result;
        }

        for (int r = 0; r < rooms.Count; r++)
        {
            var roomName = rooms[r].name;
            // `notportable` is `bool?` — compare explicitly to `true` to avoid CS0019.
            int idx = items.FindIndex(it => (it.notportable == true) && it.location == roomName);
            result[r] = idx >= 0 ? (byte)idx : (byte)255;
        }

        return result;
    }

    /// <summary>
    /// Returns the number of not-portable items present in each room.
    /// </summary>
    /// <returns>
    /// A <see cref="byte"/>[] where each element corresponds to a room in <see cref="rooms"/> and
    /// contains the count of items with <c>notportable == true</c> whose <c>location</c> matches the room's name.
    /// The counts are cast to <see cref="byte"/>; values exceeding 255 will be truncated.
    /// If <c>rooms</c> is null or empty, an empty array is returned. If <c>items</c> is null or empty,
    /// the returned array is filled with zeros.
    /// </returns>
    public byte[] sceneryPerRoom()
    {
        if (rooms is null || rooms.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var result = new byte[rooms.Count];

        if (items is null || items.Count == 0)
        {
            for (int i = 0; i < result.Length; i++)
                result[i] = 0;
            return result;
        }

        for (int r = 0; r < rooms.Count; r++)
        {
            var roomName = rooms[r].name;
            int count = items.Count(it => it.notportable == true && it.location == roomName);
            result[r] = (byte)count;
        }

        return result;
    }

    /// <summary>
    /// Returns the number of items whose <c>notportable</c> flag is unset (<c>null</c>).
    /// </summary>
    /// <returns>
    /// The count of items with <c>notportable != true</c>. Returns <c>0</c> if the <c>items</c>
    /// collection is null or empty.
    /// </returns>
    public byte portableItemsCount()
    {
        if (items is null || items.Count == 0)
            return 0;

        return (byte)items.Count(i => i.notportable != true);
    }
}

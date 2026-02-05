//-----------------------------------------------------------------------
// <copyright file="IFBuilder64/ConfigPrg.cs" company="Casasoft">
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

using Casasoft.Commodore;

namespace IFBuilder64;

/// <summary>
/// Represents the program configuration block used by the IFBuilder64 runtime.
/// </summary>
/// <remarks>
/// Instances of this class hold counts and memory addresses required by the
/// runtime/PRG image. The information can be serialized to the compact binary
/// layout expected by the target runtime using <see cref="GetConfigArray"/>.
/// </remarks>
internal class ConfigPrg
{
    /// <summary>
    /// Number of rooms in the game (stored as a single byte in the config).
    /// </summary>
    public byte roomsCount { get; set; }

    /// <summary>
    /// Number of items in the game (stored as a single byte in the config).
    /// </summary>
    public byte itemsCount { get; set; }

    /// <summary>
    /// Number of verbs (actions) supported by the game (stored as a single byte).
    /// </summary>
    public byte verbsCount { get; set; }

    /// <summary>
    /// Number of doors in the game (stored as a single byte in the config).
    /// </summary>
    public byte doorsCount { get; set; }

    /// <summary>
    /// Address of the initial room record.
    /// </summary>
    public ushort initialRoomAddress { get; set; }

    /// <summary>
    /// Address of the intro text.
    /// </summary>
    public ushort introAddress { get; set; }

    /// <summary>
    /// Address of the built-in messages table.
    /// </summary>
    public ushort messagesAddress { get; set; }

    /// <summary>
    /// Address of the user-defined messages table.
    /// </summary>
    public ushort userMessagesAddress { get; set; }

    /// <summary>
    /// Address of the verbs table.
    /// </summary>
    public ushort verbsAddress { get; set; }

    /// <summary>
    /// Address of the directions table.
    /// </summary>
    public ushort dirsAddress { get; set; }

    /// <summary>
    /// Address where door names are stored.
    /// </summary>
    public ushort doorsNameAddress { get; set; }

    /// <summary>
    /// Address where door descriptions are stored.
    /// </summary>
    public ushort doorsDescriptionAddress { get; set; }

    /// <summary>
    /// Address where item names are stored.
    /// </summary>
    public ushort itemsNameAddress { get; set; }

    /// <summary>
    /// Address where item descriptions are stored.
    /// </summary>
    public ushort itemsDescriptionAddress { get; set; }

    /// <summary>
    /// Address where rooms descriptions are stored.
    /// </summary>
    public ushort roomsDescriptionAddress { get; set; }

    /// <summary>
    /// Serializes the configuration into the 26-byte binary block produced by this class.
    /// </summary>
    /// <returns>
    /// A 26-byte array containing the configuration in the following layout:
    /// - byte 0 : <see cref="roomsCount"/> (number of rooms),
    /// - byte 1 : <see cref="itemsCount"/> (number of items),
    /// - byte 2 : duplicate of <see cref="roomsCount"/> (preserved to match the runtime format),
    /// - byte 3 : <see cref="verbsCount"/> (number of verbs),
    /// - bytes 4-5   : <see cref="initialRoomAddress"/> (little-endian, written via <see cref="Conversions.InsertUShort(byte[], int, ushort)"/>),
    /// - bytes 6-7   : <see cref="introAddress"/>,
    /// - bytes 8-9   : <see cref="messagesAddress"/>,
    /// - bytes 10-11 : <see cref="userMessagesAddress"/>,
    /// - bytes 12-13 : <see cref="verbsAddress"/>,
    /// - bytes 14-15 : <see cref="dirsAddress"/>,
    /// - bytes 16-17 : <see cref="doorsNameAddress"/>,
    /// - bytes 18-19 : <see cref="doorsDescriptionAddress"/>,
    /// - bytes 20-21 : <see cref="itemsNameAddress"/>,
    /// - bytes 22-23 : <see cref="itemsDescriptionAddress"/>,
    /// - bytes 24-25 : <see cref="roomsDescriptionAddress"/>.
    /// </returns>
    /// <remarks>
    /// The method uses <see cref="Conversions.InsertUShort(byte[], int, ushort)"/>
    /// to write 16-bit addresses into the array at the specified offsets (little-endian).
    /// The duplicate of <see cref="roomsCount"/> at byte index 2 is intentional to match the
    /// expected runtime/PRG layout produced by this tool.
    /// </remarks>
    public byte[] GetConfigArray()
    {
        byte[] ret = new byte[26];

        ret[0] = roomsCount;
        ret[1] = itemsCount;
        ret[2] = verbsCount;
        ret[3] = doorsCount;

        Conversions.InsertUShort(ret, 4, initialRoomAddress);
        Conversions.InsertUShort(ret, 6, introAddress);
        Conversions.InsertUShort(ret, 8, messagesAddress);
        Conversions.InsertUShort(ret, 10, userMessagesAddress);
        Conversions.InsertUShort(ret, 12, verbsAddress);
        Conversions.InsertUShort(ret, 14, dirsAddress);
        Conversions.InsertUShort(ret, 16, itemsNameAddress);
        Conversions.InsertUShort(ret, 18, itemsDescriptionAddress);
        Conversions.InsertUShort(ret, 20, roomsDescriptionAddress);
        Conversions.InsertUShort(ret, 22, doorsNameAddress);
        Conversions.InsertUShort(ret, 24, doorsDescriptionAddress);
        return ret;
    }
}

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
    /// Serializes the configuration into the 21-byte binary block expected by the PRG.
    /// </summary>
    /// <returns>
    /// A 21-byte array containing:
    /// - byte 0: <see cref="roomsCount"/>,
    /// - byte 1: <see cref="itemsCount"/>,
    /// - byte 2: <see cref="verbsCount"/>,
    /// - bytes 3-4: <see cref="initialRoomAddress"/> (inserted via <see cref="Conversions.InsertUShort"/>),
    /// - bytes 5-6: <see cref="introAddress"/>,
    /// - bytes 7-8: <see cref="messagesAddress"/>,
    /// - bytes 9-10: <see cref="userMessagesAddress"/>,
    /// - bytes 11-12: <see cref="verbsAddress"/>,
    /// - bytes 13-14: <see cref="dirsAddress"/>,
    /// - bytes 15-16: <see cref="itemsNameAddress"/>,
    /// - bytes 17-18: <see cref="itemsDescriptionAddress"/>,
    /// - bytes 19-20: <see cref="roomsDescriptionAddress"/>.
    /// </returns>
    /// <remarks>
    /// The method uses <see cref="Conversions.InsertUShort(byte[], int, ushort)"/>
    /// to write 16-bit addresses into the array at the specified offsets.
    /// </remarks>
    public byte[] GetConfigArray()
    {
        byte[] ret = new byte[21];

        ret[0] = roomsCount;
        ret[1] = itemsCount;
        ret[2] = verbsCount;

        Conversions.InsertUShort(ret, 3, initialRoomAddress);
        Conversions.InsertUShort(ret, 5, introAddress);
        Conversions.InsertUShort(ret, 7, messagesAddress);
        Conversions.InsertUShort(ret, 9, userMessagesAddress);
        Conversions.InsertUShort(ret, 11, verbsAddress);
        Conversions.InsertUShort(ret, 13, dirsAddress);
        Conversions.InsertUShort(ret, 15, itemsNameAddress);
        Conversions.InsertUShort(ret, 17, itemsDescriptionAddress);
        Conversions.InsertUShort(ret, 19, roomsDescriptionAddress);
        return ret;
    }
}

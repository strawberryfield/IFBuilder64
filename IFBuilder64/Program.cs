//-----------------------------------------------------------------------
// <copyright file="IFBuilder64/Program.cs" company="Casasoft">
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
using Casasoft.Helpers;
using Casasoft.IFBuilder64.Json;
using Mono.Options;
using System.Text;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
string OutputFile = string.Empty;
string TemplateType = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "o|out=", "Output file name", o => OutputFile = o },
    { "template=", "Create a game.json template file", o => TemplateType = o }
};

List<string> FilesList = FileHelpers.ExpandWildcards(p.Parse(args));

if (!ShouldSuppressBanner)
    ShowBanner();

if (ShouldShowHelp)
{
    ShowHelp();
    return;
}

//
// template creation
//
if (!string.IsNullOrEmpty(TemplateType))
{
    TemplateLanguage lang = Casasoft.IFBuilder64.Common.Helpers.GetLanguage(TemplateType);

    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        OutputFile = "game.json";
    }
    CreateTemplate(lang);
    Console.WriteLine($"Template file '{OutputFile}' created.");
    return;
}
#endregion

#region build game
if (FilesList.Count > 0)
{
    var outputFile = OutputFile;
    foreach (var inputFile in FilesList)
    {
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            outputFile = Path.ChangeExtension(inputFile, "");
        }
        BuildGame(inputFile, outputFile);
    }
    return;
}
#endregion

#region no command specified
ShowHelp();
return;
#endregion

#region template
void CreateTemplate(TemplateLanguage lang)
{
    var game = new Game(lang);

    var json = System.Text.Json.JsonSerializer.Serialize(game, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("game.json", json);
}
#endregion

#region build
void BuildGame(string inputFile, string outputFile)
{
    Console.WriteLine($"Building '{inputFile}' to '{outputFile}*'...");
    var jsonText = File.ReadAllText(inputFile);
    var game = System.Text.Json.JsonSerializer.Deserialize<Game>(jsonText);
    if (game == null)
    {
        Console.WriteLine($"Error: Unable to parse game file '{inputFile}'.");
        return;
    }

    ushort topAddress = 0xbfff;

    // --- intro ----
    string text = Charset.PETSCII(game.intro, true) + (char)0;
    int size = text.Length;
    ushort introAddress = AllocateDown(ref topAddress, size);
    PrgFile prg = new(introAddress, Encoding.UTF8.GetBytes(text));
    string filename = outputFile + "in.prg";
    prg.Save(filename);
    Console.WriteLine($"Build intro: '{filename}'.");

    // --- data file ---
    // messages
    (byte[] msgPointers, byte[] msgStringsList) = Strings2Prg.CreateArrays(game.messages, 0, true);
    size = msgPointers.Length + msgStringsList.Length;
    ushort messagesAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(msgPointers, messagesAddress);

    // rooms descrptions 
    List<string> roomDescriptions = new();
    roomDescriptions.AddRange(game.rooms.Select(room => room.description));
    (byte[] roomsDescPtr, byte[] roomsDescList) = Strings2Prg.CreateArrays(roomDescriptions, 0, true);
    size = roomsDescPtr.Length + roomsDescList.Length;
    ushort roomsDescAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(roomsDescPtr, roomsDescAddress);

    // verbs
    List<string> verbCommands = new();
    verbCommands.AddRange(game.verbs.Select(verb => verb.command));
    (byte[] verbsPtr, byte[] verbsList) = Strings2Prg.CreateArrays(verbCommands, 0, true);
    size = verbsPtr.Length + verbsList.Length;
    ushort verbsAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(verbsPtr, verbsAddress);

    // items
    List<string> itemNames = new();
    itemNames.AddRange(game.items.Select(item => item.name));
    (byte[] itemsPtr, byte[] itemsList) = Strings2Prg.CreateArrays(itemNames, 0, true);
    size = itemsPtr.Length + itemsList.Length;
    ushort itemsAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsPtr, itemsAddress);

    // items descriptions
    List<string> itemDescriptions = new();
    itemDescriptions.AddRange(game.items.Select(item => item.description));
    (byte[] itemsDescPtr, byte[] itemsDescList) = Strings2Prg.CreateArrays(itemDescriptions, 0, true);
    size = itemsDescPtr.Length + itemsDescList.Length;
    ushort itemsDescAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsDescPtr, itemsDescAddress);

    // save data file
    prg = new(itemsDescAddress, new List<byte[]> {
        itemsDescPtr, itemsDescList,
        itemsPtr, itemsList,
        verbsPtr, verbsList,
        roomsDescPtr, roomsDescList,
        msgPointers, msgStringsList});
    filename = outputFile + "dt.prg";
    prg.Save(filename);
    Console.WriteLine($"Build data file: '{filename}'.");

    // --- Databases ---
    // rooms exits
    byte[] roomsExits = new byte[game.rooms.Count * 6];
    for (int i = 0; i < game.rooms.Count; i++)
    {
        Array.Copy(game.rooms[i].Directions(), 0, roomsExits, i * 6, 6);
    }

    // verbs to actions
    byte[] verbsToActions = new byte[game.verbs.Count];
    for (int i = 0; i < game.verbs.Count; i++)
    {
        verbsToActions[i] = game.verbs[i].action;
    }

    // items to locations
    byte[] itemsToLocations = new byte[game.items.Count];
    for (int i = 0; i < game.items.Count; i++)
    {
        itemsToLocations[i] = game.items[i].location;
    }

    // initial room
    byte[] initialRoom = [game.initialroom];

    // save databases file
    topAddress = 0xcfff;
    ushort itemsDbAddress = AllocateDown(ref topAddress, itemsToLocations.Length);
    ushort verbsDbAddress = AllocateDown(ref topAddress, verbsToActions.Length);
    ushort roomsDbAddress = AllocateDown(ref topAddress, roomsExits.Length);
    ushort initialRoomDbAddress = AllocateDown(ref topAddress, initialRoom.Length);

    prg = new(initialRoomDbAddress, new List<byte[]> {
        initialRoom,
        roomsExits,
        verbsToActions,
        itemsToLocations});
    filename = outputFile + "db.prg";
    prg.Save(filename);
    Console.WriteLine($"Build database file: '{filename}'.");


    // --- basic include file ---
    StringBuilder sb = new();
    sb.AppendLine("' Generated by Casasoft IFBuilder\n'");
    sb.AppendLine("' Strings tables");
    sb.AppendLine($"{{var:introaddr=WA}}");
    sb.AppendLine($"{{var:messagesaddr=WB}}");
    sb.AppendLine($"{{var:roomsdescaddr=WC}}");
    sb.AppendLine($"{{var:verbsaddr=WD}}");
    sb.AppendLine($"{{var:verbstable=WE}}");
    sb.AppendLine($"{{var:itemsaddr=WF}}");
    sb.AppendLine($"{{var:itemstable=WG}}");
    sb.AppendLine($"{{var:itemsdescaddr=WH}}");
    sb.AppendLine("'\n' Databases");
    sb.AppendLine($"{{var:curroomdbaddr=W0}}");
    sb.AppendLine($"{{var:roomsdbaddr=W1}}");
    sb.AppendLine($"{{var:verbsdbaddr=W2}}");
    sb.AppendLine($"{{var:itemsdbaddr=W3}}");

    filename = outputFile + "def.bas";
    File.WriteAllText(filename, sb.ToString());
    Console.WriteLine($"Build BASIC definitions include: '{filename}'.");

    // --- basic include file ---
    sb = new();
    sb.AppendLine("' Generated by Casasoft IFBuilder\n'");
    sb.AppendLine("' Strings addresses");
    sb.Append($"{{var:introaddr}}={introAddress}: ");
    sb.Append($"{{var:messagesaddr}}={messagesAddress}: ");
    sb.Append($"{{var:roomsdescaddr}}={roomsDescAddress}: ");
    sb.Append($"{{var:verbsaddr}}={verbsAddress}: ");
    sb.Append($"{{var:verbstable}}={verbsAddress + verbsPtr.Length}: ");
    sb.Append($"{{var:itemsaddr}}={itemsAddress}: ");
    sb.Append($"{{var:itemstable}}={itemsAddress + itemsPtr.Length}: ");
    sb.AppendLine($"{{var:itemsdescaddr}}={itemsDescAddress}");
    sb.AppendLine("'\n' Databases addresses");
    sb.Append($"{{var:curroomdbaddr}}={initialRoomDbAddress}: ");
    sb.Append($"{{var:roomsdbaddr}}={roomsDbAddress}: ");
    sb.Append($"{{var:verbsdbaddr}}={verbsDbAddress}: ");
    sb.AppendLine($"{{var:itemsdbaddr}}={itemsDbAddress}");

    filename = outputFile + "inc.bas";
    File.WriteAllText(filename, sb.ToString());
    Console.WriteLine($"Build BASIC assignations include: '{filename}'.");
}

// Helper to allocate 'size' bytes below 'topAddress' and update topAddress
ushort AllocateDown(ref ushort top, int size)
{
    ushort addr = (ushort)(top - size + 1);
    top = (ushort)(addr - 1);
    return addr;
}
#endregion

#region Help Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: IFBuilder64 [OPTIONS] FILES");
    Console.WriteLine("Builds data files for Adventure Player 64");
    Console.WriteLine();
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
}

void ShowBanner() => Console.WriteLine("Casasoft IFBuilder64 v1.0\ncopyright (c) 2026 Roberto Ceccarelli - Casasoft\n");
#endregion

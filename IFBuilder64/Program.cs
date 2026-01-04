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
    PrgFile prg = new(introAddress, Encoding.ASCII.GetBytes(text));
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

    // save databases file
    topAddress = 0xcfff;
    ushort itemsDbAddress = AllocateDown(ref topAddress, itemsToLocations.Length);
    ushort verbsDbAddress = AllocateDown(ref topAddress, verbsToActions.Length);
    ushort roomsDbAddress = AllocateDown(ref topAddress, roomsExits.Length);
    prg = new(roomsDbAddress, new List<byte[]> {
        roomsExits,
        verbsToActions,
        itemsToLocations});
    filename = outputFile + "db.prg";
    prg.Save(filename);
    Console.WriteLine($"Build database file: '{filename}'.");


    // --- basic include file ---
    StringBuilder sb = new();
    sb.AppendLine("' Generated by Casasoft IFBuilder\n'");
    sb.AppendLine("' Strings");
    sb.AppendLine($"{{def:introaddr={introAddress}}}");
    sb.AppendLine($"{{def:messagesaddr={messagesAddress}}}");
    sb.AppendLine($"{{def:roomsdescaddr={roomsDescAddress}}}");
    sb.AppendLine($"{{def:verbsaddr={verbsAddress}}}");
    sb.AppendLine($"{{def:verbstable={verbsAddress + verbsPtr.Length}}}");
    sb.AppendLine($"{{def:itemsaddr={itemsAddress}}}");
    sb.AppendLine($"{{def:itemstable={itemsAddress + itemsPtr.Length}}}");
    sb.AppendLine($"{{def:itemsdescaddr={itemsDescAddress}}}");
    sb.AppendLine("'\n' Databases");
    sb.AppendLine($"{{def:roomsdbaddr={roomsDbAddress}}}");
    sb.AppendLine($"{{def:verbsdbaddr={verbsDbAddress}}}");
    sb.AppendLine($"{{def:itemsdbaddr={itemsDbAddress}}}");

    filename = outputFile + "inc.bas";
    File.WriteAllText(filename, sb.ToString());
    Console.WriteLine($"Build BASIC include: '{filename}'.");
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

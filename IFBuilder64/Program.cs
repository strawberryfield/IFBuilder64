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
using IFBuilder64;
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

    ConfigPrg config = new();
    config.roomsCount = (byte)game.rooms.Count;
    config.itemsCount = (byte)game.items.Count;
    config.verbsCount = (byte)game.verbs.Count;
    config.doorsCount = (byte)game.doors.Count;

    ushort topAddress = 0xbfff;

    // --- intro ----
    byte[] text = Charset.PETSCIIBytes(game.intro + (char)0, true);
    int size = text.Length;
    config.introAddress = AllocateDown(ref topAddress, size);
    PrgFile prg = new(config.introAddress, text);
    string filename = outputFile + "in.prg";
    prg.Save(filename);
    Console.WriteLine($"Build intro file: '{filename}'.");

    // --- data file ---
    // messages
    (byte[] msgPointers, byte[] msgStringsList) = Strings2Prg.CreateArrays(game.messages, 0, true);
    size = msgPointers.Length + msgStringsList.Length;
    config.messagesAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(msgPointers, config.messagesAddress);

    // user messages
    (byte[] userMsgPointers, byte[] userMsgStringsList) = Strings2Prg.CreateArrays(game.usermessages, 0, true);
    size = userMsgPointers.Length + userMsgStringsList.Length;
    config.userMessagesAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(userMsgPointers, config.userMessagesAddress);

    // rooms descrptions 
    List<string> roomDescriptions = new();
    roomDescriptions.AddRange(game.rooms.Select(room => room.description));
    (byte[] roomsDescPtr, byte[] roomsDescList) = Strings2Prg.CreateArrays(roomDescriptions, 0, true);
    size = roomsDescPtr.Length + roomsDescList.Length;
    config.roomsDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(roomsDescPtr, config.roomsDescriptionAddress);

    // verbs
    List<string> verbCommands = new();
    verbCommands.AddRange(game.verbs.Select(verb => verb.command));
    (byte[] verbsPtr, byte[] verbsList) = Strings2Prg.CreateArrays(verbCommands, 0, true);
    size = verbsPtr.Length + verbsList.Length;
    config.verbsAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(verbsPtr, config.verbsAddress);

    // verbs to actions
    byte[] verbsToActions = new byte[game.verbs.Count];
    for (int i = 0; i < game.verbs.Count; i++)
    {
        verbsToActions[i] = game.verbs[i].action;
    }
    ushort verbsToActionsAddress = AllocateDown(ref topAddress, verbsToActions.Length);

    // directions
    (byte[] dirPointers, byte[] dirStringsList) = Strings2Prg.CreateArrays(game.directions, 0, true);
    size = dirPointers.Length + dirStringsList.Length;
    config.dirsAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(dirPointers, config.dirsAddress);

    // items
    List<string> itemNames = new();
    itemNames.AddRange(game.items.Select(item => item.name));
    (byte[] itemsPtr, byte[] itemsList) = Strings2Prg.CreateArrays(itemNames, 0, true);
    size = itemsPtr.Length + itemsList.Length;
    config.itemsNameAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsPtr, config.itemsNameAddress);

    // items descriptions
    List<string> itemDescriptions = new();
    itemDescriptions.AddRange(game.items.Select(item => item.description));
    (byte[] itemsDescPtr, byte[] itemsDescList) = Strings2Prg.CreateArrays(itemDescriptions, 0, true);
    size = itemsDescPtr.Length + itemsDescList.Length;
    config.itemsDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsDescPtr, config.itemsDescriptionAddress);

    // items open descriptions
    List<string> itemOpenDescriptions = new();
    itemOpenDescriptions.AddRange(game.items.Select(item => item.opendescription ?? string.Empty));
    (byte[] itemsOpenDescPtr, byte[] itemsOpenDescList) = Strings2Prg.CreateArrays(itemOpenDescriptions, 0, true);
    size = itemsOpenDescPtr.Length + itemsOpenDescList.Length;
    config.itemsOpenDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsOpenDescPtr, config.itemsOpenDescriptionAddress);

    // doors
    List<string> doorNames = new();
    doorNames.AddRange(game.doors.Select(item => item.name));
    (byte[] doorsPtr, byte[] doorsList) = Strings2Prg.CreateArrays(doorNames, 0, true);
    size = doorsPtr.Length + doorsList.Length;
    config.doorsNameAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(doorsPtr, config.doorsNameAddress);

    // doors descriptions
    List<string> doorDescriptions = new();
    doorDescriptions.AddRange(game.doors.Select(item => item.description));
    (byte[] doorsDescPtr, byte[] doorsDescList) = Strings2Prg.CreateArrays(doorDescriptions, 0, true);
    size = doorsDescPtr.Length + doorsDescList.Length;
    config.doorsDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(doorsDescPtr, config.doorsDescriptionAddress);

    // doors exits
    byte[] doorsExits = new byte[game.doors.Count * 6];
    for (int i = 0; i < game.doors.Count; i++)
    {
        Array.Copy(game.RoomExits(game.doors[i]), 0, doorsExits, i * 6, 6);
    }
    ushort doorsExitAddress = AllocateDown(ref topAddress, doorsExits.Length);

    // rooms to doors
    byte[] roomsDoors = new byte[game.rooms.Count * 6];
    for (int i = 0; i < game.rooms.Count; i++)
    {
        Array.Copy(game.RoomDoors(game.rooms[i]), 0, roomsDoors, i * 6, 6);
    }
    ushort roomsDoorsAddress = AllocateDown(ref topAddress, roomsDoors.Length);

    // rooms doors count
    byte[] roomsDoorsCount = new byte[game.rooms.Count];
    for (int i = 0; i < game.rooms.Count; i++)
    {
        roomsDoorsCount[i] = game.RoomDoorsCount(game.rooms[i]);
    }
    ushort roomsDoorsCountAddress = AllocateDown(ref topAddress, roomsDoorsCount.Length);

    // save data file
    prg = new(roomsDoorsCountAddress, new List<byte[]> {
        roomsDoorsCount, roomsDoors, doorsExits,
        doorsDescPtr, doorsDescList,
        doorsPtr, doorsList,
        itemsOpenDescPtr, itemsOpenDescList,
        itemsDescPtr, itemsDescList,
        itemsPtr, itemsList,
        dirPointers, dirStringsList,
        verbsToActions,
        verbsPtr, verbsList,
        roomsDescPtr, roomsDescList,
        userMsgPointers, userMsgStringsList,
        msgPointers, msgStringsList});
    filename = outputFile + "dt.prg";
    prg.Save(filename);
    Console.WriteLine($"Build data file: '{filename}'.");

    // --- Databases ---
    // rooms exits
    byte[] roomsExits = new byte[game.rooms.Count * 6];
    for (int i = 0; i < game.rooms.Count; i++)
    {
        Array.Copy(game.RoomExits(game.rooms[i]), 0, roomsExits, i * 6, 6);
    }

    // items to locations
    byte[] itemsToLocations = new byte[game.items.Count];
    for (int i = 0; i < game.items.Count; i++)
    {
        itemsToLocations[i] = game.getRoomIndexByName(game.items[i].location);
    }

    // items to items
    byte[] itemsToItems = new byte[game.items.Count];
    for (int i = 0; i < game.items.Count; i++)
    {
        itemsToItems[i] = game.getItemIndexByName(game.items[i].initem);
    }

    // items properties
    byte[] itemsOptions = new byte[game.items.Count];
    for (int i = 0; i < game.items.Count; i++)
    {
        itemsOptions[i] = game.items[i].Options();
    }

    // items unlock items
    byte[] itemsUnlocks = new byte[game.items.Count];
    for (int i = 0; i < game.items.Count; i++)
    {
        itemsUnlocks[i] = game.getItemIndexByName(game.items[i].unlockitem);
    }

    // doors properties
    byte[] doorsOptions = new byte[game.doors.Count];
    for (int i = 0; i < game.doors.Count; i++)
    {
        doorsOptions[i] = game.doors[i].Options();
    }

    // doors unlock items
    byte[] doorsUnlocks = new byte[game.doors.Count];
    for (int i = 0; i < game.doors.Count; i++)
    {
        doorsUnlocks[i] = game.getItemIndexByName(game.doors[i].unlockitem);
    }


    // initial room
    byte[] initialRoom = [game.initialroom];

    // save databases file
    ushort DBtopAddress = 0xc9ff;
    topAddress = DBtopAddress;
    ushort doorsUnlocksAddress = AllocateDown(ref topAddress, doorsUnlocks.Length);
    ushort doorsOptionsAddress = AllocateDown(ref topAddress, doorsOptions.Length);
    ushort itemsDbAddress = AllocateDown(ref topAddress, itemsToLocations.Length);
    ushort itemsInItemsDbAddress = AllocateDown(ref topAddress, itemsToItems.Length);
    ushort itemsOptionsDbAddress = AllocateDown(ref topAddress, itemsOptions.Length);
    ushort itemsUnlocksDbAddress = AllocateDown(ref topAddress, itemsUnlocks.Length);
    ushort roomsDbAddress = AllocateDown(ref topAddress, roomsExits.Length);
    config.initialRoomAddress = AllocateDown(ref topAddress, initialRoom.Length);

    prg = new(config.initialRoomAddress, new List<byte[]> {
        initialRoom,
        roomsExits,
        itemsUnlocks,
        itemsOptions,
        itemsToItems,
        itemsToLocations,
        doorsOptions,
        doorsUnlocks
    });
    filename = outputFile + "db.prg";
    prg.Save(filename);
    Console.WriteLine($"Build database file: '{filename}'.");


    prg = new(0x02a7, config.GetConfigArray());
    filename = outputFile + "cf.prg";
    prg.Save(filename);
    Console.WriteLine($"Build config file: '{filename}'.");

    
    // --- memory resize include file ---
    StringBuilder sb = new();
    sb.AppendLine("' Generated by Casasoft IFBuilder\n'");
    sb.AppendLine("' Limit memory to free space for data");
    ushort basicEnd = (ushort)(roomsDoorsCountAddress > 40960 ? 40960 : roomsDoorsCountAddress);
    sb.AppendLine($"if ld=0 then poke 52, {basicEnd / 256}: poke 56, {basicEnd / 256}: clr: ld=1");

    filename = outputFile + "mem.bas";
    File.WriteAllText(filename, sb.ToString());
    Console.WriteLine($"Build BASIC config include: '{filename}'.");
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

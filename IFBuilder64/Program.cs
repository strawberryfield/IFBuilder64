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
using Casasoft.IFBuilder64.Common;
using Casasoft.IFBuilder64.Json;
using IFBuilder64;
using Mono.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Xml.Linq;

#region main
bool ShouldShowHelp = false;
bool ShouldSuppressBanner = false;
bool ShowSchema = false;
string OutputFile = string.Empty;
string TemplateType = string.Empty;

OptionSet p = new()
{
    { "q|quiet", "Suppress banner print", v => ShouldSuppressBanner = v != null },
    { "h|?|help", "Show this help", v => ShouldShowHelp = v != null },
    { "o|out=", "Output file name", o => OutputFile = o },
    { "template=", "Create a game.json template file", o => TemplateType = o },
    { "schema", "Show JSON schema", v => ShowSchema = v != null }
};

List<string> FilesList = FileHelpers.ExpandWildcards(p.Parse(args));

if (!ShouldSuppressBanner)
    ShowBanner();

if (ShouldShowHelp)
{
    ShowHelp();
    return;
}
#endregion

#region schema 
if (ShowSchema)
{
    JsonSerializerOptions options = JsonSerializerOptions.Default;
    JsonNode schema = options.GetJsonSchemaAsNode(typeof(Game), Helpers.GetJsonSchemaDescriptions);

    // Ensure schema contains $schema and title keywords and place them at the beginning
    if (schema is JsonObject schemaObj)
    {
        var ordered = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = "https://strawberryfield.altervista.org/ifbuilder/schema",
            ["title"] = "Casasoft IFBuilder Game Schema"
        };

        foreach (var kvp in schemaObj)
        {
            if (kvp.Key is "$schema" or "title" or "$id") continue;
            // JsonNode instances have a single parent. Cloning ensures we don't
            // attempt to reparent a node that already belongs to schemaObj.
            ordered[kvp.Key] = kvp.Value is null
                ? null
                : JsonNode.Parse(kvp.Value.ToJsonString());
        }
        schema = ordered;
    }

    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        OutputFile = "schema.json";
    }
    File.WriteAllText(OutputFile, schema.ToString());
    return;
}
#endregion

#region template
//
// template creation
//
if (!string.IsNullOrEmpty(TemplateType))
{
    TemplateLanguage lang = Helpers.GetLanguage(TemplateType);

    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        OutputFile = "game.json";
    }
    Game game = new(lang);

    string json = JsonSerializer.Serialize(game, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(OutputFile, json);
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

#region build
void BuildGame(string inputFile, string outputFile)
{
    Console.WriteLine($"Building '{inputFile}' to '{outputFile}*'...");
    string jsonText = File.ReadAllText(inputFile);
    Game? game = JsonSerializer.Deserialize<Game>(jsonText);
    if (game is null)
    {
        Console.WriteLine($"Error: Unable to parse game file '{inputFile}'.");
        return;
    }

    game.SortRooms();
    game.SortItems();
    ConfigPrg config = new(game);
    ushort topAddress = 0xbfff;

    #region intro
    // --- intro ----
    byte[] text = Charset.PETSCIIBytes(game.intro + (char)0, true);
    int size = text.Length;
    config.introAddress = AllocateDown(ref topAddress, size);
    PrgFile prg = new(config.introAddress, text);
    string filename = outputFile + "in.prg";
    prg.Save(filename);
    Console.WriteLine($"Build intro file: '{filename}'.");
    #endregion

    #region data file
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
    List<string> roomDescriptions = [.. game.rooms.Select(room => room.description)];
    (byte[] roomsDescPtr, byte[] roomsDescList) = Strings2Prg.CreateArrays(roomDescriptions, 0, true);
    size = roomsDescPtr.Length + roomsDescList.Length;
    config.roomsDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(roomsDescPtr, config.roomsDescriptionAddress);

    // verbs
    List<string> verbCommands = [.. game.verbs.Select(verb => verb.command)];
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
    List<string> itemNames = [.. game.items.Where(item => item.notportable != true).Select(item => item.name)];
    (byte[] portableItemsPtr, byte[] portableItemsList) = Strings2Prg.CreateArrays(itemNames, 0, true);
    int delta = game.items.Count * 2 - portableItemsPtr.Length + 2;
    Strings2Prg.RelocatePointers(portableItemsPtr, (ushort)delta);
    int offset = portableItemsList.Length;

    byte[][] ptrItemArray = new byte[game.rooms.Count][];
    byte[][] listItemArray = new byte[game.rooms.Count][];
    for (int i = 0; i < game.rooms.Count; i++)
    {
        List<string> itemNamesInRoom =
        [.. game.items.Where(item => item.location == game.rooms[i].name && item.notportable is true).Select(item => item.name)];
        (byte[] tmpPtr, byte[] tmpList) = Strings2Prg.CreateArrays(itemNamesInRoom, (ushort)offset, true);
        if (tmpPtr.Length > 2)
        {
            offset += tmpList.Length;
        }
        ptrItemArray[i] = tmpPtr;
        listItemArray[i] = tmpList;
    }

    byte[] itemsPtr = new byte[game.items.Count * 2 + 2];
    byte[] itemsList = new byte[offset];
    Array.Copy(portableItemsPtr, 0, itemsPtr, 0, portableItemsPtr.Length);
    Conversions.InsertUShort(itemsPtr, 0, (ushort)game.items.Count);
    int ptrOffset = portableItemsPtr.Length;
    Array.Copy(portableItemsList, 0, itemsList, 0, portableItemsList.Length);
    offset = portableItemsList.Length;
    for (int i = 0; i < game.rooms.Count; i++)
    {
        if (ptrItemArray[i].Length < 4)
        {
            continue;
        }
        delta = (game.items.Count + 1) * 2 - ptrItemArray[i].Length;
        Strings2Prg.RelocatePointers(ptrItemArray[i], (ushort)delta);
        Array.Copy(ptrItemArray[i], 2, itemsPtr, ptrOffset, ptrItemArray[i].Length - 2);
        ptrOffset += ptrItemArray[i].Length - 2;
        Array.Copy(listItemArray[i], 0, itemsList, offset, listItemArray[i].Length);
        offset += listItemArray[i].Length;
    }
    size = offset + itemsPtr.Length;
    config.itemsNameAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsPtr, config.itemsNameAddress);

    // items descriptions
    List<string> itemDescriptions = [.. game.items.Select(item => item.description)];
    (byte[] itemsDescPtr, byte[] itemsDescList) = Strings2Prg.CreateArrays(itemDescriptions, 0, true);
    size = itemsDescPtr.Length + itemsDescList.Length;
    config.itemsDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsDescPtr, config.itemsDescriptionAddress);

    // items open descriptions
    List<string> itemOpenDescriptions = [.. game.items.Select(item => item.opendescription ?? string.Empty)];
    (byte[] itemsOpenDescPtr, byte[] itemsOpenDescList) = Strings2Prg.CreateArrays(itemOpenDescriptions, 0, true);
    size = itemsOpenDescPtr.Length + itemsOpenDescList.Length;
    config.itemsOpenDescriptionAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(itemsOpenDescPtr, config.itemsOpenDescriptionAddress);

    // doors
    List<string> doorNames = [.. game.doors.Select(item => item.name)];
    (byte[] doorsPtr, byte[] doorsList) = Strings2Prg.CreateArrays(doorNames, 0, true);
    size = doorsPtr.Length + doorsList.Length;
    config.doorsNameAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(doorsPtr, config.doorsNameAddress);

    // doors deny open descriptions
    List<string> doorDenyOpen = [.. game.doors.Select(item => item.denyopen ?? string.Empty)];
    (byte[] doorsDenyOpenPtr, byte[] doorsDenyOpenList) = Strings2Prg.CreateArrays(doorDenyOpen, 0, true);
    size = doorsDenyOpenPtr.Length + doorsDenyOpenList.Length;
    config.doorsDenyOpenAddress = AllocateDown(ref topAddress, size);
    Strings2Prg.RelocatePointers(doorsDenyOpenPtr, config.doorsDenyOpenAddress);

    // doors descriptions
    List<string> doorDescriptions = [.. game.doors.Select(item => item.description)];
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

    // rooms first item index
    byte[] roomsFirstItem = game.GetFirstNotPortableItemIndexPerRoom();
    ushort roomsFirstItemAddress = AllocateDown(ref topAddress, roomsFirstItem.Length);

    // rooms scenery items count
    byte[] roomsScenery = game.sceneryPerRoom();
    roomsScenery[0] = game.portableItemsCount();
    ushort roomsSceneryAddress = AllocateDown(ref topAddress, roomsScenery.Length);

    // save data file
    prg = new(roomsSceneryAddress, new List<byte[]> {
        roomsScenery, roomsFirstItem,
        roomsDoorsCount, roomsDoors, doorsExits,
        doorsDescPtr, doorsDescList,
        doorsDenyOpenPtr, doorsDenyOpenList,
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
    #endregion

    #region database
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
        itemsToItems[i] = game.getItemIndexById(game.items[i].initem);
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

    // maxload
    byte[] playerLoad = [game.maxload, 0];

    // initial room
    byte[] initialRoom = [game.getRoomIndexByName(game.initialroom)];

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
    AllocateDown(ref topAddress, playerLoad.Length);
    config.initialRoomAddress = AllocateDown(ref topAddress, initialRoom.Length);

    prg = new(config.initialRoomAddress, new List<byte[]> {
        initialRoom,
        playerLoad,
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
    #endregion

    #region config
    prg = new(0x02a7, config.GetConfigArray());
    filename = outputFile + "cf.prg";
    prg.Save(filename);
    Console.WriteLine($"Build config file: '{filename}'.");
    #endregion

    #region ifiction
    StringBuilder sb;
    if (game.ifiction is not null)
    {
        // About data
        filename = outputFile + "ab.seq";
        sb = new();
        sb.Append(Charset.PETSCII(game.ifiction.title, true));
        sb.Append((char)13);
        sb.Append(Charset.PETSCII(game.ifiction.headline, true));
        sb.Append((char)13);
        sb.Append(Charset.PETSCII(game.ifiction.author, true));
        sb.Append((char)13);
        sb.Append(Charset.PETSCII(game.ifiction.ifid ?? "--", true));
        sb.Append((char)13);
        sb.Append(DateTime.Now.Date.ToString("yyyyMMdd"));
        sb.Append((char)13);
        sb.Append((char)13);
        File.WriteAllText(filename, sb.ToString());
        Console.WriteLine($"Build About include: '{filename}'.");

        // IFID 
        if (!string.IsNullOrWhiteSpace(game.ifiction.ifid))
        {
            filename = outputFile + "ifid.seq";
            File.WriteAllText(filename, game.ifiction.ifid.ToUpper());
            Console.WriteLine($"Build IFID include: '{filename}'.");
        }

        // ifiction.xml
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        string version = fvi.FileVersion ?? "0";
        var buildDateAttribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        string buildDate = buildDateAttribute is not null ? buildDateAttribute.DateTime.ToString("yyyy-MM-dd") : "";

        XElement oXml = game.ifiction.ToIFictionXml("Casasoft IFBuilder64", $"{version} {buildDate}");

        // Find elements regardless of namespace
        XElement? oStory = oXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "story");
        XElement? oExecutable = oXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "executable");

        // Add the format element using the same namespace as story
        XNamespace ns = oStory?.Name.Namespace ?? "http://babel.ifarchive.org/protocol/iFiction/";
        oExecutable?.Add(new XElement(ns + "platform", "Commodore64"));

        filename = outputFile + "iFiction.xml";
        oXml.Save(filename);
        Console.WriteLine($"Build IFiction XML: '{filename}'.");
    }
    #endregion 

    #region memory resize
    // --- memory resize include file ---
    sb = new();
    sb.AppendLine("' Generated by Casasoft IFBuilder\n'");
    sb.AppendLine("' Limit memory to free space for data");
    ushort basicEnd = (ushort)(roomsSceneryAddress > 40960 ? 40960 : roomsSceneryAddress);
    sb.AppendLine($"if ld=0 then poke 52, {basicEnd / 256}: poke 56, {basicEnd / 256}: clr: ld=1");

    filename = outputFile + "mem.bas";
    File.WriteAllText(filename, sb.ToString());
    Console.WriteLine($"Build BASIC config include: '{filename}'.");
    #endregion

    #region defs include
    // --- basic defs include file ---
    sb = new();
    sb.AppendLine("' Generated by Casasoft IFBuilder\n'");
    sb.AppendLine("{ifndef:defs_include}");
    sb.AppendLine("{def:defs_include}\n'");

    sb.AppendLine("' Rooms");
    for (int i = 0; i < game.rooms.Count; i++)
    {
        sb.AppendLine($"{{def:room_{game.rooms[i].name}={i}}}");
    }

    sb.AppendLine("'\n' Items");
    for (int i = 0; i < game.items.Count; i++)
    {
        sb.AppendLine($"{{def:item_{game.items[i].id}={i}}}");
    }

    sb.AppendLine("{endif}");
    filename = outputFile + "def.bas";
    File.WriteAllText(filename, sb.ToString());
    Console.WriteLine($"Build BASIC defs include: '{filename}'.");
    #endregion
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

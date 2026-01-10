//-----------------------------------------------------------------------
// <copyright file="SeqConverter/Program.cs" company="Casasoft">
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

TemplateLanguage lang = TemplateLanguage.English; // default language
if (!string.IsNullOrEmpty(TemplateType))
{
    lang = Casasoft.IFBuilder64.Common.Helpers.GetLanguage(TemplateType);
}

foreach (var file in FilesList)
{
    string outFile = OutputFile;
    if (string.IsNullOrWhiteSpace(outFile))
    {
        outFile = Path.ChangeExtension(file, ".json");
    }

    Game game = new(lang);
    string input = Charset.ASCII(File.ReadAllBytes(file), true);
    string[] seqData = input.Split("\r");

    game.intro = seqData[0];
    int numRooms = Convert.ToInt32(seqData[1]);
    int numObjects = Convert.ToInt32(seqData[2]);
    int numVerbs = Convert.ToInt32(seqData[3]);
    int scorebased = Convert.ToInt32(seqData[4]);
    int maxscore = Convert.ToInt32(seqData[5]);
    int startRoom = Convert.ToInt32(seqData[6]);

    game.initialroom = (byte)startRoom;
    for (int i = 0; i < numRooms; i++)
    {
        game.rooms.Add(new Room
        {
            description = seqData[7 + i * 7],
            north = GetByte(seqData[8 + i * 7]),
            south = GetByte(seqData[9 + i * 7]),
            east = GetByte(seqData[10 + i * 7]),
            west = GetByte(seqData[11 + i * 7]),
            up = GetByte(seqData[12 + i * 7]),
            down = GetByte(seqData[13 + i * 7])
        });
    }
    int baseIndex = 7 + numRooms * 7;

    for (int i = 0; i < numObjects; i++)
    {
        game.items.Add(new Item
        {
            name = seqData[baseIndex + i * 2],
            location = GetByte(seqData[baseIndex + i * 2 + 1]),
            description = ""
        });
    }
    baseIndex += numObjects * 2;


    for (int i = 0; i < numVerbs; i++)
    {
        game.messages.Add(seqData[baseIndex + i * 9 + 2]);
    }

    var json = System.Text.Json.JsonSerializer.Serialize(game, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(outFile, json);
    Console.WriteLine($"Converted '{file}' to '{outFile}'.");
}

#endregion

#region functions
byte GetByte(string data)
{
    int x = Convert.ToInt32(data);
    return x >= 0 ? (byte)x : (byte)255;
}
#endregion

#region Help Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: SeqConverter [OPTIONS] FILES");
    Console.WriteLine("Convert SEQ Files to JSON");
    Console.WriteLine();
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
}

void ShowBanner() => Console.WriteLine("Casasoft SeqConverter v1.0\ncopyright (c) 2026 Roberto Ceccarelli - Casasoft\n");
#endregion


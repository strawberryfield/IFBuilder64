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

using Casasoft.IFBuilder64.Builder;
using Casasoft.Helpers;
using Casasoft.IFBuilder64.Common;
using Casasoft.IFBuilder64.Json;
using Mono.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

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

    BuildResult result = new GameBuilder().Build(game, outputFile);
    foreach (string line in result.Log)
    {
        Console.WriteLine(line);
    }
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

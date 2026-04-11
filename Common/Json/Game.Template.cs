//-----------------------------------------------------------------------
// <copyright file="Game.Template.cs" company="Casasoft">
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
/// Represents the supported template languages for creating an initial game template.
/// </summary>
public enum TemplateLanguage
{
    /// <summary>
    /// English language templates.
    /// </summary>
    English,

    /// <summary>
    /// Italian language templates (Italiano).
    /// </summary>
    Italiano
}

/// <summary>
/// Partial representation of a game template used for initialization and loading template resources such as directions,
/// messages and verbs.
/// </summary>
/// <remarks>
/// This partial <see cref="Game"/> class provides constructors and helper methods used when creating a new game from a
/// language-specific template. Template resources are read from the on-disk "templates" directory and include:
/// - direction names (plain text file),
/// - message lines (JSON array of strings),
/// - verb definitions (JSON array of <see cref="Verb"/>).
///
/// The methods in this class populate the internal collections such as <c>rooms</c>, <c>verbs</c>, <c>items</c>,
/// <c>messages</c>, <c>directions</c> and <c>doors</c>. Callers should handle IO and JSON exceptions when creating a
/// new instance from a template language.
/// </remarks>
public partial class Game
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Game"/> class.
    /// </summary>
    /// <remarks>
    /// This parameterless constructor sets default, empty values for all internal collections and string fields.
    /// It is safe to use from serializers and other consumers that expect a default constructible type.
    /// </remarks>
    public Game()
    {
        intro = string.Empty;
        rooms = new();
        verbs = new();
        items = new();
        messages = new();
        usermessages = new();
        directions = new();
        doors = new();
        initialroom = string.Empty;
    }

     /// <summary>
    /// Initializes a new instance of the <see cref="Game"/> class using the specified template language.
    /// </summary>
    /// <param name="language">The template language to use when loading template resources.</param>
    /// <remarks>
    /// The constructor performs these high-level steps:
    /// - initializes default collections by calling the parameterless constructor,
    /// - creates a default player room entry in <c>rooms</c>,
    /// - loads direction names, localized messages and verbs for the chosen language,
    /// - adds a localized starter item (the "me" item) located at the "player" room.
    ///
    /// The exact template files used depend on <paramref name="language"/> and follow the
    /// naming convention described in the loader methods (for example,
    /// "templates/messages.en.json" for English).
    /// </remarks>
    public Game(TemplateLanguage language) : this()
    {
        rooms = [new Room { description = string.Empty, name = "player" }];
        LoadDirections(language);
        LoadMessages(language);
        LoadVerbs(language);
        switch (language)
        {
            case TemplateLanguage.English:
                items.Add(new Item { name = "me", description = "As good-looking as ever.", location = "player" });
                break;
            case TemplateLanguage.Italiano:
                items.Add(new Item { name = "me", description = "In gran forma, come sempre.", location = "player" });
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Loads message lines for the specified template language from the templates directory and stores them in the
    /// <c>messages</c> collection.
    /// </summary>
    /// <param name="language">The template language whose messages should be loaded.</param>
    /// <remarks>
    /// The filename is built as "templates/messages.{xx}.json" where {xx} is the two-letter lowercase language code
    /// derived from the <see cref="TemplateLanguage"/> value. The file is expected to contain a JSON array of strings,
    /// for example:
    /// <code>
    /// [ "You can't go that way.", "I don't understand." ]
    /// </code>
    /// After reading the file the method deserializes the JSON into <see cref="List{String}"/> and assigns it to the
    /// internal <c>messages</c> collection.
    /// </remarks>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the messages file does not exist.</exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the templates directory cannot be found.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the messages file is denied.</exception>
    /// <exception cref="System.IO.IOException">Thrown when the messages file cannot be read.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the file contents cannot be parsed as a JSON array of strings.</exception>
    protected void LoadMessages(TemplateLanguage language)
    {
        string filename = "templates/messages." + language.ToString().ToLower().Substring(0, 2) + ".json";
        string jsondata = File.ReadAllText(filename);
        messages = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsondata) ?? [];
    }

    /// <summary>
    /// Loads direction names for the specified template language from the templates directory and stores them in the
    /// <see cref="directions"/> field.
    /// </summary>
    /// <param name="language">The template language whose directions should be loaded.</param>
    /// <remarks>
    /// The filename is built as "templates/directions.{xx}.txt" where {xx} is the two-letter lowercase language code
    /// derived from the <see cref="TemplateLanguage"/> value. The file is expected to contain one direction name per
    /// line (for example "north", "south", ...). The resulting list is used to build directional verbs.
    /// </remarks>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the directions file does not exist.</exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the templates directory cannot be found.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the directions file is denied.</exception>
    /// <exception cref="System.IO.IOException">Thrown when the directions file cannot be read.</exception>
    protected void LoadDirections(TemplateLanguage language)
    {
        string filename = "templates/directions." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        directions = File.ReadAllLines(filename).ToList();
    }

    /// <summary>
    /// Loads verb definitions from the verbs template file for the specified language and appends them to the
    /// <c>verbs</c> collection.
    /// </summary>
    /// <param name="language">The template language whose verbs should be loaded.</param>
    /// <remarks>
    /// The filename is built as "templates/verbs.{xx}.json" where {xx} is the two-letter lowercase language code
    /// derived from the <see cref="TemplateLanguage"/> value. The file is expected to contain a JSON array of
    /// <see cref="Verb"/> objects, for example:
    /// <code>
    /// [ { "command": "take", "action": 1 }, { "command": "drop", "action": 2 } ]
    /// </code>
    /// After reading and deserializing the file the method assigns the result to the internal <c>verbs</c> collection.
    /// </remarks>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when the verbs file does not exist.</exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the templates directory cannot be found.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the verbs file is denied.</exception>
    /// <exception cref="System.IO.IOException">Thrown when the verbs file cannot be read.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the file contents cannot be parsed as a JSON array of <see cref="Verb"/>.</exception>
    protected void LoadVerbs(TemplateLanguage language)
    {
        string filename = "templates/verbs." + language.ToString().ToLower().Substring(0, 2) + ".json";
        string jsondata = File.ReadAllText(filename);
        verbs = System.Text.Json.JsonSerializer.Deserialize<List<Verb>>(jsondata) ?? [];
    }
}

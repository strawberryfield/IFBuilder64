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
public partial class Game
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Game"/> class. Default parameterless constructor; may be used by
    /// serializers or other consumers.
    /// </summary>
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
    /// Initializes a new instance of the <see cref="Game"/> class using the specified template language. The
    /// constructor initializes internal collections and loads language-specific templates (rooms, directions, items,
    /// messages and verbs).
    /// </summary>
    /// <param name="language">The template language to use when loading template resources.</param>
    public Game(TemplateLanguage language) : this()
    {
        rooms = [new Room { description = string.Empty, name = "player" }];
        LoadDirections(language);
        verbs = VerbsFromDirections();
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
    /// The filename is built as "templates/messages.{xx}.txt" where {xx} is the two-letter lowercase language code
    /// derived from the <see cref="TemplateLanguage"/> value.
    /// </remarks>
    /// <exception cref="System.IO.IOException">Thrown when the messages file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the messages file is denied.</exception>
    protected void LoadMessages(TemplateLanguage language)
    {
        string filename = "templates/messages." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        messages = File.ReadAllLines(filename).ToList();
    }

    /// <summary>
    /// Loads direction names for the specified template language from the templates directory and stores them in the
    /// <see cref="directions"/> field.
    /// </summary>
    /// <param name="language">The template language whose directions should be loaded.</param>
    /// <remarks>
    /// The filename is built as "templates/directions.{xx}.txt" where {xx} is the two-letter lowercase language code
    /// derived from the <see cref="TemplateLanguage"/> value.
    /// </remarks>
    /// <exception cref="System.IO.IOException">Thrown when the directions file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the directions file is denied.</exception>
    protected void LoadDirections(TemplateLanguage language)
    {
        string filename = "templates/directions." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        directions = File.ReadAllLines(filename).ToList();
    }

    /// <summary>
    /// Builds a list of directional verbs from the currently loaded <see cref="directions"/> array. For each direction
    /// it adds two <see cref="Verb"/> entries: one for the single-letter shorthand and one for the full direction name.
    ///
    /// </summary>
    /// <returns>A list of <see cref="Verb"/> objects representing directional commands.</returns>
    protected List<Verb> VerbsFromDirections()
    {
        List<Verb> ret = new();
        for (byte i = 0; i < directions.Count; i++)
        {
            ret.Add(new Verb { command = directions[i].Substring(0, 1), action = i });
            ret.Add(new Verb { command = directions[i], action = i });
        }
        return ret;
    }

    /// <summary>
    /// Loads verb definitions from the verbs template file for the specified language and appends them to the
    /// <c>verbs</c> collection.
    /// </summary>
    /// <param name="language">The template language whose verbs should be loaded.</param>
    /// <remarks>
    /// Verb file lines are expected in the format "&lt;action&gt;;&lt;command&gt;". The method converts the action
    /// portion to a byte for the <see cref="Verb.action"/> field.
    /// </remarks>
    /// <exception cref="System.IO.IOException">Thrown when the verbs file cannot be read.</exception>
    /// <exception cref="System.FormatException">Thrown when a verb action value cannot be parsed as a byte.</exception>
    /// <exception cref="System.OverflowException">Thrown when a parsed verb action is outside the range of a byte.</exception>
    protected void LoadVerbs(TemplateLanguage language)
    {
        string filename = "templates/verbs." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        string[] verbsLines = File.ReadAllLines(filename);
        foreach (var line in verbsLines)
        {
            string[] verb = line.Split(';');
            verbs.Add(new Verb { command = verb[1], action = Convert.ToByte(verb[0]) });
        }
    }
}

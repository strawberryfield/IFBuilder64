//-----------------------------------------------------------------------
// <copyright file="IFiction.cs" company="Casasoft">
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

using System.Reflection;
using System.Xml.Linq;

namespace Casasoft.IFBuilder64.Common.Json;

/// <summary>
/// Represents the metadata for an interactive fiction work used for JSON
/// (de)serialization within the application.
/// </summary>
/// <remarks>
/// This class maps common descriptive fields for an interactive fiction
/// (title, author, identifiers and optional metadata). Some properties provide
/// sensible defaults when the value is omitted in incoming JSON (see
/// <see cref="headline"/> and <see cref="genre"/>).
/// </remarks>
public class IFiction
{
    /// <summary>
    /// The title of the interactive fiction.
    /// This property is required when creating or deserializing an <see cref="IFiction"/>.
    /// </summary>
    public required string title { get; set; }

    /// <summary>
    /// The primary author of the interactive fiction.
    /// This property is required when creating or deserializing an <see cref="IFiction"/>.
    /// </summary>
    public required string author { get; set; }

    /// <summary>
    /// Optional IFID (Interactive Fiction ID) that uniquely identifies the work.
    /// </summary>
    public string? ifid { get; set; }

    /// <summary>
    /// Backing field for <see cref="headline"/>.
    /// </summary>
    private string? _headline;

    /// <summary>
    /// Gets or sets the headline (short subtitle) for the interactive fiction.
    /// When getting, if no explicit value has been set this property returns
    /// the default value "An interactive fiction".
    /// </summary>
    /// <remarks>
    /// The property uses a private backing field (<see cref="_headline"/>)
    /// so callers receive a sensible default when the headline is omitted in JSON.
    /// </remarks>
    public string headline
    {
        get => _headline ?? "An interactive fiction";
        set => _headline = value;
    }

    /// <summary>
    /// Backing field for <see cref="genre"/>.
    /// </summary>
    private string? _genre;

    /// <summary>
    /// The genre of the interactive fiction.
    /// Returns "Fiction" when no explicit genre has been set.
    /// </summary>
    public string genre
    {
        get => _genre ?? "Fiction";
        set => _genre = value;
    }

    /// <summary>
    /// Optional year when the work was first published (calendar year).
    /// </summary>
    public int? firstpublished { get; set; }

    /// <summary>
    /// Optional version number for the work or its metadata.
    /// </summary>
    public int? version { get; set; }

    /// <summary>
    /// Optional longer description or synopsis of the interactive fiction.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// Optional language identifier (for example "en" or "en-US").
    /// </summary>
    public string? language { get; set; }

    /// <summary>
    /// Optional URL related to the work (project page, author page, repository, etc.).
    /// </summary>
    public string? url { get; set; }

    /// <summary>
    /// Optional contact email for the author or project.
    /// </summary>
    public string? email { get; set; }

    /// <summary>
    /// Build XML following the Babel iFiction/iFIndex structure like the example:
    /// <ifindex version="1.0" xmlns="http://babel.ifarchive.org/protocol/iFiction/">
    ///   <story>
    ///     <identification>...</identification>
    ///     <bibliographic>...</bibliographic>
    ///     ...
    ///   </story>
    /// </ifindex>
    /// </summary>
    /// <returns></returns>
    public XElement ToIFictionXml()
    {
        XNamespace ns = XNamespace.Get("http://babel.ifarchive.org/protocol/iFiction/");

        XElement root = new(ns + "ifindex",
            new XAttribute("version", "1.0")
        );

        XElement story = new(ns + "story");
        root.Add(story);

        // identification (ifid, format if available in future)
        XElement identification = new(ns + "identification");
        if (!string.IsNullOrWhiteSpace(ifid))
            identification.Add(new XElement(ns + "ifid", ifid));

        // only add identification if we produced any children
        if (identification.HasElements)
            story.Add(identification);

        // bibliographic in the order commonly used by iFiction
        XElement bibliographic = new(ns + "bibliographic",
            new XElement(ns + "title", title),
            new XElement(ns + "author", author),
            new XElement(ns + "headline", headline),
            new XElement(ns + "genre", genre)
        );

        if (firstpublished.HasValue)
            bibliographic.Add(new XElement(ns + "firstpublished", firstpublished.Value));

        if (!string.IsNullOrWhiteSpace(description))
        {
            // Convert HTML <br/> variants to newlines and store the whole text in a CDATA section.
            XElement descElem = new XElement(ns + "description");
            var normalized = description.Replace("\r\n", "\n");
            normalized = normalized.Replace("<br/>", "\n").Replace("<br />", "\n").Replace("<br>", "\n");
            descElem.Add(new XCData(normalized));
            bibliographic.Add(descElem);
        }

        if (!string.IsNullOrWhiteSpace(language))
            bibliographic.Add(new XElement(ns + "language", language));

        // Add bibliographic even if it contains only required fields.
        story.Add(bibliographic);

        // Additional sections (resources, releases, cover, etc.) are not present in this model.
        // They can be added in future if the data model is extended.
        string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
       
        return root;
    }
}

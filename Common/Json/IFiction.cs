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

    public System.Xml.Linq.XElement ToIFictionXml()
    {
        var root = new System.Xml.Linq.XElement("ifiction",
            new System.Xml.Linq.XElement("title", title),
            new System.Xml.Linq.XElement("author", author)
        );

        if (!string.IsNullOrWhiteSpace(ifid))
            root.Add(new System.Xml.Linq.XElement("ifid", ifid));

        // include defaults provided by the properties
        root.Add(new System.Xml.Linq.XElement("headline", headline));
        root.Add(new System.Xml.Linq.XElement("genre", genre));

        if (firstpublished.HasValue)
            root.Add(new System.Xml.Linq.XElement("firstpublished", firstpublished.Value));

        if (version.HasValue)
            root.Add(new System.Xml.Linq.XElement("version", version.Value));

        if (!string.IsNullOrWhiteSpace(description))
            root.Add(new System.Xml.Linq.XElement("description", new System.Xml.Linq.XCData(description)));

        if (!string.IsNullOrWhiteSpace(language))
            root.Add(new System.Xml.Linq.XElement("language", language));

        if (!string.IsNullOrWhiteSpace(url))
            root.Add(new System.Xml.Linq.XElement("url", url));

        if (!string.IsNullOrWhiteSpace(email))
            root.Add(new System.Xml.Linq.XElement("email", email));

        return root;
    }
}

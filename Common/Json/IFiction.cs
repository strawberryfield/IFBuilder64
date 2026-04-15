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

using System.Xml.Linq;
using System.ComponentModel;

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
[Description("Metadata for an interactive fiction work used for JSON (de)serialization.")]
public class IFiction
{
    /// <summary>
    /// The title of the interactive fiction.
    /// This property is required when creating or deserializing an <see cref="IFiction"/>.
    /// </summary>
    [Description("The title of the interactive fiction.")]
    public required string title { get; set; }

    /// <summary>
    /// The primary author of the interactive fiction.
    /// This property is required when creating or deserializing an <see cref="IFiction"/>.
    /// </summary>
    [Description("The primary author of the interactive fiction.")]
    public required string author { get; set; }

    /// <summary>
    /// Optional IFID (Interactive Fiction ID) that uniquely identifies the work.
    /// </summary>
    [Description("Optional IFID (Interactive Fiction ID) that uniquely identifies the work.")]
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
    [Description("Short subtitle for the work; defaults to 'An interactive fiction' when omitted.")]
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
    [Description("The genre of the work; defaults to 'Fiction' when omitted.")]
    public string genre
    {
        get => _genre ?? "Fiction";
        set => _genre = value;
    }

    /// <summary>
    /// Optional year when the work was first published (calendar year).
    /// </summary>
    [Description("Optional year when the work was first published (calendar year).")]
    public int? firstpublished { get; set; }

    /// <summary>
    /// Optional version number for the work or its metadata.
    /// </summary>
    [Description("Optional version number for the work or its metadata.")]
    public int? version { get; set; }

    /// <summary>
    /// Optional longer description or synopsis of the interactive fiction.
    /// </summary>
    [Description("Optional longer description or synopsis of the interactive fiction.")]
    public string? description { get; set; }

    /// <summary>
    /// Optional language identifier (for example "en" or "en-US").
    /// </summary>
    [Description("Optional language identifier (e.g., 'en' or 'en-US').")]
    public string? language { get; set; }

    /// <summary>
    /// Optional URL related to the work (project page, author page, repository, etc.).
    /// </summary>
    [Description("Optional URL related to the work (project page, author page, repository, etc.).")]
    public string? url { get; set; }

    /// <summary>
    /// Optional contact email for the author or project.
    /// </summary>
    [Description("Optional contact email for the author or project.")]
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
    public XElement ToIFictionXml(string builderName = "CASASOFT IFBuilder", string builderVersion = "")
    {
        XNamespace ns = XNamespace.Get("http://babel.ifarchive.org/protocol/iFiction/");

        XElement root = new(ns + "ifindex",
            new XAttribute("version", "1.0")
        );

        XElement story = new(ns + "story");
        root.Add(story);

        // identification (ifid, format if available in future)
        XElement identification = new(ns + "identification",
            new XElement(ns + "format", "executable")
        );
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

        string today = DateTime.Now.Date.ToString("yyyy-MM-dd");
        XElement release = new(ns + "releases",
            new XElement(ns + "attached",
                new XElement(ns + "release",
                    new XElement(ns + "releasedate", today),
                    new XElement(ns + "version", version.HasValue ? version.Value.ToString() : "1"),
                    new XElement(ns + "compiler", builderName.Trim()),
                    new XElement(ns + "compilerversion", builderVersion.Trim())
                )
            )
        );
        story.Add(release);

        XElement colophon = new(ns + "colophon",
            new XElement(ns + "generator", builderName.Trim()),
            new XElement(ns + "generatorversion", builderVersion.Trim()),
            new XElement(ns + "originated", today)
        );
        story.Add(colophon);

        if (!string.IsNullOrWhiteSpace(url) || !string.IsNullOrWhiteSpace(email))
        {
            XElement contact = new(ns + "contacts");
            if (!string.IsNullOrWhiteSpace(url))
                contact.Add(new XElement(ns + "url", url));
            if (!string.IsNullOrWhiteSpace(email))
                contact.Add(new XElement(ns + "authoremail", email));
            story.Add(contact);
        }

        XElement executable = new(ns + "executable",
            new XElement(ns + "compiler", builderName.Trim()),
            new XElement(ns + "compilerversion", builderVersion.Trim()),
            new XElement(ns + "serial", DateTime.Now.Date.ToString("yyyyMMdd"))
        );
        story.Add(executable);

        return root;
    }
}

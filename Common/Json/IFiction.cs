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
/// Represents metadata for an interactive fiction work used by the application's
/// JSON (de)serialization and export routines.
/// </summary>
/// <remarks>
/// Instances of <see cref="IFiction"/> encapsulate common descriptive fields
/// (title, author, identifiers and optional metadata) that are serialized to
/// and from JSON input used by the application. Two properties provide
/// sensible defaults when omitted in incoming JSON: <see cref="headline"/>
/// defaults to "An interactive fiction" and <see cref="genre"/> defaults to
/// "Fiction".
///
/// The <see cref="ToIFictionXml(string,string)"/> method converts the stored
/// metadata into an XML structure that follows the Babel iFiction / iFIndex
/// conventions (root element <c>ifindex</c> in the namespace
/// "http://babel.ifarchive.org/protocol/iFiction/"). This XML is used when
/// producing iFiction-compatible metadata files.
/// </remarks>
[Description("Metadata for an interactive fiction work used for JSON (de)serialization.")]
public class IFiction
{
    /// <summary>
    /// The title of the interactive fiction.
    /// </summary>
    /// <remarks>
    /// This property is required for valid metadata. The JSON (de)serializer
    /// and compiler logic expect a non-empty value for <see cref="title"/>
    /// </remarks>
    [Description("The title of the interactive fiction.")]
    public required string title { get; set; }

    /// <summary>
    /// The primary author of the interactive fiction.
    /// </summary>
    /// <remarks>
    /// This property is required for valid metadata. Use the author's full
    /// name or pseudonym as appropriate for display and bibliographic output.
    /// </remarks>
    [Description("The primary author of the interactive fiction.")]
    public required string author { get; set; }

    /// <summary>
    /// Optional IFID (Interactive Fiction ID) that uniquely identifies the work.
    /// </summary>
    /// <remarks>
    /// When present this value is emitted into the iFiction
    /// <c>&lt;identification&gt;</c> element as <c>&lt;ifid&gt;</c>.
    /// </remarks>
    [Description("Optional IFID (Interactive Fiction ID) that uniquely identifies the work.")]
    public string? ifid { get; set; }

    /// <summary>
    /// Backing field for <see cref="headline"/>.
    /// </summary>
    private string? _headline;

    /// <summary>
    /// Short subtitle or tagline for the work.
    /// </summary>
    /// <remarks>
    /// If no explicit value is provided, the getter returns the default text
    /// "An interactive fiction" so downstream consumers always receive a
    /// non-empty headline when generating bibliographic output.
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
    /// </summary>
    /// <remarks>
    /// Returns "Fiction" when no explicit genre has been set. This value is
    /// included in the iFiction <c>&lt;genre&gt;</c> bibliographic element.
    /// </remarks>
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
    /// <remarks>
    /// When converted to iFiction XML the description is placed inside a
    /// CDATA section and common HTML line-break variants (&lt;br&gt;, &lt;br/&gt;, &lt;br /&gt;)
    /// are normalized to newlines so the textual synopsis remains human-readable.
    /// </remarks>
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
    /// Build an XML document following the Babel iFiction / iFIndex structure.
    /// </summary>
    /// <param name="builderName">
    /// The name of the builder or generator to include in the output (emitted
    /// in <c>&lt;compiler&gt;</c> and <c>&lt;generator&gt;</c> fields). If
    /// empty or whitespace it is still trimmed before inclusion.
    /// </param>
    /// <param name="builderVersion">
    /// The version string for the builder/generator (emitted in
    /// <c>&lt;compilerversion&gt;</c> and <c>&lt;generatorversion&gt;</c>).
    /// </param>
    /// <returns>
    /// An <see cref="XElement"/> representing the root <c>ifindex</c> element
    /// (including nested <c>story</c>, <c>identification</c>, <c>bibliographic</c>,
    /// <c>releases</c>, <c>colophon</c>, and other standard iFiction child elements).
    /// The returned element uses the namespace "http://babel.ifarchive.org/protocol/iFiction/".
    /// </returns>
    /// <remarks>
    /// - Required fields (<see cref="title"/> and <see cref="author"/>) are always
    ///   emitted inside the <c>&lt;bibliographic&gt;</c> element.
    /// - Optional fields are added only when non-empty (for strings) or present
    ///   (for nullable numeric values).
    /// - The <c>&lt;description&gt;</c> element, when present, is emitted as
    ///   CDATA with normalized newlines.
    /// - The method sets sensible defaults for <c>&lt;version&gt;</c> (defaults to "1")
    ///   and date fields (uses the current local date).
    /// </remarks>
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

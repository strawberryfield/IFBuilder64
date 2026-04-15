//-----------------------------------------------------------------------
// <copyright file="Helpers.cs" company="Casasoft">
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

using Casasoft.IFBuilder64.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Casasoft.IFBuilder64.Common;

/// <summary>
/// Provides general helper utilities used across the application.
/// </summary>
/// <remarks>
/// This class currently exposes methods that assist with template language resolution.
/// Additional helper methods may be added here as needed.
/// </remarks>
public static class Helpers
{
    /// <summary>
    /// Converts a language identifier string into the corresponding <see cref="TemplateLanguage"/> value.
    /// </summary>
    /// <param name="langStr">
    /// A language identifier string. Supported values (case-insensitive) are:
    /// "italiano", "italian", "it" (maps to <see cref="TemplateLanguage.Italiano"/>)
    /// and "inglese", "english", "en" (maps to <see cref="TemplateLanguage.English"/>).
    /// </param>
    /// <returns>
    /// The <see cref="TemplateLanguage"/> value that corresponds to the provided <paramref name="langStr"/>.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="langStr"/> does not match any supported language identifier.
    /// </exception>
    /// <exception cref="System.NullReferenceException">
    /// Thrown when <paramref name="langStr"/> is <c>null</c> because the method calls <c>ToLower()</c> on the input.
    /// Callers should ensure a non-null string is provided.
    /// </exception>
    /// <remarks>
    /// The comparison is performed using <c>langStr.ToLower()</c> and exact string matches against known identifiers.
    /// This method intentionally preserves current behaviour and does not perform culture-specific normalization.
    /// </remarks>
    public static TemplateLanguage GetLanguage(string langStr)
    {
        switch (langStr.ToLower())
        {
            case "italiano":
            case "italian":
            case "it":
                return TemplateLanguage.Italiano;
            case "inglese":
            case "english":
            case "en":
                return TemplateLanguage.English;
            default:
                throw new ArgumentException($"Unknown template language '{langStr}'. Supported languages are 'english' and 'italiano'.");
        }
    }

    /// <summary>
    /// Provides a preconfigured <see cref="JsonSchemaExporterOptions"/> that injects
    /// <see cref="DescriptionAttribute"/> values into generated JSON Schema nodes.
    /// </summary>
    /// <remarks>
    /// The returned options set <see cref="JsonSchemaExporterOptions.TransformSchemaNode"/>
    /// to a delegate that:
    /// - determines the attribute provider from the current context (property or type),
    /// - searches for a <see cref="DescriptionAttribute"/> (including inherited attributes),
    /// - if found, ensures the schema node is represented as a <see cref="JsonObject"/>; when the
    ///   original schema is a boolean schema it is wrapped into an object and a <c>{"not": true}</c>
    ///   entry is added for a <see cref="JsonValueKind.False"/> boolean schema to preserve semantics,
    /// - inserts a "description" property containing the attribute's <see cref="DescriptionAttribute.Description"/>
    ///   as the first property in the resulting object.
    ///
    /// This behavior is intended for use with System.Text.Json-based schema generation and preserves
    /// existing behavior for boolean schemas and description injection.
    /// </remarks>
    /// <value>
    /// A <see cref="JsonSchemaExporterOptions"/> instance with the <see cref="JsonSchemaExporterOptions.TransformSchemaNode"/>
    /// delegate configured to apply <see cref="DescriptionAttribute"/> values to schema output.
    /// </value>
    public static JsonSchemaExporterOptions GetJsonSchemaDescriptions => new()
    {
        TransformSchemaNode = (context, schema) =>
        {
            // Determine if a type or property and extract the relevant attribute provider.
            ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
                ? context.PropertyInfo.AttributeProvider
                : context.TypeInfo.Type;

            // Look up any description attributes.
            DescriptionAttribute? descriptionAttr = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DescriptionAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply description attribute to the generated schema.
            if (descriptionAttr != null)
            {
                if (schema is not JsonObject jObj)
                {
                    // Handle the case where the schema is a Boolean.
                    JsonValueKind valueKind = schema.GetValueKind();
                    Debug.Assert(valueKind is JsonValueKind.True or JsonValueKind.False);
                    schema = jObj = new JsonObject();
                    if (valueKind is JsonValueKind.False)
                    {
                        jObj.Add("not", true);
                    }
                }

                jObj.Insert(0, "description", descriptionAttr.Description);
            }

            return schema;
        }
    };
}

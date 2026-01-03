using Casasoft.IFBuilder64.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Casasoft.IFBuilder64.Common;

public static class Helpers
{
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
}

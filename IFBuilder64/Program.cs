using IFBuilder64.Json;
using Casasoft.Commodore;
using Casasoft.Helpers;
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

if (ShouldShowHelp || FilesList.Count == 0)
{
    ShowHelp();
    return;
}

//
// template creation
//
if (!string.IsNullOrEmpty(TemplateType))
{
    var lang = TemplateLanguage.English;
    switch (TemplateType.ToLower())
    {
        case "italiano":
        case "italian":
        case "it":
            lang = TemplateLanguage.Italiano;
            break;
        case "inglese":
        case "english":
        case "en":
            lang = TemplateLanguage.English;
            break;
        default:
            Console.WriteLine($"Unknown template language '{TemplateType}'. Supported languages are 'english' and 'italiano'.");
            return;
    }

    if (string.IsNullOrWhiteSpace(OutputFile))
    {
        OutputFile = "game.json";
    }
    CreateTemplate(lang);
    Console.WriteLine($"Template file '{OutputFile}' created.");
    return;
}
#endregion

#region template
void CreateTemplate(TemplateLanguage lang)
{
    var game = new Game(TemplateLanguage.English);

    var json = System.Text.Json.JsonSerializer.Serialize(game, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("game.json", json);
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
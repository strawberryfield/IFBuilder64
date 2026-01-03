using Casasoft.Commodore;
using Casasoft.Helpers;
using Casasoft.IFBuilder64.Json;
using Mono.Options;
using System.Text;

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

if (ShouldShowHelp)
{
    ShowHelp();
    return;
}

TemplateLanguage lang = TemplateLanguage.English; // default language
if (!string.IsNullOrEmpty(TemplateType))
{
    lang = Casasoft.IFBuilder64.Common.Helpers.GetLanguage(TemplateType);
}

foreach (var file in FilesList)
{
    string outFile = OutputFile;
    if (string.IsNullOrWhiteSpace(outFile))
    {
        outFile = Path.ChangeExtension(file, ".json");
    }

    Game game = new(lang);
    string input = Charset.ASCII(File.ReadAllBytes(file), true);
    string[] seqData = input.Split("\r");  

    game.intro = seqData[0];

    var json = System.Text.Json.JsonSerializer.Serialize(game, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(outFile, json);
    Console.WriteLine($"Converted '{file}' to '{outFile}'.");
}

#endregion

#region functions

#endregion

#region Help Procedures
void ShowHelp()
{
    Console.WriteLine("Usage: SeqConverter [OPTIONS] FILES");
    Console.WriteLine("Convert SEQ Files to JSON");
    Console.WriteLine();
    Console.WriteLine("Options:");
    p.WriteOptionDescriptions(Console.Out);
}

void ShowBanner() => Console.WriteLine("Casasoft SeqConverter v1.0\ncopyright (c) 2026 Roberto Ceccarelli - Casasoft\n");
#endregion


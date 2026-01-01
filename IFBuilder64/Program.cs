using Casasoft.Commodore;
using Casasoft.Helpers;
using IFBuilder64.Json;
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

#region build game
if (FilesList.Count > 0)
{
    var outputFile = OutputFile;
    foreach (var inputFile in FilesList)
    {
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            outputFile = Path.ChangeExtension(inputFile, "");
        }
        BuildGame(inputFile, outputFile);
    }
    return;
}
#endregion

#region no command specified
ShowHelp();
return;
#endregion

#region template
void CreateTemplate(TemplateLanguage lang)
{
    var game = new Game(lang);

    var json = System.Text.Json.JsonSerializer.Serialize(game, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("game.json", json);
}
#endregion

#region build
void BuildGame(string inputFile, string outputFile)
{
    Console.WriteLine($"Building '{inputFile}' to '{outputFile}'...");
    var jsonText = File.ReadAllText(inputFile);
    var game = System.Text.Json.JsonSerializer.Deserialize<Game>(jsonText);
    if (game == null)
    {
        Console.WriteLine($"Error: Unable to parse game file '{inputFile}'.");
        return;
    }

    ushort topAddress = 0xbfff;

    // intro
    string text = Charset.PETSCII(game.intro, true) + (char)0;
    int size = text.Length + 2;
    ushort introAddress = (ushort)(topAddress - size + 1);
    PrgFile prg = new(introAddress, Encoding.ASCII.GetBytes(text));
    string filename = outputFile+".in.prg";
    prg.Save(filename);
    Console.WriteLine($"Build intro: '{outputFile}'.");
    topAddress = (ushort)(introAddress - 1);

    // messages
    (byte[] msgPointers, byte[] msgStringsList) = Strings2Prg.CreateArrays(game.messages, 0, true);
    size = msgPointers.Length + msgStringsList.Length;
    ushort messagesAddress = (ushort)(topAddress - size + 1);
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
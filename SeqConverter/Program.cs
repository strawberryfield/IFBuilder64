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


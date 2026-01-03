namespace Casasoft.IFBuilder64.Json;

public enum TemplateLanguage
{
    English,
    Italiano
}

public partial class Game
{
    public Game()
    {
    }

    protected string[] directions;

    public Game(TemplateLanguage language)
    {
        intro = string.Empty;
        verbs = new();
        items = new();
        messages = new();
        switch (language)
        {
            case TemplateLanguage.English:
                rooms =
                [
                    new Room
                    {
                        description = "You look good",
                        north = 0,
                        south = 0,
                        east = 0,
                        west = 0,
                        up = 0,
                        down = 0
                    }
                ];
                LoadDirections(language);
                verbs = VerbsFromDirections();
                items = ItemsFromDirections();
                LoadMessages(language);
                LoadVerbs(language);
                break;
            case TemplateLanguage.Italiano:
                rooms =
                [
                    new Room
                    {
                        description = "In gran forma, come sempre.",
                        north = 0,
                        south = 0,
                        east = 0,
                        west = 0,
                        up = 0,
                        down = 0
                    }
                ];
                LoadDirections(language);
                verbs = VerbsFromDirections();
                items = ItemsFromDirections();
                LoadMessages(language);
                LoadVerbs(language);
                break;
            default:
                break;
        }
    }

    protected void LoadMessages(TemplateLanguage language)
    {
        string filename = "templates/messages." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        messages = File.ReadAllLines(filename).ToList();
    }

    protected void LoadDirections(TemplateLanguage language)
    {
        string filename = "templates/directions." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        directions = File.ReadAllLines(filename);
    }

    protected List<Verb> VerbsFromDirections()
    {
        List<Verb> ret = new();
        for (byte i = 0; i < directions.Length; i++)
        {
            ret.Add(new Verb { command = directions[i].Substring(0, 1), action = i });
            ret.Add(new Verb { command = directions[i], action = i });
        }
        return ret;
    }

    protected List<Item> ItemsFromDirections()
    {
        List<Item> ret = new();
        foreach (var dir in directions)
        {
            ret.Add(new Item { name = dir, description = string.Empty, location = 255 });
        }
        return ret;
    }

    protected void LoadVerbs(TemplateLanguage language)
    {
        string filename = "templates/verbs." + language.ToString().ToLower().Substring(0, 2) + ".txt";
        string[] verbsLines = File.ReadAllLines(filename);
        foreach (var line in verbsLines)
        {
            string[] verb = line.Split(';');
            verbs.Add(new Verb { command = verb[1], action = Convert.ToByte(verb[0]) });
        }
    }
}

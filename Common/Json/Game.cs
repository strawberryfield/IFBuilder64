namespace Casasoft.IFBuilder64.Json;

public partial class Game
{
    public string intro { get; set; }
    public List<Room> rooms { get; set; }
    public List<Verb> verbs { get; set; }
    public List<Item> items { get; set; }
    public List<string> messages { get; set; }
}

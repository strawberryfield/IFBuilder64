# Editor grafico Avalonia — Piano di implementazione

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Un'app desktop Avalonia che crea e modifica i `game.json` di IFBuilder64 (mappa, porte, oggetti, verbi, messaggi, metadati) e li compila con lo stesso risultato byte per byte della CLI.

**Architecture:** Il codice di compilazione esce da `IFBuilder64/Program.cs` in una class library `Builder` usata sia dalla CLI (ridotta a guscio) sia dall'editor. L'editor è MVVM (CommunityToolkit.Mvvm): un `DocumentoViewModel` mappa `Game` nei due sensi; la logica (rinomini, collegamenti/porte, disposizione, validazione) sta in classi senza dipendenze da Avalonia, testate in xUnit; le viste sono sottili. La mappa è un `Control` disegnato a mano con la geometria isolata in una classe pura.

**Tech Stack:** .NET 10, Avalonia 12.1.1 (Fluent), CommunityToolkit.Mvvm, Avalonia.Controls.DataGrid, xUnit + Avalonia.Headless.XUnit, System.Text.Json, Casasoft.Commodore 1.0.2.

**Spec:** `docs/superpowers/specs/2026-10-30-editor-grafico-avalonia-design.md`

## Global Constraints

- Target `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` in ogni nuovo progetto.
- `AssemblyName`/`RootNamespace` = `Casasoft.IFBuilder64.$(MSBuildProjectName)` come in `Common/Common.csproj`.
- Intestazione di copyright dal `file_header_template` di `.editorconfig` su ogni `.cs` (copiare il blocco di `IFBuilder64/ConfigPrg.cs`, cambiando `file=`).
- Solo dipendenze con licenza compatibile (A)GPL; nessun pacchetto Avalonia Accelerate o commerciale.
- Tutti i pacchetti Avalonia alla stessa versione: **12.1.1** (già in cache NuGet locale).
- Codice nuovo dell'editor: identificatori, commenti e stringhe UI in italiano; il codice spostato da `Program.cs` mantiene i nomi attuali (`BuildGame`, `AllocateDown`, `ConfigPrg`…).
- Indici validi 0–254; **255** = "non trovato".
- Codici azione 0–25 (§5.4 della spec).
- Il Builder non scrive mai su `Console`.
- Fuori ambito v1: annulla/ripeti, hook BASIC, VICE, import SEQ, self-contained/installer.
- Messaggi di commit in italiano, terminati con `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Porta aperta all'inizio.** Il manuale (riga 435) richiede che una porta con `"open": true` abbia le uscite dichiarate *anche* nelle due stanze, perché il motore non le scrive. Quindi: l'avviso "uscita sia in stanza sia in porta" e la sua azione "Correggi" valgono **solo per porte chiuse**; per una porta aperta l'avviso è il contrario ("mancano le uscite nelle stanze"); spuntare/togliere "aperta" nel pannello scrive/rimuove le uscite nelle stanze. Test in Task 7 e Task 9.
2. **La stanza `player`.** È la prima stanza del template (indice 0, preservato da `SortRooms`) e `location = "player"` significa "in mano al giocatore". Non va disegnata sulla mappa, non va disposta, non conta come "non raggiungibile", non si rinomina né si elimina, e le nuove stanze si aggiungono in coda. Test in Task 4, 8, 9.
3. **Testi con accenti e codici PETSCII.** Le descrizioni contengono `à`, `è` e caratteri di controllo come `\u001c`/`\u0090` (colori). Il salvataggio deve conservarli: accenti scritti in chiaro (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`), controlli equivalenti. Test in Task 5 con `esempio.it.json` che li contiene.
4. **Template trovati da qualsiasi cartella.** `Game.LoadMessages` & co. leggono `templates/...` relativo alla cartella *corrente*: l'editor lanciato da un collegamento non li troverebbe. Si risolvono rispetto a `AppContext.BaseDirectory`, con ripiego sulla cartella corrente. Test in Task 1.
5. **Compilare un documento mai salvato o con errori.** F5 su un documento nuovo apre "Salva con nome" (annullare = niente compilazione); con errori di validazione la compilazione non parte e il pannello mostra gli errori. Test in Task 11.

---

## Struttura dei file

```
Common/Json/MapPosition.cs                 NUOVO   coordinate x,y,z
Common/Json/Room.cs                        MOD     proprietà map
Common/Json/Game.Template.cs               MOD     percorso template da AppContext.BaseDirectory
Builder/Builder.csproj                     NUOVO
Builder/GameBuilder.cs                     NUOVO   BuildGame spostato qui
Builder/BuildResult.cs                     NUOVO
Builder/ConfigPrg.cs                       SPOSTATO da IFBuilder64/
Builder/BuildDateAttribute.cs              SPOSTATO da IFBuilder64/
IFBuilder64/Program.cs                     MOD     guscio: opzioni, parsing, stampa Log
Editor/Editor.csproj, Program.cs, App.axaml(.cs)
Editor/Risorse/Testi.resx, Testi.it.resx   stringhe UI (classe generata Testi)
Editor/ViewModels/Direzione.cs             enum + estensioni
Editor/ViewModels/StanzaViewModel.cs, PortaViewModel.cs, OggettoViewModel.cs,
                  VerboViewModel.cs, MessaggioViewModel.cs, IFictionViewModel.cs
Editor/ViewModels/DocumentoViewModel.cs            mappatura Game <-> VM, Modificato
Editor/ViewModels/DocumentoViewModel.Riferimenti.cs rinomini a cascata, eliminazioni
Editor/ViewModels/MainWindowViewModel.cs, MappaViewModel.cs, OggettiViewModel.cs,
                  VerbiViewModel.cs, MessaggiViewModel.cs, CampoNomeViewModel.cs
Editor/Servizi/CatalogoAzioni.cs, ServizioFile.cs, ImpostazioniUtente.cs, IDialoghi.cs,
               Collegamenti.cs, DisposizioneAutomatica.cs, Validatore.cs, Avviso.cs,
               ServizioCompilazione.cs
Editor/Controlli/GeometriaMappa.cs         matematica pura cella<->pixel, hit test
Editor/Controlli/MappaControl.cs           Render + input
Editor/Views/*.axaml                       MainWindow, MappaView, PannelloStanza, PannelloPorta,
                                           OggettiView, VerbiView, MessaggiView, GiocoView,
                                           InformazioniWindow, OpzioniCompilazioneWindow,
                                           DialoghiAvalonia.cs (implementa IDialoghi)
Editor/THIRD-PARTY-NOTICES.txt
Tests/Tests.csproj
Tests/Dati/esempio.it.json
Tests/Riferimenti/esempio*.{prg,seq,xml,bas}
Tests/*Tests.cs
```

Namespace: `Casasoft.IFBuilder64.Builder`, `Casasoft.IFBuilder64.Editor` (+ `.ViewModels`, `.Servizi`, `.Controlli`, `.Views`, `.Risorse`), `Casasoft.IFBuilder64.Tests`.

Comando di test usato in tutto il piano: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~<Classe>"`.

---

## Fase A — Fondamenta

### Task 1: Progetto Tests, `MapPosition`, template indipendenti dalla cartella corrente

**Files:**
- Create: `Common/Json/MapPosition.cs`, `Tests/Tests.csproj`, `Tests/ModelloTests.cs`
- Modify: `Common/Json/Room.cs`, `Common/Json/Game.Template.cs:130-150`, `Common/Common.csproj` (Compile Update per MapPosition come gli altri), `IFBuilder64.slnx`

**Interfaces:**
- Produces: `Casasoft.IFBuilder64.Json.MapPosition { int x; int y; int z; }`, `Room.map : MapPosition?` (testo e attributi esattamente come §3.1 della spec); progetto `Tests` con riferimenti a `Common` (Builder ed Editor si aggiungono nei task che li creano).

- [ ] **Step 1: Crea `Tests/Tests.csproj`** con xUnit (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`), `IsPackable=false`, riferimento a `Common`. Aggiungi a `IFBuilder64.slnx`. `dotnet build` deve passare.

- [ ] **Step 2: Scrivi i test che falliscono** in `Tests/ModelloTests.cs`:

```csharp
[Fact] public void StanzaSenzaMappa_NonScriveIlCampo()
{
    var json = JsonSerializer.Serialize(new Room { name = "a", description = "" });
    Assert.DoesNotContain("\"map\"", json);
}
[Fact] public void StanzaConMappa_AndataERitorno()
{
    var r = new Room { name = "a", description = "", map = new MapPosition { x = 2, y = -1, z = 1 } };
    var r2 = JsonSerializer.Deserialize<Room>(JsonSerializer.Serialize(r))!;
    Assert.Equal((2, -1, 1), (r2.map!.x, r2.map.y, r2.map.z));
}
[Fact] public void Template_SiCaricaDaAltraCartellaCorrente()
{
    var vecchia = Directory.GetCurrentDirectory();
    try {
        Directory.SetCurrentDirectory(Path.GetTempPath());
        var g = new Game(TemplateLanguage.Italiano);
        Assert.Equal(55, g.messages.Count);
        Assert.Equal(6, g.directions.Count);
    } finally { Directory.SetCurrentDirectory(vecchia); }
}
```

- [ ] **Step 3: Verifica che falliscano** — `MapPosition` non esiste (errore di compilazione); dopo averla aggiunta il terzo fallisce con `DirectoryNotFoundException`/`FileNotFoundException`.

- [ ] **Step 4: Implementa** `MapPosition` e `Room.map` come §3.1. In `Game.Template.cs` aggiungi `private static string PercorsoTemplate(string nomeFile)` che restituisce `Path.Combine(AppContext.BaseDirectory, "templates", nomeFile)` se esiste, altrimenti `Path.Combine("templates", nomeFile)`; usalo nei tre `Load*`.

- [ ] **Step 5: Verifica** — `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~ModelloTests"` → 3 PASS. `dotnet run --project IFBuilder64 -- --schema --out %TEMP%/s.json` e controlla che lo schema contenga `"map"` con le descrizioni.

- [ ] **Step 6: Commit** — `git commit -m "Common: posizione mappa nelle stanze, progetto Tests, template da cartella applicazione"`

### Task 2: Estrazione del Builder con test di non regressione

**Files:**
- Create: `Tests/Dati/esempio.it.json`, `Tests/Riferimenti/*`, `Builder/Builder.csproj`, `Builder/GameBuilder.cs`, `Builder/BuildResult.cs`, `Tests/BuilderTests.cs`
- Move: `IFBuilder64/ConfigPrg.cs` → `Builder/ConfigPrg.cs` (namespace `Casasoft.IFBuilder64.Builder`, resta `internal`), `IFBuilder64/BuildDateAttribute.cs` → `Builder/`
- Modify: `IFBuilder64/Program.cs`, `IFBuilder64/IFBuilder64.csproj`, `Tests/Tests.csproj`, `IFBuilder64.slnx`

**Interfaces:**
- Produces:
  ```csharp
  public class GameBuilder { public BuildResult Build(Game game, string outputBase); }
  public class BuildResult {
      public bool Successo { get; }  public IReadOnlyList<string> FileGenerati { get; }
      public IReadOnlyList<string> Log { get; }
  }
  ```

- [ ] **Step 1: Crea `Tests/Dati/esempio.it.json`** partendo dall'esempio "castello" del manuale italiano (righe ~360–410) ed estendendolo perché contenga: stanze su tre livelli (up/down), una porta chiusa con `unlockitem`, una porta **aperta** (`"open": true`) con le uscite dichiarate anche nelle stanze, un contenitore statico (`baule@torre`) con un oggetto dentro (`initem`), un contenitore mobile con un oggetto dentro, un oggetto con `location: "player"`, 3 `usermessages`, `ifiction` con `ifid`, testi con accenti e con `\u001c`/`\u0090`. Nessun campo `map`. Le liste verbs/messages/directions dal template `it`.

- [ ] **Step 2: Registra l'output della CLI attuale** (prima di toccare `Program.cs`):
  Run: `dotnet run --project IFBuilder64 -- -q Tests/Dati/esempio.it.json -o Tests/Riferimenti/esempio`
  Expected: 9 file `Tests/Riferimenti/esempio{in,dt,db,cf}.prg`, `esempioab.seq`, `esempioifid.seq`, `esempioiFiction.xml`, `esempiomem.bas`, `esempiodef.bas`. Marca la cartella come binaria in `.gitattributes` (`Tests/Riferimenti/** binary`) e commit: `git commit -m "Test: esempio e output di riferimento della CLI prima dell'estrazione"`.

- [ ] **Step 3: Scrivi i test che falliscono** in `Tests/BuilderTests.cs` (Tests referenzia ora `Builder`; `Riferimenti` e `Dati` copiati nell'output con `CopyToOutputDirectory=PreserveNewest`):

```csharp
[Fact] public void Build_ProduceGliStessiByteDellaCli()
{
    var game = JsonSerializer.Deserialize<Game>(File.ReadAllText("Dati/esempio.it.json"))!;
    var dir = Directory.CreateTempSubdirectory().FullName;
    var r = new GameBuilder().Build(game, Path.Combine(dir, "esempio"));
    Assert.True(r.Successo);
    Assert.Equal(9, r.FileGenerati.Count);
    foreach (var rif in Directory.GetFiles("Riferimenti"))
        Assert.Equal(Normalizza(rif), Normalizza(Path.Combine(dir, Path.GetFileName(rif))));
}
// Normalizza: byte del file; per *ab.seq azzera la 5ª riga (data, separatore '\r');
// per *iFiction.xml sostituisce il contenuto di <releasedate> e <compilerversion> con "".

[Fact] public void Build_NonModificaIlGameDelChiamante()
{   // confronta JsonSerializer.Serialize(game) prima e dopo Build
}
[Fact] public void Build_NonScriveSuConsole()
{   // Console.SetOut(StringWriter) attorno a Build; Assert.Equal("", writer.ToString()); Log non vuoto
}
[Fact] public void Build_CartellaInesistente_RestituisceErroreSenzaEccezione()
{   // outputBase in una cartella che non esiste → Successo == false, Log contiene un messaggio
}
```

- [ ] **Step 4: Verifica che falliscano** — errore di compilazione: `GameBuilder` non esiste.

- [ ] **Step 5: Implementa `Builder`**: progetto class library (riferimenti `Common`, `Casasoft.Commodore 1.0.2`; sposta l'`AssemblyAttribute BuildDateAttribute` dal csproj della CLI a questo). `GameBuilder.Build` copia il `Game` con `JsonSerializer.Deserialize<Game>(JsonSerializer.Serialize(game))`, poi esegue il corpo di `BuildGame` da `#region intro` a `#region defs include` invariato, sostituendo ogni `Console.WriteLine(x)` con un'aggiunta a `Log` e ogni salvataggio con un'aggiunta a `FileGenerati`. `AllocateDown` diventa `private static`. Tutto il corpo in `try/catch (Exception ex)`: in caso di errore `Successo=false` e `Log` riceve `ex.Message`. `Assembly.GetExecutingAssembly()` ora è l'assembly Builder: va bene (la versione è esclusa dal confronto).

- [ ] **Step 6: Riduci la CLI**: `BuildGame(inputFile, outputFile)` in `Program.cs` mantiene la riga `Building '...' to '...*'...`, il parsing e il messaggio `Error: Unable to parse game file`, poi chiama `new GameBuilder().Build(game, outputFile)` e stampa ogni riga di `Log`. Rimuovi `ConfigPrg.cs`, `BuildDateAttribute.cs` e il riferimento `Casasoft.Commodore` dalla CLI; aggiungi `ProjectReference` a `Builder`. Aggiungi `Builder` alla solution.

- [ ] **Step 7: Verifica** — `dotnet test ... --filter "FullyQualifiedName~BuilderTests"` → 4 PASS. Poi esegui di nuovo il comando dello Step 2 verso una cartella temporanea e controlla che l'output console sia identico a prima (stesse righe `Build ... file:`).

- [ ] **Step 8: Commit** — `git commit -m "Builder: estrazione della compilazione in libreria, CLI ridotta a guscio"`

---

## Fase B — Logica dell'editor (senza interfaccia)

### Task 3: Progetto Editor, localizzazione, catalogo azioni, impostazioni utente

**Files:**
- Create: `Editor/Editor.csproj`, `Editor/Program.cs`, `Editor/App.axaml(.cs)`, `Editor/Views/MainWindow.axaml(.cs)` (vuota per ora), `Editor/Risorse/Testi.resx`, `Editor/Risorse/Testi.it.resx`, `Editor/Servizi/CatalogoAzioni.cs`, `Editor/Servizi/ImpostazioniUtente.cs`, `Tests/CatalogoAzioniTests.cs`, `Tests/ImpostazioniTests.cs`
- Modify: `IFBuilder64.slnx`, `Tests/Tests.csproj`

**Interfaces:**
- Produces:
  ```csharp
  public record AzioneMotore(byte Codice, string Nome) { public override string ToString() => $"{Nome} ({Codice})"; }
  public static class CatalogoAzioni { IReadOnlyList<AzioneMotore> Tutte; bool Esiste(byte codice); AzioneMotore? Trova(byte codice); }
  public class ImpostazioniUtente {
      string? Lingua; List<string> FileRecenti; string? UltimaCartellaOutput;
      static ImpostazioniUtente Carica(string? percorso = null); void Salva(string? percorso = null);
      void AggiungiRecente(string percorso);   // in testa, senza duplicati, max 10
      static string PercorsoPredefinito { get; } // ApplicationData/IFBuilder64/editor.json
  }
  ```
  Classe risorse generata `Casasoft.IFBuilder64.Editor.Risorse.Testi` (pubblica, proprietà statiche) — tutti i task successivi aggiungono le proprie chiavi a entrambi i `.resx`.

- [ ] **Step 1: Crea il progetto** `Editor` (`OutputType=WinExe`, pacchetti `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent` 12.1.1, `CommunityToolkit.Mvvm` ultima 8.x, riferimenti `Builder` e `Common`, `AvaloniaUseCompiledBindingsByDefault=true`). Verifica che `Avalonia.Controls.DataGrid` 12.1.1 esista su nuget.org con licenza MIT (`dotnet add package Avalonia.Controls.DataGrid --version 12.1.1`); se non esiste o non è MIT, **fermati e chiedi** all'utente prima di procedere (impatta i Task 14–15). Il `.resx` usa la generazione MSBuild, che funziona anche fuori da Visual Studio:
  ```xml
  <EmbeddedResource Update="Risorse\Testi.resx">
    <Generator>MSBuild:Compile</Generator>
    <StronglyTypedFileName>$(IntermediateOutputPath)Testi.Designer.cs</StronglyTypedFileName>
    <StronglyTypedLanguage>CSharp</StronglyTypedLanguage>
    <StronglyTypedNamespace>Casasoft.IFBuilder64.Editor.Risorse</StronglyTypedNamespace>
    <StronglyTypedClassName>Testi</StronglyTypedClassName>
    <PublicClass>true</PublicClass>
  </EmbeddedResource>
  ```
  `Program.Main` legge `ImpostazioniUtente.Carica()` e, se `Lingua` non è null, imposta `CultureInfo.DefaultThreadCurrentUICulture` prima di `BuildAvaloniaApp()`. Aggiungi `Editor` alla solution e il riferimento in `Tests`.

- [ ] **Step 2: Scrivi i test che falliscono**:

```csharp
[Fact] public void Catalogo_Ha26AzioniDa0a25() =>
    Assert.Equal(Enumerable.Range(0, 26).Select(i => (byte)i), CatalogoAzioni.Tutte.Select(a => a.Codice));
[Fact] public void Catalogo_26NonEsiste() => Assert.False(CatalogoAzioni.Esiste(26));
[Fact] public void Catalogo_NomiLocalizzati()
{   // con Testi.Culture = it → Trova(8)!.Nome == "prendi"; con en → "take"; Trova(7) it == "guarda/esamina"
}
[Fact] public void Impostazioni_AndataERitorno() { /* Salva su file temporaneo, Carica, stessi valori */ }
[Fact] public void Impostazioni_FileMancanteOCorrotto_RestituisceDefault() { /* Lingua null, FileRecenti vuoto */ }
[Fact] public void Impostazioni_RecentiSenzaDuplicatiMax10() { /* 12 aggiunte con un duplicato → 10, il più recente primo */ }
```

- [ ] **Step 3: Verifica che falliscano** (tipi mancanti).

- [ ] **Step 4: Implementa.** Nomi delle azioni nelle chiavi `Azione_0`…`Azione_25` dei due `.resx`, testi dalla lista di §5.4 (en: north, south, east, west, up, down, go, look/examine, take, drop, open, close, switch on, switch off, insert, inventory, magic word, help, command list, save, load, undo, quit, about, record commands, list exits). `ImpostazioniUtente` usa `System.Text.Json`; ogni errore di lettura → istanza vuota.

- [ ] **Step 5: Verifica** — `--filter "FullyQualifiedName~CatalogoAzioniTests|FullyQualifiedName~ImpostazioniTests"` → PASS; `dotnet run --project Editor` apre una finestra vuota.

- [ ] **Step 6: Commit** — `git commit -m "Editor: progetto Avalonia, risorse localizzate, catalogo azioni, impostazioni utente"`

### Task 4: ViewModel del documento e mappatura `Game` ↔ ViewModel

**Files:**
- Create: `Editor/ViewModels/Direzione.cs`, `StanzaViewModel.cs`, `PortaViewModel.cs`, `OggettoViewModel.cs`, `VerboViewModel.cs`, `MessaggioViewModel.cs`, `IFictionViewModel.cs`, `DocumentoViewModel.cs`, `Tests/DocumentoTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public enum Direzione { Nord, Sud, Est, Ovest, Su, Giu }   // stesso ordine di RoomExits
  public static class DirezioneEstensioni { Direzione Opposta(this Direzione d); (int dx, int dy, int dz) Spostamento(this Direzione d); }
  public interface IConUscite { string? Uscita(Direzione d); void ImpostaUscita(Direzione d, string? stanza); }
  public partial class StanzaViewModel : ObservableObject, IConUscite {
      string Nome; string Descrizione; string? Nord, Sud, Est, Ovest, Su, Giu;
      bool HaPosizione; int X, Y, Z; bool EGiocatore => Nome == "player";
      static StanzaViewModel Da(Room r); Room ToRoom();      // map scritto solo se HaPosizione
  }
  public partial class PortaViewModel : ObservableObject, IConUscite {
      string Nome; string Descrizione; string? MessaggioNegato /*denyopen*/; string? OggettoSblocco; bool? Aperta;
      string? Nord…Giu; static PortaViewModel Da(Door d); Door ToDoor();
  }
  public partial class OggettoViewModel : ObservableObject {
      string Nome; string Descrizione; string? DescrizioneAperto; string? Posizione /*location*/;
      string? DentroA /*initem*/; string? OggettoSblocco; bool? Statico /*notportable*/;
      bool? Apribile, Chiudibile, Aperto, Accendibile, Acceso, EContenitore;
      string Id { get; }   // stessa regola di Item.id
      static OggettoViewModel Da(Item i); Item ToItem();
  }
  public partial class VerboViewModel : ObservableObject { string Comando; byte Azione; }
  public partial class MessaggioViewModel : ObservableObject { int Indice; string Testo; string? TestoTemplate; }
  public partial class IFictionViewModel : ObservableObject { /* tutti i campi di IFiction, stessi nomi in italiano */ }
  public partial class DocumentoViewModel : ObservableObject {
      string? Percorso; bool Modificato;
      ObservableCollection<StanzaViewModel> Stanze; …<OggettoViewModel> Oggetti; …<PortaViewModel> Porte;
      …<VerboViewModel> Verbi; …<MessaggioViewModel> MessaggiStandard, MessaggiUtente, Direzioni;
      string Intro; string StanzaIniziale; byte CaricoMassimo; IFictionViewModel? IFiction;
      static DocumentoViewModel DaGame(Game g); Game ToGame();
      StanzaViewModel? TrovaStanza(string? nome); OggettoViewModel? TrovaOggettoPerId(string? id);
      string NomeUnivoco(string prefisso, IEnumerable<string> esistenti); // prefisso + N, N minimo ≥ 1 libero
      event EventHandler? ContenutoCambiato;  // ogni modifica a proprietà o collezioni annidate
  }
  ```

- [ ] **Step 1: Scrivi i test che falliscono** (helper `JsonUguale(Game a, Game b)` = `JsonNode.DeepEquals` dei due `JsonSerializer.SerializeToNode`):

```csharp
[Theory] [InlineData("it")] [InlineData("en")] [InlineData("esempio")]
public void AndataERitorno_JsonEquivalente(string sorgente) { /* template o Dati/esempio.it.json → DaGame → ToGame → JsonUguale */ }
[Fact] public void FlagNullRestanoNull() { /* Item senza openable → ToItem().openable == null; idem Door.open */ }
[Fact] public void OrdinePreservato() { /* nomi stanze/oggetti/verbi nello stesso ordine */ }
[Fact] public void StanzaSenzaMappa_DopoPosizionamento_SalvaLeCoordinate()
{ /* HaPosizione=true, X=3 → ToGame().rooms[i].map!.x == 3 */ }
[Fact] public void ModificaAnnidata_ImpostaModificato()
{ /* doc.Oggetti[0].Descrizione = "x" → doc.Modificato; doc.Stanze.Add(...) → doc.Modificato; DaGame parte con Modificato == false */ }
[Fact] public void NomeUnivoco_PrimoLibero() => Assert.Equal("stanza2", doc.NomeUnivoco("stanza", ["stanza1", "stanza3"]));
```

- [ ] **Step 2: Verifica che falliscano** (tipi mancanti).

- [ ] **Step 3: Implementa** con `[ObservableProperty]`. `DocumentoViewModel` si iscrive a `PropertyChanged` di ogni elemento e a `CollectionChanged` di ogni collezione (anche per gli elementi aggiunti dopo) e imposta `Modificato = true` + solleva `ContenutoCambiato`. `IFiction` null nel `Game` → `IFiction` null nel documento.

- [ ] **Step 4: Verifica** — `--filter "FullyQualifiedName~DocumentoTests"` → PASS.

- [ ] **Step 5: Commit** — `git commit -m "Editor: ViewModel del documento con mappatura Game nei due sensi"`

### Task 5: `ServizioFile` — nuovo, apri, salva, lingua del template

**Files:**
- Create: `Editor/Servizi/ServizioFile.cs`, `Tests/ServizioFileTests.cs`

**Interfaces:**
- Consumes: `DocumentoViewModel.DaGame/ToGame` (Task 4).
- Produces:
  ```csharp
  public class ErroreApertura(string messaggio) : Exception(messaggio);
  public static class ServizioFile {
      static JsonSerializerOptions Opzioni { get; } // WriteIndented, WhenWritingNull, Encoder = UnsafeRelaxedJsonEscaping
      static DocumentoViewModel Nuovo(TemplateLanguage lingua);
      static DocumentoViewModel Apri(string percorso);       // ErroreApertura con riga/posizione di JsonException
      static void Salva(DocumentoViewModel doc, string percorso); // aggiorna Percorso, Modificato=false
      static TemplateLanguage? RilevaLingua(Game g);          // ifiction.language "it*"/"en*", altrimenti confronto directions con i template, altrimenti null
      static IReadOnlyList<string> MessaggiTemplate(TemplateLanguage lingua);
  }
  ```

- [ ] **Step 1: Scrivi i test che falliscono**:

```csharp
[Fact] public void Apri_Salva_ConservaAccentiEControlli()
{ /* Apri esempio, Salva in temp: il testo contiene "è" in chiaro; JsonNode.DeepEquals con l'originale a parte "map" e "id" */ }
[Fact] public void Apri_JsonNonValido_ErroreConPosizione()
{ /* file "{ \"rooms\": [ }" → Assert.Throws<ErroreApertura>, Message contiene "line" o la riga */ }
[Fact] public void Apri_RiempieTestoTemplate() { /* MessaggiStandard[0].TestoTemplate == MessaggiTemplate(Italiano)[0] */ }
[Fact] public void Apri_FileSenzaMappa_DisponeLeStanze() { /* dopo Apri, tutte le stanze non-player hanno HaPosizione */ }
[Fact] public void Nuovo_Italiano_HaStanzaPlayerEModificatoFalse() { }
[Fact] public void Salva_AzzeraModificatoEImpostaPercorso() { }
```

- [ ] **Step 2: Verifica che falliscano.** Il quarto resta rosso fino al Task 8: marcalo `[Fact(Skip = "Task 8")]` dopo averlo visto fallire.

- [ ] **Step 3: Implementa.** `Apri` legge, deserializza con `Opzioni`, mappa, imposta `TestoTemplate` se `RilevaLingua` non è null, poi (dal Task 8) chiama `DisposizioneAutomatica.Disponi(doc, tutte: false)` e azzera `Modificato`.

- [ ] **Step 4: Verifica** — `--filter "FullyQualifiedName~ServizioFileTests"` → PASS (uno saltato).

- [ ] **Step 5: Commit** — `git commit -m "Editor: apertura e salvataggio dei file di gioco"`

### Task 6: Rinomini a cascata ed eliminazioni con riferimenti

**Files:**
- Create: `Editor/ViewModels/DocumentoViewModel.Riferimenti.cs`, `Tests/RiferimentiTests.cs`

**Interfaces:**
- Produces (su `DocumentoViewModel`; i metodi `Rinomina*` restituiscono `null` se riusciti, altrimenti il messaggio d'errore localizzato e non cambiano nulla):
  ```csharp
  string? RinominaStanza(StanzaViewModel s, string nuovo);
  string? RinominaOggetto(OggettoViewModel o, string nuovo);
  string? RinominaPorta(PortaViewModel p, string nuovo);
  void SpostaOggetto(OggettoViewModel o, string? posizione, string? dentroA); // aggiorna gli initem che puntano al vecchio Id
  void ImpostaStatico(OggettoViewModel o, bool statico);                    // idem (cambia l'Id)
  public record Riferimento(object Elemento, string Campo);
  IReadOnlyList<Riferimento> TrovaRiferimenti(object elemento);
  void Elimina(object elemento);  // svuota i riferimenti e rimuove l'elemento
  ```

Regole (§6.1): rinomina stanza → uscite di stanze e porte, `Posizione` degli oggetti, `StanzaIniziale`, e gli `DentroA` che puntano a `x@vecchia`. Rinomina oggetto → `OggettoSblocco` di oggetti e porte, `DentroA` (con il nuovo Id). Duplicati: nome stanza/porta già usato; Id oggetto già usato. Nome vuoto → errore. Stanza `player` → errore, non rinominabile né eliminabile (Review Focus 2).

- [ ] **Step 1: Scrivi i test che falliscono**:

```csharp
[Fact] public void RinominaStanza_AggiornaUsciteDoorLocationInizialeEInitem() { /* su esempio: "torre"→"mastio": porte, baule.Posizione, pergamena.DentroA=="baule@mastio" */ }
[Fact] public void RinominaContenitoreStatico_AggiornaInitem() { /* baule→cassa: pergamena.DentroA == "cassa@torre" */ }
[Fact] public void SpostaContenitoreStatico_AggiornaInitem() { /* SpostaOggetto(baule, "cortile", null) → "baule@cortile" */ }
[Fact] public void RinominaOggetto_AggiornaUnlockitem() { /* chiave→chiavone: porta.OggettoSblocco == "chiavone" */ }
[Fact] public void RinominaDuplicato_Rifiutato() { /* errore != null, nome invariato */ }
[Fact] public void StessoNomeStaticoInStanzeDiverse_Ammesso() { /* "tavolo"@a e "tavolo"@b: Id diversi → ok */ }
[Fact] public void RinominaOEliminaPlayer_Rifiutato() { }
[Fact] public void Elimina_SvuotaIRiferimenti() { /* Elimina(chiave): TrovaRiferimenti prima ha 1 voce; dopo porta.OggettoSblocco == null */ }
```

- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~RiferimentiTests"` → PASS.

- [ ] **Step 5: Commit** — `git commit -m "Editor: rinomini a cascata ed eliminazioni con riferimenti"`

### Task 7: Collegamenti, porte e passaggi

**Files:**
- Create: `Editor/Servizi/Collegamenti.cs`, `Tests/CollegamentiTests.cs`

Semantica delle porte (manuale, sezione Porte): `door.<d> = R` significa "dalla stanza R, andando in direzione d, si attraversa la porta". Una porta con esattamente due lati (d1→R1, d2→R2) collega R1 —d1→ R2 e R2 —d2→ R1. Porte con un numero di lati diverso da 2 non producono collegamenti.

**Interfaces:**
- Produces:
  ```csharp
  public record Collegamento(StanzaViewModel Da, Direzione Direzione, StanzaViewModel A, PortaViewModel? Porta);
  public static class Collegamenti {
      IEnumerable<Collegamento> Tutti(DocumentoViewModel doc);                 // uscite + porte, ignora destinazioni inesistenti
      void Collega(DocumentoViewModel doc, StanzaViewModel da, Direzione dir, StanzaViewModel a, bool senzaRitorno);
      void Elimina(DocumentoViewModel doc, StanzaViewModel da, Direzione dir); // rimuove anche il ritorno se reciproco
      PortaViewModel TrasformaInPorta(DocumentoViewModel doc, StanzaViewModel da, Direzione dir); // nome NomeUnivoco(Testi.PrefissoPorta), Aperta=false
      void TrasformaInPassaggio(DocumentoViewModel doc, PortaViewModel porta);
      void ImpostaAperta(DocumentoViewModel doc, PortaViewModel porta, bool aperta); // aperta → scrive le uscite nelle due stanze; chiusa → le rimuove
      void CorreggiUscitaDoppia(DocumentoViewModel doc, StanzaViewModel stanza, Direzione dir); // solo porte chiuse
  }
  ```

- [ ] **Step 1: Scrivi i test che falliscono**:

```csharp
[Fact] public void Collega_ScriveUscitaEOpposta() { /* a.Est == "b", b.Ovest == "a" */ }
[Fact] public void Collega_SenzaRitorno() { /* b.Ovest == null */ }
[Fact] public void TrasformaInPorta_RimuoveUsciteECreaPortaChiusa()
{ /* a.Nord="b", b.Sud="a" → porta.Nord=="a", porta.Sud=="b", Aperta==false, a.Nord==null, b.Sud==null */ }
[Fact] public void TrasformaInPassaggio_RicreaUsciteERimuovePorta() { }
[Fact] public void Tutti_IncludeLatiDellePorte() { /* porta {north:cortile, south:torre} → (cortile,Nord,torre) e (torre,Sud,cortile) */ }
[Fact] public void ImpostaAperta_ScriveERimuoveUsciteNelleStanze() { }
[Fact] public void CorreggiUscitaDoppia_PortaApertaNonTocca() { /* uscita conservata */ }
```

- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~CollegamentiTests"` → PASS.

- [ ] **Step 5: Commit** — `git commit -m "Editor: collegamenti tra stanze e conversione porta/passaggio"`

### Task 8: Disposizione automatica

**Files:**
- Create: `Editor/Servizi/DisposizioneAutomatica.cs`, `Tests/DisposizioneTests.cs`
- Modify: `Editor/Servizi/ServizioFile.cs` (chiamata in `Apri`), `Tests/ServizioFileTests.cs` (togli lo `Skip`)

**Interfaces:**
- Consumes: `Collegamenti.Tutti` (Task 7), `Direzione.Spostamento` (Task 4).
- Produces: `public static class DisposizioneAutomatica { static void Disponi(DocumentoViewModel doc, bool tutte); }`

Algoritmo (§5.2): con `tutte=true` azzera `HaPosizione` di tutte le stanze. La stanza `player` non viene mai posizionata. Partenza: `StanzaIniziale` (se senza posizione → (0,0,0)); BFS su `Collegamenti.Tutti`; ogni stanza non posizionata va in `posizione(da) + Spostamento(dir)`; se occupata, la cella libera più vicina sullo stesso livello cercando ad anelli concentrici (distanza di Chebyshev 1, 2, …; dentro l'anello in ordine di riga e poi colonna, così il risultato è deterministico). Le stanze rimaste vanno in fila al livello 0, `y = maxY(livello 0) + 2`, `x` da `minX(livello 0)` crescente, saltando le celle occupate.

- [ ] **Step 1: Scrivi i test che falliscono**:

```csharp
[Fact] public void Bfs_RispettaLeDirezioni() { /* iniziale (0,0,0); nord → (0,-1,0); su → (0,0,1) */ }
[Fact] public void NessunaSovrapposizione() { /* esempio: (X,Y,Z) distinti per tutte le stanze posizionate */ }
[Fact] public void CellaOccupata_UsaLaPiuVicina() { /* due stanze a est della stessa: la seconda a distanza 1 dalla cella prevista */ }
[Fact] public void Isolate_InFilaSottoAlLivello0() { /* Y > max Y delle raggiungibili a z=0 */ }
[Fact] public void PosizioniEsistentiConservateSeNonTutte() { }
[Fact] public void PlayerMaiPosizionato() { }
```

- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~DisposizioneTests|FullyQualifiedName~ServizioFileTests"` → tutti PASS.

- [ ] **Step 5: Commit** — `git commit -m "Editor: disposizione automatica delle stanze sulla griglia"`

### Task 9: Validatore

**Files:**
- Create: `Editor/Servizi/Avviso.cs`, `Editor/Servizi/Validatore.cs`, `Tests/ValidatoreTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public enum Gravita { Errore, Avviso }
  public record Avviso(Gravita Gravita, string Messaggio, object? Elemento, Action? Correzione = null);
  public static class Validatore {
      static IReadOnlyList<Avviso> Valida(DocumentoViewModel doc, int numeroMessaggiTemplate);
      static bool HaErrori(IReadOnlyList<Avviso> avvisi);
  }
  ```

Regole: esattamente l'elenco di §6.3 della spec, con due precisazioni (Review Focus 1–2): "uscita sia nella stanza sia in una porta" solo per porte chiuse (con `Correzione = () => Collegamenti.CorreggiUscitaDoppia(...)`), e un avviso in più "porta aperta senza le uscite nelle stanze"; la stanza `player` è esclusa dalla raggiungibilità. Il limite è "più di 255" → errore quando `Count > 255`. `numeroMessaggiTemplate` = `ServizioFile.MessaggiTemplate(lingua).Count` (55 per entrambi i template).

- [ ] **Step 1: Scrivi i test che falliscono** — un `[Fact]` per regola, ciascuno su un documento minimo valido (da `Nuovo(Italiano)` + una stanza iniziale) con un solo difetto, che verifica gravità ed `Elemento`:
  `NessunaStanza_Errore`, `StanzaInizialeVuotaOInesistente_Errore`, `NomiStanzaDuplicati_Errore`, `NomiPortaDuplicati_Errore`, `IdOggettoDuplicati_Errore`, `Oltre255Stanze_Errore` (256 → errore, 255 → nessun errore), `Oltre255Oggetti_Errore`, `Oltre255Porte_Errore`, `StaticoSenzaStanza_Errore`, `UscitaVersoStanzaInesistente_Avviso`, `LocationInitemUnlockitemOrfani_Avviso`, `StanzaNonRaggiungibile_Avviso`, `PlayerNonSegnalatoNonRaggiungibile`, `UscitaDoppiaPortaChiusa_AvvisoConCorrezione` (eseguire `Correzione` rimuove l'uscita), `PortaApertaConUscite_NessunAvviso`, `PortaApertaSenzaUscite_Avviso`, `VerboFuoriCatalogo_Avviso`, `NumeroMessaggiStandardDiverso_Avviso`, `DocumentoEsempio_NessunErrore`.

- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~ValidatoreTests"` → PASS.

- [ ] **Step 5: Commit** — `git commit -m "Editor: validatore del documento"`

### Task 10: Servizio di compilazione

**Files:**
- Create: `Editor/Servizi/ServizioCompilazione.cs`, `Tests/CompilazioneTests.cs`

**Interfaces:**
- Consumes: `GameBuilder` (Task 2).
- Produces: `public class ServizioCompilazione { Task<BuildResult> CompilaAsync(DocumentoViewModel doc, string outputBase); static string OutputPredefinito(string percorsoJson) => Path.ChangeExtension(percorsoJson, ""); }` — stessa regola della CLI, quindi stessi nomi di file. Esegue `GameBuilder.Build(doc.ToGame(), outputBase)` con `Task.Run`.

- [ ] **Step 1: Scrivi i test che falliscono**:

```csharp
[Fact] public async Task Compila_StessiByteDellaCli() { /* Apri esempio → CompilaAsync in temp → stesso confronto normalizzato di BuilderTests (estrai Normalizza in una classe Tests/Confronto.cs) */ }
[Fact] public async Task Compila_NonModificaIlDocumento() { /* ordine delle stanze nel documento invariato, Modificato invariato */ }
```

- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~CompilazioneTests|FullyQualifiedName~BuilderTests"` → PASS. Il primo test dimostra il criterio "stessi byte" della spec anche passando per i ViewModel e la disposizione (che aggiunge `map`, ignorato dal Builder).

- [ ] **Step 5: Commit** — `git commit -m "Editor: compilazione in background tramite Builder"`

---

## Fase C — Interfaccia

Ogni task di questa fase: le stringhe visibili vanno in entrambi i `.resx` (`{x:Static r:Testi.Chiave}` nello XAML); i dialoghi passano da `IDialoghi`, così i ViewModel restano testabili senza finestre.

### Task 11: Finestra principale, menu, avvisi, compilazione

**Files:**
- Create: `Editor/Servizi/IDialoghi.cs`, `Editor/Views/DialoghiAvalonia.cs`, `Editor/ViewModels/MainWindowViewModel.cs`, `Editor/Views/OpzioniCompilazioneWindow.axaml(.cs)`, `Tests/MainWindowViewModelTests.cs`, `Tests/InterfacciaTests.cs`, `Tests/AppTest.cs`
- Modify: `Editor/Views/MainWindow.axaml(.cs)`, `Tests/Tests.csproj` (`Avalonia.Headless.XUnit` 12.1.1; se richiede xUnit v3, passa tutto il progetto a `xunit.v3` — i test esistenti non cambiano)

**Interfaces:**
- Produces:
  ```csharp
  public interface IDialoghi {
      Task<bool> ConfermaAsync(string titolo, string messaggio);
      Task<bool?> ConfermaSalvataggioAsync();            // true salva, false non salvare, null annulla
      Task<string?> ApriFileAsync(); Task<string?> SalvaFileAsync(string? suggerito);
      Task<TemplateLanguage?> ScegliTemplateAsync(); Task MostraErroreAsync(string messaggio);
      Task<string?> OpzioniCompilazioneAsync(string corrente);
  }
  public partial class MainWindowViewModel : ObservableObject {
      MainWindowViewModel(IDialoghi dialoghi, ImpostazioniUtente impostazioni);
      DocumentoViewModel Documento; string Titolo /* "IFBuilder64 Editor - nome.json*" */;
      int SchedaAttiva; ObservableCollection<Avviso> Avvisi; ObservableCollection<string> LogCompilazione;
      bool CompilazioneInCorso; string? OutputBase; object? ElementoSelezionato;
      IAsyncRelayCommand NuovoCommand, ApriCommand, SalvaCommand, SalvaConNomeCommand, CompilaCommand, OpzioniCompilazioneCommand;
      IAsyncRelayCommand<string> ApriRecenteCommand; IRelayCommand<Avviso> VaiAdAvvisoCommand;
      Task<bool> PuoChiudereAsync();     // chiede conferma se Modificato
      void ValidaOra();                   // usato dai test; la modifica avvia un timer di 500 ms che chiama ValidaOra
  }
  ```

Comportamento: dopo ogni Apri/Nuovo si esegue subito `ValidaOra` (così l'avviso "uscita doppia" con "Correggi" compare all'apertura, §6.2). "Apri" con JSON non valido → `MostraErroreAsync` con il messaggio di `ErroreApertura` e documento invariato (§6.5). "Compila" (F5, `KeyBinding`): se `Percorso` null → `SalvaConNome` (annullato = stop); salva; `ValidaOra`; con errori → stop e avvisi visibili; altrimenti `CompilaAsync` con `CompilazioneInCorso=true` (comando disabilitato), al termine `LogCompilazione` = `Log` + `FileGenerati`. `VaiAdAvvisoCommand` imposta `ElementoSelezionato` e la scheda giusta. Recenti e ultima cartella di output in `ImpostazioniUtente`. `MainWindow.Closing` usa `PuoChiudereAsync`.

- [ ] **Step 1: Scrivi i test che falliscono** (`Tests/MainWindowViewModelTests.cs`, con un `DialoghiFinti` che registra le chiamate e restituisce risposte preimpostate):
  `Titolo_MostraAsteriscoSeModificato`, `Apri_JsonNonValido_MostraErroreEMantieneDocumento`, `Apri_ConModifiche_ChiedeConferma_AnnullaNonApre`, `Compila_DocumentoNuovo_ChiedeSalvaConNome_AnnullaNonCompila`, `Compila_ConErrori_NonCompilaEMostraAvvisi`, `Compila_Ok_LogContieneIFileGenerati`, `VaiAdAvviso_SelezionaElemento`, `Apri_AggiungeAiRecenti`.

- [ ] **Step 2: Verifica che falliscano.**

- [ ] **Step 3: Implementa** ViewModel e finestra (layout di §5.1: menu, `TabControl` con le cinque schede vuote, pannello proprietà a destra, pannello avvisi/log comprimibile in basso con doppio clic → `VaiAdAvvisoCommand`).

- [ ] **Step 4: Test di interfaccia headless** (`Tests/InterfacciaTests.cs`, `[assembly: AvaloniaTestApplication(typeof(AppTest))]`, `[AvaloniaFact]`): `Finestra_SiApre`, `CaricaDocumento_TitoloAggiornato`, `F5_CompilaInCartellaTemporanea_LogContieneFileGenerati` (documento esempio salvato in temp, `KeyPress` F5 sulla finestra, attesa del termine, `LogCompilazione` contiene `esempiodt.prg`). Nessun confronto di immagini.

- [ ] **Step 5: Verifica** — `--filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~InterfacciaTests"` → PASS; poi `dotnet test Tests/Tests.csproj` → tutto PASS.

- [ ] **Step 6: Commit** — `git commit -m "Editor: finestra principale, menu File/Compila, pannello avvisi"`

### Task 12: Mappa — ViewModel, geometria e controllo

**Files:**
- Create: `Editor/ViewModels/MappaViewModel.cs`, `Editor/Controlli/GeometriaMappa.cs`, `Editor/Controlli/MappaControl.cs`, `Editor/Views/MappaView.axaml(.cs)`, `Tests/MappaViewModelTests.cs`, `Tests/GeometriaMappaTests.cs`

**Interfaces:**
- Consumes: `Collegamenti` (Task 7), `DisposizioneAutomatica` (Task 8), `Validatore` per le stanze non raggiungibili.
- Produces:
  ```csharp
  public partial class MappaViewModel : ObservableObject {
      MappaViewModel(DocumentoViewModel doc, IDialoghi dialoghi);
      int LivelloCorrente; double Zoom; IReadOnlyList<int> Livelli; object? Selezione;
      IEnumerable<StanzaViewModel> StanzeDelLivello { get; }      // esclusa player
      StanzaViewModel? StanzaIn(int x, int y);                   // sul livello corrente
      StanzaViewModel CreaStanza(int x, int y);                  // NomeUnivoco(Testi.PrefissoStanza), in coda a Stanze
      bool SpostaStanza(StanzaViewModel s, int x, int y);        // false se occupata
      void AggiungiLivello(bool sopra);
      void CollegaVerticale(StanzaViewModel da, Direzione suOGiu, StanzaViewModel a);
      IAsyncRelayCommand DisponiTuttoCommand;                    // chiede conferma
      void ImpostaIniziale(StanzaViewModel s);
  }
  public readonly record struct Cella(int X, int Y);
  public enum Bordo { Nessuno, Nord, Sud, Est, Ovest }
  public class GeometriaMappa {
      GeometriaMappa(double latoCella, double zoom, Point origine);
      Rect RettangoloCella(Cella c); Cella CellaDa(Point p);
      Bordo BordoDa(Point p, Cella c);           // fascia del 20% del lato vicino a ciascun bordo
      IReadOnlyList<Point> Tracciato(Cella da, Direzione dir, Cella a); // retta se adiacenti, altrimenti spezzata
  }
  ```

`MappaControl : Control` con `StyledProperty<MappaViewModel?> Mappa`; `Render` disegna griglia, stanze (nome, ★ iniziale, ▲/▼ se ha `Su`/`Giu`, bordo d'avviso se non raggiungibile), collegamenti da `Collegamenti.Tutti` filtrati sul livello, porte (rettangolino pieno = chiusa, vuoto = aperta). Input: clic su cella vuota → `CreaStanza`; trascinamento dal centro → `SpostaStanza`; trascinamento da un `Bordo` → `Collegamenti.Collega` (Ctrl = senza ritorno); rotella + Ctrl = zoom, trascinamento con tasto centrale = pan; `ContextMenu` su stanza ("Collega in alto a…", "Collega in basso a…", "Imposta come iniziale", "Elimina" — con conferma che elenca `TrovaRiferimenti` se non vuoto) e su collegamento ("Trasforma in porta"/"Trasforma in passaggio", "Elimina"). L'hit test del collegamento usa la distanza punto-segmento dai punti di `Tracciato` (soglia 6 px).

- [ ] **Step 1: Scrivi i test che falliscono**:
  `GeometriaMappaTests`: `CellaDa_InversoDiRettangoloCella` (con zoom 1.5 e origine spostata), `BordoDa_RiconosceIQuattroBordiEIlCentro`, `Tracciato_AdiacentiDuePunti_NonAdiacentiSpezzata`.
  `MappaViewModelTests`: `CreaStanza_NomeProvvisorioUnivocoEPosizione`, `CreaStanza_InCodaNonAIndice0`, `SpostaStanza_SuCellaOccupata_Rifiutato`, `StanzeDelLivello_EsclusoPlayer`, `AggiungiLivello_SopraESotto`, `CollegaVerticale_ScriveSuEGiu`, `DisponiTutto_AnnullatoNonCambiaNulla`.

- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~MappaViewModelTests|FullyQualifiedName~GeometriaMappaTests"` → PASS. Avvia l'editor, apri `Tests/Dati/esempio.it.json`, verifica a vista: livelli, porte, ▲/▼, ★.

- [ ] **Step 5: Commit** — `git commit -m "Editor: mappa delle stanze su griglia con livelli e collegamenti"`

### Task 13: Pannelli proprietà di stanza e porta

**Files:**
- Create: `Editor/ViewModels/CampoNomeViewModel.cs`, `Editor/Views/PannelloStanza.axaml(.cs)`, `Editor/Views/PannelloPorta.axaml(.cs)`, `Tests/CampoNomeTests.cs`

**Interfaces:**
- Consumes: `DocumentoViewModel.Rinomina*` (Task 6), `Collegamenti.ImpostaAperta` (Task 7).
- Produces: `public partial class CampoNomeViewModel : ObservableObject { CampoNomeViewModel(Func<string> leggi, Func<string, string?> rinomina); string Testo; string? Errore; void Conferma(); void Annulla(); }` — riusato dal pannello oggetti (Task 14). Il `TextBox` del nome conferma su Invio/perdita del focus; con `Errore` non null il bordo è rosso e il messaggio sotto il campo.

Pannello stanza (§5.2): nome (`CampoNomeViewModel`), descrizione multilinea, "Imposta come stanza iniziale", sei uscite in sola lettura cliccabili (selezionano la stanza di arrivo), due liste **Statici**/**Mobili** degli oggetti con `Posizione == Nome`, "Nuovo oggetto qui" (mobile, nome `NomeUnivoco(Testi.PrefissoOggetto)`) e "Sposta qui…" (scelta tra gli oggetti → `SpostaOggetto`). Pannello porta: nome, descrizione, `MessaggioNegato`, `OggettoSblocco` (ComboBox con "— nessuno —" + nomi oggetti), checkbox Aperta → `Collegamenti.ImpostaAperta`.

- [ ] **Step 1: Scrivi i test che falliscono**: `Conferma_NomeValido_Rinomina`, `Conferma_Duplicato_ImpostaErroreENonRinomina`, `Annulla_RipristinaTesto`.
- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~CampoNomeTests"` → PASS; verifica a vista con l'esempio (rinomina una stanza: le uscite in mappa seguono).
- [ ] **Step 5: Commit** — `git commit -m "Editor: pannelli proprietà di stanze e porte"`

### Task 14: Scheda Oggetti

**Files:**
- Create: `Editor/ViewModels/OggettiViewModel.cs`, `Editor/Views/OggettiView.axaml(.cs)`, `Tests/OggettiViewModelTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public record VocePosizione(string Etichetta, string? Posizione, string? DentroA); // "— nessuna —", "Giocatore" (player), stanze, "Dentro: <id>"
  public partial class OggettiViewModel : ObservableObject {
      OggettiViewModel(DocumentoViewModel doc, IDialoghi dialoghi);
      IEnumerable<OggettoViewModel> OggettiFiltrati; string? FiltroStanza; bool? FiltroStatico; OggettoViewModel? Selezionato;
      IReadOnlyList<VocePosizione> PosizioniPer(OggettoViewModel o);  // statici: solo stanze (no player, no contenitori)
      void ImpostaPosizione(OggettoViewModel o, VocePosizione v);     // via SpostaOggetto
      IRelayCommand NuovoCommand, DuplicaCommand; IAsyncRelayCommand EliminaCommand; // Elimina: conferma con TrovaRiferimenti
  }
  ```
  DataGrid con le colonne di §5.3; pannello con descrizione, `DescrizioneAperto`, `OggettoSblocco` (ComboBox), nome con `CampoNomeViewModel`.

- [ ] **Step 1: Scrivi i test che falliscono**: `PosizioniPerStatico_SoloStanze`, `PosizioniPerMobile_IncludeGiocatoreEContenitori`, `ImpostaPosizioneDentro_ImpostaInitemEAzzeraLocation`, `Duplica_NomeUnivoco`, `Elimina_ConRiferimenti_ChiedeConfermaElencandoli`, `FiltroStanzaETipo`.
- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~OggettiViewModelTests"` → PASS.
- [ ] **Step 5: Commit** — `git commit -m "Editor: scheda oggetti"`

### Task 15: Scheda Verbi

**Files:**
- Create: `Editor/ViewModels/VerbiViewModel.cs`, `Editor/Views/VerbiView.axaml(.cs)`, `Tests/VerbiViewModelTests.cs`

**Interfaces:**
- Produces: `public partial class VerbiViewModel : ObservableObject { VerbiViewModel(DocumentoViewModel doc); IEnumerable<VerboViewModel> VerbiFiltrati; AzioneMotore? FiltroAzione; VerboViewModel? Selezionato; IRelayCommand NuovoCommand, EliminaCommand, AggiungiSinonimoCommand; }` — DataGrid comando + ComboBox azione da `CatalogoAzioni.Tutte` (un codice fuori catalogo si mostra come "? (n)" e resta invariato).

- [ ] **Step 1: Scrivi i test che falliscono**: `AggiungiSinonimo_StessaAzioneDopoIlSelezionato`, `FiltroAzione`, `VerboFuoriCatalogo_NonVieneAlterato`.
- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~VerbiViewModelTests"` → PASS.
- [ ] **Step 5: Commit** — `git commit -m "Editor: scheda verbi"`

### Task 16: Scheda Messaggi

**Files:**
- Create: `Editor/ViewModels/MessaggiViewModel.cs`, `Editor/Views/MessaggiView.axaml(.cs)`, `Tests/MessaggiViewModelTests.cs`

**Interfaces:**
- Produces: `public partial class MessaggiViewModel : ObservableObject { MessaggiViewModel(DocumentoViewModel doc, IDialoghi dialoghi); IRelayCommand<MessaggioViewModel> RipristinaCommand; IRelayCommand AggiungiCommand; IAsyncRelayCommand<MessaggioViewModel> EliminaCommand, SuCommand, GiuCommand; }` — dopo ogni eliminazione/spostamento gli `Indice` vengono rinumerati; la conferma cita `{var:Message}=N` (testo in `.resx`).

- [ ] **Step 1: Scrivi i test che falliscono**: `Ripristina_UsaTestoTemplate`, `Ripristina_SenzaTemplate_Disabilitato`, `Aggiungi_InCodaConIndice`, `Elimina_ChiedeConferma_RinumeraIndici`, `Sposta_Annullato_NonCambiaOrdine`, `MessaggiStandard_NonSiAggiungonoNeEliminano` (nessun comando esposto per lo standard).
- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~MessaggiViewModelTests"` → PASS.
- [ ] **Step 5: Commit** — `git commit -m "Editor: scheda messaggi standard e personalizzati"`

### Task 17: Scheda Gioco, Informazioni, lingua dell'interfaccia

**Files:**
- Create: `Editor/Views/GiocoView.axaml(.cs)`, `Editor/Views/InformazioniWindow.axaml(.cs)`, `Tests/GiocoTests.cs`
- Modify: `Editor/ViewModels/IFictionViewModel.cs`, `Editor/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Produces: `IFictionViewModel.GeneraIfidCommand` (`Guid.NewGuid().ToString().ToUpperInvariant()`); `MainWindowViewModel.CreaIFictionCommand` (se `IFiction` è null) ; `MainWindowViewModel.CambiaLinguaCommand(string cultura)` che salva `ImpostazioniUtente.Lingua` e avvisa che vale al riavvio; `LingueDisponibili` = culture per cui esiste un satellite `.resx` + inglese neutro.

Scheda Gioco (§5.6): intro multilinea, `CaricoMassimo` (NumericUpDown 0–255), le sei direzioni, campi iFiction. Informazioni: versione dell'assembly, licenza AGPL-3.0-or-later, contenuto di `THIRD-PARTY-NOTICES.txt`.

- [ ] **Step 1: Scrivi i test che falliscono**: `GeneraIfid_FormatoGuidMaiuscolo`, `CambiaLingua_SalvaImpostazione`, `LingueDisponibili_ContieneItEEn`.
- [ ] **Step 2–4: Verifica fallimento, implementa, verifica** — `--filter "FullyQualifiedName~GiocoTests"` → PASS.
- [ ] **Step 5: Commit** — `git commit -m "Editor: scheda gioco, finestra informazioni, lingua interfaccia"`

---

## Fase D — Distribuzione e documentazione

### Task 18: Pubblicazione, notice di terze parti, documentazione

**Files:**
- Create: `Editor/THIRD-PARTY-NOTICES.txt`
- Modify: `Editor/Editor.csproj` (notice `CopyToOutputDirectory=PreserveNewest`), `CLAUDE.md`, `README.md`, `IFBuilder64 — Manuale per creare una nuova avventura.md`, `IFBuilder64 — Creating a New Adventure User Guide.md`

- [ ] **Step 1: `THIRD-PARTY-NOTICES.txt`** con nome, versione, licenza e URL di ogni pacchetto della tabella §2 della spec più Casasoft.Commodore; ricava l'elenco reale con `dotnet list Editor package --include-transitive` e includi le dipendenze transitive con licenza diversa da MIT (es. SkiaSharp, HarfBuzzSharp). Se una transitiva non è compatibile con (A)GPL, fermati e segnalalo.
- [ ] **Step 2: Verifica la pubblicazione** per i quattro RID:
  Run: `dotnet publish Editor -c Release -r <rid> --self-contained false -o artifacts/editor-<rid>` per `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`.
  Expected: ogni cartella contiene `Casasoft.IFBuilder64.Editor.dll`, `templates/messages.it.json` e `THIRD-PARTY-NOTICES.txt`. Aggiungi `artifacts/` a `.gitignore`.
- [ ] **Step 3: Documentazione** — `CLAUDE.md` e `README.md`: progetti `Builder`, `Editor`, `Tests`, comandi `dotnet run --project Editor`, `dotnet test`, publish; togli "No test project currently exists". Manuali it/en: sezione breve "Uso dell'editor grafico" / "Using the graphical editor" (aprire, mappa, porte, oggetti, compilare con F5).
- [ ] **Step 4: Verifica finale** — `dotnet build` senza errori, `dotnet test Tests/Tests.csproj` → tutto PASS.
- [ ] **Step 5: Commit** — `git commit -m "Editor: notice di terze parti, pubblicazione e documentazione"`

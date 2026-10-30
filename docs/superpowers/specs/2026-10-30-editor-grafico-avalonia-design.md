# Editor grafico per IFBuilder64 — Design

**Data:** 2026-10-30
**Stato:** in revisione
**Licenza dei nuovi progetti:** AGPL-3.0-or-later (come il resto del repository)

## 1. Obiettivo

Un'applicazione desktop multipiattaforma (Windows, Linux, macOS) basata su Avalonia per creare e modificare i file `game.json` di IFBuilder64 e compilarli nei file per Adventure Player 64, senza dover scrivere JSON a mano.

Funzioni richieste:

- creazione della mappa delle stanze su griglia, con livelli per su/giù;
- gestione delle porte direttamente sulla mappa;
- lista degli oggetti e loro collocazione nelle stanze come statici (`notportable`) o mobili;
- lista dei verbi con abbinamento alle azioni del motore;
- messaggi standard e messaggi personalizzati;
- compilazione dei file `.prg` e accessori con lo stesso risultato della CLI;
- interfaccia multilingue (italiano e inglese, estendibile).

Vincolo di licenza: solo dipendenze con licenza compatibile con (A)GPL. Nessun componente di Avalonia Accelerate o altro software commerciale.

### Criteri di successo

- Un gioco creato interamente con l'editor compila e gira in Adventure Player 64.
- Un `game.json` esistente aperto e salvato senza modifiche resta equivalente all'originale, a parte l'aggiunta dei campi `map`.
- La compilazione dall'editor produce gli stessi byte della CLI per lo stesso JSON.

### Fuori ambito (v1)

- Annulla/ripeti.
- Modifica degli hook BASIC (`defs.bas`, `init.bas`, ecc.).
- Avvio dell'emulatore VICE.
- Importazione dei file SEQ legacy (resta compito di `SeqConverter`).
- Pacchetti self-contained e installer.

## 2. Struttura della solution

```
IFBuilder64.slnx
├── Common/          esistente — aggiunta del campo opzionale mappa in Room
├── Builder/         NUOVO class library  → Casasoft.IFBuilder64.Builder
├── IFBuilder64/     esistente — CLI ridotta a guscio sopra Builder
├── Editor/          NUOVO app Avalonia   → Casasoft.IFBuilder64.Editor
├── SeqConverter/    invariato
└── Tests/           NUOVO xUnit          → Casasoft.IFBuilder64.Tests
```

Riferimenti tra progetti:

- `Builder` → `Common`, `Casasoft.Commodore`
- `IFBuilder64` → `Builder`, `Common`, `Casasoft.Helpers`, `Mono.Options`
- `Editor` → `Builder`, `Common`
- `Tests` → `Builder`, `Common`, `Editor`

Convenzioni: target `net10.0`, `Nullable` e `ImplicitUsings` abilitati, intestazione di copyright da `.editorconfig` su ogni `.cs`, `AssemblyName`/`RootNamespace` nel formato `Casasoft.IFBuilder64.<Nome>` come in `Common`. Codice nuovo dell'editor con identificatori e commenti in italiano; il codice spostato da `Program.cs` mantiene i nomi esistenti.

### Dipendenze nuove

| Pacchetto | Licenza | Progetto |
|---|---|---|
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent | MIT | Editor |
| Avalonia.Controls.DataGrid | MIT | Editor |
| CommunityToolkit.Mvvm | MIT | Editor |
| xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | Apache-2.0 / MIT | Tests |
| Avalonia.Headless.XUnit | MIT | Tests |

## 3. Modifiche al codice esistente

### 3.1 Common: posizione sulla mappa

Nuova classe `MapPosition` in `Common/Json/MapPosition.cs`:

```csharp
[Description("Position of a room on the editor map grid.")]
public class MapPosition
{
    [Description("Column on the grid.")]  public int x { get; set; }
    [Description("Row on the grid.")]     public int y { get; set; }
    [Description("Level (floor); 0 = ground, positive = up, negative = down.")]
    public int z { get; set; }
}
```

In `Room`:

```csharp
[Description("Optional position of the room in the graphical editor map.")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public MapPosition? map { get; set; }
```

Il campo è usato solo dall'editor. Il Builder lo ignora e lo schema JSON lo documenta automaticamente.

### 3.2 Estrazione del Builder

Il codice di compilazione di `IFBuilder64/Program.cs` (funzione `BuildGame` e funzioni di supporto come `AllocateDown`) e `ConfigPrg.cs` passano nel progetto `Builder`.

API pubblica:

```csharp
public class GameBuilder
{
    public BuildResult Build(Game game, string outputBase);
}

public class BuildResult
{
    public bool Successo { get; }
    public IReadOnlyList<string> FileGenerati { get; }
    public IReadOnlyList<string> Log { get; }
}
```

Regole:

- Il Builder **non scrive su `Console`**: le righe di log oggi stampate con `Console.WriteLine` vanno in `BuildResult.Log`.
- Il Builder **lavora su una copia** del `Game` (serializzazione e deserializzazione JSON) perché `SortRooms`/`SortItems` modificano le liste; l'istanza del chiamante resta invariata.
- Il parsing del file JSON resta nella CLI; l'editor passa direttamente il `Game` costruito dai ViewModel.
- La CLI conserva opzioni, banner e messaggi attuali: legge il JSON, chiama `GameBuilder.Build` e stampa `Log`.
- Prima dell'estrazione si registra l'output della CLI attuale come riferimento per il test di non regressione (§7.1).

## 4. Architettura dell'Editor

Pattern MVVM con CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`).

### 4.1 ViewModel

- `DocumentoViewModel` — documento aperto: percorso del file, flag `Modificato`, collezioni osservabili di `StanzaViewModel`, `OggettoViewModel`, `PortaViewModel`, `VerboViewModel`, `MessaggioViewModel` (standard e personalizzati), più i campi di gioco (intro, stanza iniziale, `maxload`, iFiction, direzioni).
  - `static DocumentoViewModel DaGame(Game game)` e `Game ToGame()` per la mappatura nei due sensi.
  - L'ordine degli elementi è preservato.
  - I flag `bool?` mantengono `null` se non toccati, così il salvataggio altera il JSON il meno possibile.
- `MainWindowViewModel` — comandi File/Compila/Strumenti, scheda attiva, pannello avvisi/log.
- `MappaViewModel` — livello corrente, zoom, selezione, operazioni sulla griglia.

### 4.2 Servizi

- `ServizioFile` — apertura/salvataggio JSON con le stesse `JsonSerializerOptions` della CLI (indentato, `WhenWritingNull`); creazione di un nuovo gioco tramite `new Game(TemplateLanguage)`.
- `Validatore` — produce `Avviso { Gravita, Messaggio, Elemento }` (§6).
- `DisposizioneAutomatica` — assegna le coordinate alle stanze senza `map` (§5.2).
- `ServizioCompilazione` — esegue `GameBuilder.Build` in background.
- `CatalogoAzioni` — elenco fisso dei codici azione 0–25 con nomi localizzati.

### 4.3 Localizzazione

Stringhe dell'interfaccia in `Editor/Risorse/Testi.resx` (inglese, neutro) e `Testi.it.resx`. Una nuova lingua si aggiunge con un nuovo file `.resx`. La lingua iniziale segue `CultureInfo.CurrentUICulture` e si cambia da **Strumenti → Lingua interfaccia**; la scelta è salvata nelle impostazioni utente e applicata al riavvio. La lingua dell'interfaccia è indipendente dalla lingua del gioco (template).

### 4.4 Impostazioni utente

File JSON nella cartella di configurazione per utente (`Environment.SpecialFolder.ApplicationData/IFBuilder64/editor.json`): lingua, file recenti, ultima cartella di output.

## 5. Interfaccia

### 5.1 Finestra principale

```
┌ File  Modifica  Compila  Strumenti  ? ───────────────────────────┐
│ [Mappa] [Oggetti] [Verbi] [Messaggi] [Gioco]                     │
│ ┌──────────────────────────────────────┬───────────────────────┐ │
│ │  contenuto della scheda              │ pannello proprietà    │ │
│ │                                      │ elemento selezionato  │ │
│ ├──────────────────────────────────────┴───────────────────────┤ │
│ │ Avvisi / output compilazione (comprimibile)                  │ │
└──────────────────────────────────────────────────────────────────┘
```

- **File**: Nuovo (template italiano/inglese), Apri, Salva, Salva con nome, Recenti, Esci. Le modifiche non salvate sono indicate con `*` nel titolo e chiedono conferma alla chiusura e prima di aprire un altro file.
- **Compila** (F5): salva il documento e chiama il Builder. Il nome base di output è di default quello del file JSON nella stessa cartella e si cambia in **Compila → Opzioni**.
- **Strumenti**: Lingua interfaccia.
- **?**: Informazioni (versione, licenza, componenti di terze parti).

### 5.2 Scheda Mappa

Controllo personalizzato `MappaControl` che deriva da `Control` e si disegna con un override di `Render`, con pan e zoom.

- **Barra degli strumenti**: selettore di livello (lista dei livelli esistenti più "+" per aggiungerne uno sopra o sotto), zoom, "Disponi automaticamente".
- **Creazione**: clic su una cella vuota → nuova stanza con nome univoco provvisorio (`stanza1`, `stanza2`…), subito modificabile nel pannello.
- **Spostamento**: trascinamento di una stanza su una cella libera dello stesso livello.
- **Collegamenti orizzontali**: trascinamento dal bordo nord/sud/est/ovest di una stanza a un'altra stanza dello stesso livello → uscita in quella direzione e uscita opposta nella stanza di arrivo. Con Ctrl premuto il collegamento è a senso unico. La stanza di arrivo può anche non essere adiacente; in quel caso il collegamento è disegnato come linea spezzata.
- **Collegamenti verticali**: menu contestuale della stanza → "Collega in alto a…" / "Collega in basso a…", con le stanze dei livelli z+1 / z−1. Sulle celle sono indicati da ▲/▼.
- **Menu contestuale di un collegamento**: "Trasforma in porta" / "Trasforma in passaggio", "Elimina".
- **Simboli**: ★ stanza iniziale; porta = tratto con rettangolino (pieno se chiusa all'inizio, vuoto se aperta); bordo d'avviso per le stanze non raggiungibili dalla stanza iniziale.
- **Pannello proprietà della stanza**: nome, descrizione, "Imposta come stanza iniziale", uscite (sola lettura, cliccabili), oggetti nella stanza divisi in **Statici** e **Mobili**, con i pulsanti "Nuovo oggetto qui" e "Sposta qui…".
- **Pannello proprietà della porta**: nome, descrizione, `denyopen`, `unlockitem` (menu a tendina con gli oggetti), aperta/chiusa.

**Disposizione automatica** (all'apertura, per le stanze senza `map`, o su richiesta):

1. Visita in ampiezza dalla stanza iniziale seguendo uscite e porte.
2. Spostamento della cella in base alla direzione: nord y−1, sud y+1, est x+1, ovest x−1, su z+1, giù z−1.
3. Se la cella è occupata, si usa la cella libera più vicina sullo stesso livello (spirale).
4. Le stanze non raggiungibili vengono messe in fila sotto il riquadro occupato, al livello 0.
5. Le stanze che hanno già `map` non vengono spostate, tranne con "Disponi automaticamente", che chiede conferma e ridispone tutto.

### 5.3 Scheda Oggetti

DataGrid con le colonne nome, tipo (**Statico/Mobile**, cioè `notportable`), posizione e i flag `openable`, `closeable`, `open`, `switchable`, `switchedon`, `container`.

- **Posizione**: un unico menu a tendina con "— nessuna —", "Giocatore" (`location = "player"`), le stanze e "Dentro: <contenitore>" (`initem`). Per gli oggetti statici sono disponibili solo le stanze.
- **Pannello**: descrizione, `opendescription`, `unlockitem` (menu a tendina con gli oggetti).
- Pulsanti Nuovo, Duplica, Elimina; filtro per stanza e per tipo.

### 5.4 Scheda Verbi

DataGrid con comando e azione. L'azione si sceglie da un menu a tendina popolato dal `CatalogoAzioni` (nome localizzato più il codice). Filtro per azione e "Aggiungi sinonimo", che crea un verbo con la stessa azione del verbo selezionato.

Codici (dal manuale): 0–5 movimento (n, s, e, o, alto, basso), 6 vai, 7 guarda/esamina, 8 prendi, 9 lascia, 10 apri, 11 chiudi, 12 accendi, 13 spegni, 14 inserisci, 15 inventario, 16 parola magica, 17 aiuto, 18 elenco comandi, 19 salva, 20 carica, 21 annulla, 22 fine, 23 informazioni, 24 registra comandi, 25 elenca uscite.

### 5.5 Scheda Messaggi

Divisa in due sezioni.

- **Standard** (`messages`): elenco a lunghezza fissa con indice, testo modificabile e testo del template della stessa lingua come suggerimento; pulsante "Ripristina" per il singolo messaggio. Non si possono aggiungere né eliminare messaggi.
- **Personalizzati** (`usermessages`): elenco con indice ben visibile; Aggiungi in fondo, Elimina, Sposta su/giù. Eliminare o spostare chiede conferma e ricorda che gli hook BASIC usano l'indice (`{var:Message}=N`).

### 5.6 Scheda Gioco

Intro (testo multilinea), `maxload`, le sei direzioni, metadati iFiction (titolo, autore, IFID con "Genera IFID", headline, genere, descrizione, lingua, url, email).

## 6. Coerenza, validazione ed errori

### 6.1 Rinomini a cascata

- **Stanza**: aggiornamento di uscite delle stanze, direzioni delle porte, `location` degli oggetti, `initialroom`.
- **Oggetto**: aggiornamento di `unlockitem` su oggetti e porte e di `initem` degli oggetti contenuti. `initem` contiene l'**id**, cioè `nome` per gli oggetti mobili e `nome@stanza` per gli statici: rinominare o spostare un contenitore statico, o rinominare la sua stanza, aggiorna gli `initem` che lo riferiscono.
- **Nome duplicato**: il rinomino viene rifiutato e il campo è evidenziato in rosso con il messaggio d'errore.
- **Eliminazione** di un elemento ancora riferito: conferma con l'elenco dei riferimenti, che vengono svuotati.

### 6.2 Uscite e porte

- "Trasforma in porta": rimuove le due uscite dalle stanze e crea una `Door` con le direzioni corrispondenti (nome provvisorio univoco, chiusa).
- "Trasforma in passaggio": elimina la `Door` e ricrea le uscite nelle stanze.
- All'apertura, se un'uscita è dichiarata sia in una stanza sia in una porta, compare un avviso con l'azione "Correggi", che rimuove l'uscita dalla stanza.

### 6.3 Validatore

Il controllo parte al salvataggio, alla compilazione e, con un ritardo di circa 500 ms, dopo ogni modifica. Gli avvisi vanno nel pannello in basso; con un doppio clic si seleziona l'elemento interessato.

**Errori**, che bloccano la compilazione:

- nessuna stanza;
- `initialroom` vuota o inesistente;
- nomi di stanze, porte o id di oggetti duplicati;
- più di 255 stanze, oggetti o porte (indici validi 0–254; 255 è il valore riservato per "non trovato");
- oggetto statico senza stanza.

**Avvisi**, che non bloccano:

- uscita (di stanza o porta) verso una stanza inesistente;
- `location`, `initem` o `unlockitem` che non puntano a nulla;
- stanza non raggiungibile dalla stanza iniziale;
- uscita dichiarata sia nella stanza sia in una porta;
- verbo con codice fuori dal catalogo;
- numero di messaggi standard diverso da quello del template.

### 6.4 Compilazione

`ServizioCompilazione` esegue `GameBuilder.Build(documento.ToGame(), outputBase)` su un thread in background; durante l'esecuzione il comando Compila è disattivato. Le eccezioni (I/O, libreria Commodore) vengono catturate e riportate come errore nel log, senza chiudere l'applicazione. Al termine il pannello mostra il log e l'elenco dei file generati.

### 6.5 Errori di apertura

Un JSON non valido o non deserializzabile mostra un dialogo con il messaggio del serializzatore (riga/posizione) e non sostituisce il documento corrente.

## 7. Test

Progetto `Tests` (xUnit).

1. **Non regressione del Builder**. Prima dell'estrazione si crea `Tests/Dati/esempio.it.json` (stanze su più livelli, porte, contenitore statico e mobile, oggetti nel giocatore, messaggi personalizzati) e si salva in `Tests/Riferimenti/` l'output della CLI attuale. Dopo l'estrazione, `GameBuilder.Build` deve produrre file identici byte per byte; il `.seq` About si confronta escludendo la data.
2. **Andata e ritorno del modello**. `Game → DocumentoViewModel → Game` restituisce un JSON equivalente per i template `it`/`en` e per l'esempio; un file senza `map` si carica e le coordinate vengono salvate; i flag `null` restano `null`.
3. **Logica**: rinomini a cascata (compreso il caso `nome@stanza`), trasformazione collegamento↔porta, disposizione automatica (nessuna sovrapposizione, stanze isolate nell'area separata), ogni regola del Validatore, il Builder non modifica il `Game` del chiamante.
4. **Interfaccia** (`Avalonia.Headless.XUnit`), test di base: apertura della finestra, caricamento di un documento, F5 compila in una cartella temporanea e il log contiene i file generati. Nessun confronto di immagini.

## 8. Distribuzione

- `dotnet publish -c Release` **framework-dependent** (richiede il runtime .NET 10) per `win-x64`, `linux-x64`, `osx-x64` e `osx-arm64`. Nessun pacchetto self-contained, nessun installer: archivi zip o tar.gz.
- I file di `Common/templates/` vengono copiati nell'output come per la CLI.
- `THIRD-PARTY-NOTICES.txt` con le licenze delle dipendenze, incluso nell'output dell'Editor.

## 9. Documentazione

- `CLAUDE.md` e `README.md`: nuovi progetti `Builder`, `Editor`, `Tests` e comandi di build ed esecuzione.
- Manuali utente (it/en): breve sezione "Uso dell'editor grafico".

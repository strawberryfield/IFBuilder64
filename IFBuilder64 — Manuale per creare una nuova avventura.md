# IFBuilder64 — Manuale per creare una nuova avventura

Oct 26, 2026 · @Roberto

## Introduzione

Con IFBuilder64 un'avventura testuale per Commodore 64 si scrive in un unico file JSON (stanze, oggetti, porte, verbi, messaggi) e il tool lo trasforma nei file dati letti dal motore *Adventure Player 2026*, scritto in BASIC V2 più routine in linguaggio macchina.

Il repository contiene quattro parti:

| Parte | Cartella | Ruolo |
| --- | --- | --- |
| IFBuilder64 | `IFBuilder64/` | Programma .NET a riga di comando: crea il template JSON, valida lo schema e compila il gioco nei file `.prg`/`.seq` |
| Common | `Common/` | Modello JSON (`Game`, `Room`, `Item`, `Door`, `Verb`, `IFiction`) e template di verbi, messaggi e direzioni in italiano e inglese |
| Motore C64 | `C64/` | Loader, parser BASIC, routine LM (`lm.prg`) e gli "hook" personalizzabili in `C64/custom/` |
| SeqConverter | `SeqConverter/` | Converte i vecchi file dati in formato SEQ nel nuovo `game.json` |

Questo manuale segue il percorso tipico: template, scrittura del mondo, logica personalizzata, compilazione e test in emulatore.

## Requisiti e installazione

Servono il .NET 10 SDK per IFBuilder64 e una toolchain C64 per il motore; il builder da solo produce i dati, non il disco giocabile.

| Strumento | A cosa serve | Note |
| --- | --- | --- |
| .NET 10 SDK | Compilare ed eseguire IFBuilder64 | `TargetFramework` = `net10.0` |
| Git | Clonare il repository | Facoltativo |
| Preprocessore BASIC per la sintassi `{uses:}`, `{def:}`, `{:etichetta}` (C64List) | Trasformare `loader.bas` e `parser.bas` in `.prg` | La sintassi dei sorgenti BASIC lo richiede |
| 64tass | Ricompilare `lm.prg` | Solo se modifichi l'assembly; `lm.prg` è già incluso |
| VICE (o altro emulatore) + c1541 | Creare l'immagine `.d64` e provare il gioco | Anche SD2IEC su hardware reale |

1. Clona il repository: `git clone https://github.com/strawberryfield/IFBuilder64.git`
2. Compila: `dotnet build -c Release` dalla cartella principale.
3. Verifica: `dotnet run --project IFBuilder64 -- --help` stampa le opzioni disponibili.

Il comando `--template` legge i file da una sottocartella `templates/` relativa alla **cartella corrente**. La build la copia accanto all'eseguibile (`IFBuilder64/bin/Release/net10.0/templates`), quindi lancia il template da quella cartella oppure copia `templates/` nella cartella del tuo progetto.

Opzioni della riga di comando:

| Opzione | Effetto |
| --- | --- |
| `--template=it` (o `en`) | Crea un `game.json` precompilato con verbi, messaggi e direzioni |
| `-o`, `--out=<nome>` | Nome del file di output (template/schema) o prefisso dei file compilati |
| `--schema` | Scrive lo schema JSON del modello (`schema.json`) |
| `-q`, `--quiet` | Nasconde il banner |
| `-h`, `--help` | Mostra l'aiuto |
| `<file.json>` | Compila il gioco; accetta anche caratteri jolly |

## Concetti fondamentali

Un'avventura è un oggetto JSON con undici proprietà di primo livello; stanze, oggetti e porte si collegano tra loro **per nome**, non per indice.

| Proprietà | Tipo | Contenuto |
| --- | --- | --- |
| `intro` | testo | Schermata iniziale mostrata all'avvio |
| `rooms` | lista di `Room` | Le stanze del mondo |
| `initialroom` | testo | Nome della stanza di partenza |
| `items` | lista di `Item` | Oggetti trasportabili e oggetti di scenario |
| `doors` | lista di `Door` | Passaggi apribili tra due stanze |
| `verbs` | lista di `Verb` | Parole riconosciute dal parser e codice azione associato |
| `directions` | 6 testi | Nomi delle direzioni: nord, sud, est, ovest, alto, basso |
| `messages` | testi | Messaggi di sistema del motore (55, indicizzati da 0) |
| `usermessages` | testi | Messaggi tuoi, richiamabili dagli hook BASIC |
| `maxload` | numero | Oggetti trasportabili contemporaneamente (default 10) |
| `ifiction` | oggetto | Metadati bibliografici (titolo, autore, IFID…) |

Regole che il motore dà per scontate:

- **La stanza `player` è sempre la prima.** Rappresenta l'inventario: un oggetto con `"location": "player"` è in mano al giocatore. Il template la crea già.
- **Il primo oggetto resta il primo.** Il template crea `me` ("In gran forma, come sempre."); non spostarlo.
- **L'ordine nel file non conta.** Il builder ordina le stanze per nome (dopo `player`) e gli oggetti mettendo prima i trasportabili, poi lo scenario, raggruppati per stanza.
- **I nomi sono le parole che il giocatore digita.** Il nome di un oggetto (`lanterna`) è ciò che va scritto dopo il verbo; il nome di una stanza invece è un identificatore interno, mai mostrato.
- **Tutti gli indici stanno in un byte.** Il valore 255 significa "nessuno", quindi stanze, oggetti, porte e verbi devono essere al massimo 254 ciascuno, e la memoria del C64 è il vero limite pratico.
- **Il testo è PETSCII a 40 colonne.** `\r` va a capo, `\f` pulisce lo schermo; i codici colore `\u001c` (rosso), `\u001f` (blu) e `\u0090` (nero, colore del testo) evidenziano parole. Per convenzione gli oggetti esaminabili sono in rosso nelle descrizioni.

## Creare un nuovo progetto

Il punto di partenza è sempre il template: contiene già i 45 verbi, i 55 messaggi di sistema e le 6 direzioni nella lingua scelta, così scrivi solo il mondo.

1. Crea una cartella di lavoro, ad esempio `C:\Avventure\castello`, e copiaci la cartella `templates/` dall'output della build.
2. Genera il template, chiamandolo `<nome>.<lingua>.json`:

   ```
   IFBuilder64 --template=it -o castello.it.json
   ```
3. Genera lo schema per avere completamento e validazione nell'editor:

   ```
   IFBuilder64 --schema -o schema.json
   ```

   In VS Code aggiungi in testa al file `"$schema": "./schema.json"`, oppure associalo in `settings.json` con `json.schemas`.
4. Compila `intro`, `initialroom` e `ifiction` (titolo, autore, `ifid`). Un IFID valido è un GUID maiuscolo, ad esempio generato con `[guid]::NewGuid()` in PowerShell.
5. Aggiungi stanze, oggetti e porte (sezione successiva), poi compila e prova spesso: ogni compilazione richiede meno di un secondo.

La convenzione `<nome>.<lingua>` conta: il loader C64 compone il nome dei file come `nome.it` o `nome.en` in base alla lingua scelta all'avvio, quindi un gioco bilingue è fatto di due JSON gemelli (`castello.it.json` e `castello.en.json`) con gli stessi nomi interni di stanze e oggetti.

Scheletro minimo dopo il template (verbi e messaggi omessi):

```json
{
  "intro": "\fIL CASTELLO\r\rSei davanti al ponte levatoio...",
  "initialroom": "ponte",
  "maxload": 6,
  "rooms": [
    { "name": "player", "description": "" },
    { "name": "ponte", "description": "Sei sul ponte levatoio." }
  ],
  "items": [
    { "name": "me", "description": "In gran forma, come sempre.", "location": "player" }
  ],
  "doors": [],
  "usermessages": [],
  "ifiction": {
    "title": "Il castello",
    "author": "Mario Rossi",
    "ifid": "3F2A9C1E-5B7D-4E8A-9C21-7D4E5F6A8B90",
    "language": "it",
    "firstpublished": 2026
  }
}
```

## Definire il mondo di gioco

Il mondo si costruisce in quattro liste: stanze con le uscite libere, porte per i passaggi che si aprono, oggetti, e verbi/messaggi se vuoi personalizzare quelli del template.

### Stanze

Ogni stanza ha `name` (identificatore interno), `description` (testo mostrato) e fino a sei uscite `north`, `south`, `east`, `west`, `up`, `down`, il cui valore è il **nome** della stanza di destinazione.

```json
{ "name": "cortile", "description": "Sei nel cortile. Al centro c'è un \u001cpozzo\u0090.",
  "north": "torre", "south": "ponte" }
```

- Le uscite sono a senso unico: per tornare indietro dichiara anche l'uscita inversa nella stanza di arrivo.
- Un nome di destinazione scritto male non dà errore: l'uscita diventa semplicemente inesistente (255).
- Il motore elenca da solo uscite e oggetti visibili dopo la descrizione; non ripeterli nel testo.

### Porte

Una porta collega due stanze e all'inizio è chiusa. Ogni proprietà di direzione indica **da quale stanza** si attraversa la porta andando in quella direzione.

```json
{ "name": "portone", "description": "Un portone di quercia con borchie di ferro.",
  "denyopen": "È chiuso a chiave.", "unlockitem": "chiave", "open": false,
  "north": "cortile", "south": "torre" }
```

Qui dal `cortile` si va a nord nella `torre` e dalla `torre` a sud nel `cortile`, ma solo dopo `apri portone`. Le regole:

- **Non dichiarare l'uscita nelle stanze**: è la porta, quando si apre, a scrivere le due uscite; quando si chiude le cancella.
- `unlockitem` è il **nome** dell'oggetto che il giocatore deve avere con sé per aprire; senza, vede `denyopen` (o il messaggio di sistema 27).
- Il nome della porta si usa come oggetto dei verbi (`esamina portone`, `apri portone`, `chiudi portone`).

### Oggetti

Gli oggetti sono di due tipi: **trasportabili** (si prendono e si lasciano) e **di scenario** (`"notportable": true`, restano nella stanza e si cercano solo lì).

| Campo | Significato |
| --- | --- |
| `name` | Parola che il giocatore digita |
| `description` | Testo di `esamina` |
| `location` | Nome della stanza in cui si trova; `player` = in mano al giocatore |
| `initem` | Id del contenitore che lo contiene (al posto di `location`) |
| `notportable` | `true` = oggetto di scenario |
| `container` | Può contenere altri oggetti (`inserisci`) |
| `openable` / `closeable` / `open` | Si può aprire / chiudere / è aperto all'inizio |
| `opendescription` | Testo mostrato all'apertura e a `esamina` quando è aperto |
| `switchable` / `switchedon` | Si accende e spegne / è acceso all'inizio |
| `unlockitem` | Nome dell'oggetto necessario per aprirlo o accenderlo |

L'**id** di un oggetto è il nome per i trasportabili e `nome@stanza` per quelli di scenario: due stanze possono quindi avere entrambe un `tavolo` di scenario. L'id si usa in `initem` e negli hook BASIC.

```json
{ "name": "baule", "description": "Un vecchio baule chiuso.",
  "opendescription": "Il baule è aperto.", "location": "torre",
  "notportable": true, "container": true, "openable": true, "closeable": true,
  "unlockitem": "chiave" },
{ "name": "pergamena", "description": "C'è scritto: XYZZY non funziona.",
  "initem": "baule@torre" }
```

Un oggetto dentro un contenitore diventa visibile solo quando il contenitore è aperto. I nomi di tutti gli oggetti trasportabili devono essere distinti tra loro.

### Verbi

Ogni verbo è una coppia `command` (parola digitata) + `action` (codice dell'azione del motore). I sinonimi sono semplicemente più verbi con lo stesso codice.

| Codice | Azione | Verbi del template italiano |
| --- | --- | --- |
| 0–5 | Movimento | `n`/`nord`, `s`/`sud`, `e`/`est`, `o`/`w`/`ovest`, `a`/`alto`, `b`/`basso` |
| 6 | Vai (con direzione) | `v`, `vai` |
| 7 | Guarda / esamina | `g`, `guarda`, `x`, `esamina` |
| 8 / 9 | Prendi / lascia | `p`, `t`, `prendi` / `l`, `d`, `lascia` |
| 10 / 11 | Apri / chiudi | `apri` / `chiudi` |
| 12 / 13 | Accendi / spegni | `accendi` / `spegni` |
| 14 | Inserisci in un contenitore | `inserisci` |
| 15 | Inventario | `i`, `inventario` |
| 16 | Parola magica | `xyzzy`, `abracadabra` |
| 17 / 18 | Aiuto / elenco comandi | `aiuto`, `help` / `comandi` |
| 19 / 20 | Salva / carica | `salva` / `carica` |
| 21 | Annulla (fino a 4 mosse) | `undo`, `annulla` |
| 22 | Fine | `fine` |
| 23 | Informazioni sul gioco | `info` |
| 24 | Registra i comandi su disco | `registra` |
| 25 | Elenca le uscite | `uscite` |

Puoi aggiungere sinonimi o rimuovere verbi, ma non inventare codici nuovi: un codice fuori tabella produce "Non so come svolgere questa azione".

### Messaggi

`messages` sono i 55 testi del motore e vanno lasciati **nello stesso ordine**, perché il parser li richiama per posizione (0 = "Non ne vedo qui intorno...", 43 = "Hai vinto!", 44 = "Hai perso"). Puoi cambiarne il testo. `usermessages` invece è tuo: la posizione di ciascun testo è il numero con cui lo richiami dagli hook.

## Logica di gioco: gli hook BASIC

Il JSON descrive il mondo; enigmi, condizioni di vittoria ed eventi si scrivono in BASIC nei file di `C64/custom/`, che il preprocessore inserisce in punti precisi di `parser.bas`.

| File | Quando viene eseguito | Variabili utili |
| --- | --- | --- |
| `defs.bas` | In compilazione, prima di tutto | Le tue `{def:}` e `{var:}` |
| `init.bas` | All'avvio, dopo l'impostazione dei colori | Puoi ridefinire `{var:ColorText}`, `{var:ColorObject}`… |
| `pre_going.bas` | Ogni volta che una stanza viene mostrata (ingresso, `guarda`, ripristino) | `{var:CurrentLocation}` |
| `pre_examine.bas` | Prima di `esamina <oggetto>` | `{var:ObjectId}`, `{var:DoorId}` |
| `pre_takedrop.bas` | Prima di `prendi` / `lascia` | `{var:ObjectId}`; `tl` = stanza corrente (prendi) o 0 (lascia) |
| `pre_openclose.bas` | Prima di `apri` / `chiudi` (oggetti e porte) | `tl` = 4 apri, 8 chiudi |
| `post_open.bas` | Dopo che un oggetto si è aperto | `{var:ObjectId}` |
| `pre_open_door.bas` | Prima di aprire una porta chiusa | `{var:DoorId}` |
| `pre_switch.bas` | Prima di `accendi` / `spegni` | `tl` = 0 accendi, 1 spegni |
| `pre_insert.bas` | Prima di `inserisci` | `{var:ObjectId}` = oggetto da inserire |

Con `{var:ObjectId}` e `{var:DoorId}` il valore 255 (`W`) significa "non trovato". Le costanti `Z`=0, `U`=1, `W`=255 sono già definite.

### Riferirsi a stanze e oggetti

La compilazione genera `<nome>.def.bas` con una costante per ogni stanza e oggetto:

```
{def:room_cortile=3}
{def:item_chiave=2}
{def:item_baule@torre=7}
```

Nel codice le usi con `{usedef:room_cortile}`. Gli hook includono `bin/game.def.bas`: rinomina il riferimento (o copia il file) in modo che punti al `.def.bas` del tuo gioco.

### Leggere e cambiare lo stato

| Dato | Lettura | Valori |
| --- | --- | --- |
| Posizione di un oggetto | `peek({var:itemsdbaddr}+{usedef:item_chiave})` | 0 = in mano al giocatore, altrimenti indice stanza, 255 = in un contenitore o fuori gioco |
| Stato di un oggetto | `peek({var:itemsoptionsdbaddr}+id)` | bit 1 scenario, 2 aperto, 4 apribile, 8 chiudibile, 16 acceso, 32 accendibile, 64 contenitore |
| Porta aperta | `peek({var:doorsoptionsdbaddr}+indice)` | 1 aperta, 0 chiusa (indice = posizione nella lista `doors`) |

Per far comparire un oggetto nella stanza corrente basta `poke {var:itemsdbaddr}+{usedef:item_chiave}, {var:CurrentLocation}`. Se la modifica deve essere annullabile, chiama prima `gosub {:save_undo}`.

### Uscire dall'hook

- **Lasciar proseguire** l'azione normale: non fare nulla, l'hook termina sull'etichetta `{:..._continue}`.
- **Mostrare un tuo messaggio e bloccare l'azione**: `{var:Message}=0: goto {:show_user_message}` (0 = primo elemento di `usermessages`).
- **Vittoria / sconfitta**: `goto {:end_win}` o `goto {:end_loose}`; il motore propone annulla, nuova partita, carica o fine.

### Esempi

Vittoria entrando nella sala del tesoro (`pre_going.bas`):

```
	if {var:CurrentLocation}={usedef:room_tesoro} then {:end_win}
```

La chiave compare esaminando il pozzo la prima volta (`pre_examine.bas`):

```
	if {var:ObjectId}<>{usedef:item_pozzo@cortile} then {:pre_examine_continue}
	if peek({var:itemsdbaddr}+{usedef:item_chiave})<>W then {:pre_examine_continue}
	gosub {:save_undo}: poke {var:itemsdbaddr}+{usedef:item_chiave}, {var:CurrentLocation}
	{var:Message}=1: goto {:show_user_message}
```

Per questo secondo esempio la chiave va dichiarata senza `location` (così parte da 255, fuori gioco) e `usermessages[1]` contiene il testo, ad esempio "In fondo al pozzo luccica una \\u001cchiave\\u0090!".

## Compilare, creare il disco e provare

Un comando trasforma il JSON in nove file; poi si adattano loader e hook, si preprocessano i sorgenti BASIC e si mette tutto su un'immagine `.d64`.

### 1. Compilare i dati

```
IFBuilder64 castello.it.json
```

Senza `-o` il prefisso dei file è il nome del JSON senza estensione (`castello.it.`). Il builder stampa un rigo per ogni file creato:

| File | Contenuto | Nome sul disco |
| --- | --- | --- |
| `castello.it.in.prg` | Testo introduttivo | `castello.it.in` |
| `castello.it.cf.prg` | Configurazione: numeri di entità e indirizzi (caricata a $02A7) | `castello.it.cf` |
| `castello.it.dt.prg` | Tutti i testi: messaggi, descrizioni, nomi, verbi, direzioni | `castello.it.dt` |
| `castello.it.db.prg` | Stato iniziale: uscite, posizioni e stati di oggetti e porte (fino a $C9FF) | `castello.it.db` |
| `castello.it.ab.seq` | Dati per il comando `info` (titolo, autore, IFID, data) | `castello.it.ab` (SEQ) |
| `castello.it.ifid.seq` | IFID in maiuscolo | facoltativo |
| `castello.it.iFiction.xml` | Scheda bibliografica Babel/iFiction per archivi IF | non va sul disco |
| `castello.it.mem.bas` | Abbassa la fine della RAM BASIC per lasciare spazio ai dati | incluso dal loader |
| `castello.it.def.bas` | Costanti `room_*` e `item_*` per gli hook | incluso dagli hook |

I testi crescono verso il basso a partire da $BFFF: se il gioco è troppo grande, la RAM lasciata al BASIC si riduce fino a non bastare più.

### 2. Adattare loader e hook

`C64/loader.bas` contiene ancora i riferimenti al gioco d'esempio dell'autore. Da modificare:

- `{uses:bin/latinorum.it.mem.bas}` → il tuo `mem.bas` (in un gioco bilingue usa quello della lingua con più testo);
- `"latinorum."` in `{:intro1}` → `"castello."`;
- le righe di presentazione ("The Strawberry Field presenta", nome dell'autore) in `{:intro1}` e `{:intro2}`;
- il riferimento `bin/game.def.bas` negli hook di `C64/custom/`.

Il simbolo `sd2iec` (definito con `{def:sd2iec}`) esclude il fast loader "star turbo", che non è incluso nel repository.

### 3. Preprocessare il BASIC

Converti `loader.bas` e `parser.bas` in programmi `.prg` con il preprocessore, dalla cartella `C64/` così che i percorsi `{uses:}` relativi si risolvano. `lm/lm.prg` è già compilato; serve 64tass (`make` in `C64/lm`) solo se cambi l'assembly.

### 4. Creare il disco

Il loader carica nell'ordine: `lm`, `<gioco>.in`, `<gioco>.cf`, `cover`, `<gioco>.dt`, poi digita da solo `LOAD"PARSER",8` + `RUN`; il parser carica `<gioco>.db`. Con c1541 di VICE:

```
c1541 -format "castello,01" d64 castello.d64 ^
  -write loader.prg castello ^
  -write lm\lm.prg lm ^
  -write parser.prg parser ^
  -write cover.prg cover ^
  -write castello.it.in.prg castello.it.in ^
  -write castello.it.cf.prg castello.it.cf ^
  -write castello.it.dt.prg castello.it.dt ^
  -write castello.it.db.prg castello.it.db ^
  -write castello.it.ab.seq "castello.it.ab,s"
```

Ripeti le ultime cinque righe con `castello.en.*` per la versione inglese. Il file `cover` (schermata di copertina copiata a $A000) non è nel repository: va preparato a parte.

### 5. Provare in VICE

`x64sc -autostart castello.d64` avvia il loader. Scegli la lingua con `I` o `E`, poi prova almeno: `comandi`, ogni uscita di ogni stanza, ogni porta chiusa e aperta, `prendi`/`lascia` fino a `maxload`, `annulla`, `salva` e `carica`. Il comando `registra` scrive su disco i comandi digitati: utile per ripetere una partita di test dopo ogni modifica.

## Esempio completo: "Il castello"

Quattro stanze, una porta, un enigma e una condizione di vittoria: il giocatore esamina il pozzo, trova la chiave, apre il portone, prende la pergamena dal baule e sale in cima alla torre.

Mappa:

```
            [sommita]
                | alto/basso
             [torre]   (baule con pergamena)
                | portone (chiave)
            [cortile]  (pozzo: rivela la chiave)
                |
             [ponte]   <- partenza
```

`castello.it.json` (le liste `verbs`, `messages` e `directions` restano quelle del template):

```json
{
  "intro": "\fIL CASTELLO\r\rUn antico castello abbandonato. Dicono che dalla cima della torre si veda il mare.\r",
  "initialroom": "ponte",
  "maxload": 5,
  "rooms": [
    { "name": "player", "description": "" },
    { "name": "ponte", "description": "Sei sul ponte levatoio. A nord si apre il cortile.",
      "north": "cortile" },
    { "name": "cortile", "description": "Un cortile invaso dalle erbacce. Al centro c'è un vecchio \u001cpozzo\u0090.",
      "south": "ponte" },
    { "name": "torre", "description": "La base della torre. Una scala sale verso l'alto. In un angolo c'è un \u001cbaule\u0090.",
      "up": "sommita" },
    { "name": "sommita", "description": "La cima della torre. All'orizzonte brilla il mare.",
      "down": "torre" }
  ],
  "doors": [
    { "name": "portone", "description": "Un portone di quercia con borchie di ferro.",
      "denyopen": "Il portone è chiuso a chiave.", "unlockitem": "chiave", "open": false,
      "north": "cortile", "south": "torre" }
  ],
  "items": [
    { "name": "me", "description": "In gran forma, come sempre.", "location": "player" },
    { "name": "chiave", "description": "Una grossa chiave arrugginita." },
    { "name": "pergamena", "description": "C'è scritto: la vista migliore è in alto.",
      "initem": "baule@torre" },
    { "name": "pozzo", "description": "Un pozzo profondo e asciutto.",
      "location": "cortile", "notportable": true },
    { "name": "baule", "description": "Un baule di legno, chiuso.", "opendescription": "Il baule è aperto.",
      "location": "torre", "notportable": true, "container": true,
      "openable": true, "closeable": true }
  ],
  "usermessages": [
    "Senza la pergamena non sai cosa cercare quassù. Torni indietro.",
    "In fondo al pozzo luccica una \u001cchiave\u0090!"
  ],
  "ifiction": {
    "title": "Il castello", "author": "Mario Rossi", "language": "it",
    "ifid": "3F2A9C1E-5B7D-4E8A-9C21-7D4E5F6A8B90", "firstpublished": 2026
  }
}
```

La chiave non ha `location`: parte fuori gioco e la fa comparire l'hook di `pre_examine.bas` mostrato nella sezione sulla logica. Il portone non compare tra le uscite di `cortile` e `torre` perché le scrive lui quando si apre.

`pre_going.bas` — in cima si vince solo con la pergamena in mano:

```
{uses:bin/castello.it.def.bas}
'
	if {var:CurrentLocation}<>{usedef:room_sommita} then {:pre_going_continue}
	if peek({var:itemsdbaddr}+{usedef:item_pergamena})=Z then {:end_win}
	{usedef:print_string}, {var:usrmsgaddr}, 0: print
	{var:CurrentLocation}={usedef:room_torre}: poke {var:curroomdbaddr},{var:CurrentLocation}

{:pre_going_continue}
```

Soluzione: `n`, `esamina pozzo`, `prendi chiave`, `apri portone`, `n`, `apri baule`, `prendi pergamena`, `a`.

## Risoluzione problemi

Quasi tutti gli errori nascono da un nome scritto in modo diverso in due punti: il builder non segnala riferimenti mancanti, li trasforma in 255.

| Sintomo | Causa probabile | Soluzione |
| --- | --- | --- |
| `--template` termina con file non trovato | Manca `templates/` nella cartella corrente | Lancia dalla cartella dell'eseguibile o copia `templates/` |
| `Unknown template language` | Lingua non riconosciuta | Usa `it`, `italiano`, `italian`, `en`, `english` o `inglese` |
| `Unable to parse game file` o eccezione JSON | JSON non valido o campo obbligatorio mancante | `name` e `description` sono obbligatori per stanze, oggetti e porte; valida con lo schema |
| Un'uscita non compare | Nome della stanza di destinazione errato | Controlla maiuscole e spazi: il confronto è esatto |
| La porta non si apre mai | `unlockitem` non corrisponde al `name` dell'oggetto, o l'oggetto non è in mano | Il giocatore deve *portare* la chiave, non basta che sia nella stanza |
| La porta aperta all'inizio non fa passare | Con `"open": true` le uscite non vengono scritte | Dichiara le uscite anche nelle due stanze |
| L'oggetto nel contenitore non si vede | Contenitore chiuso o `initem` senza `@stanza` per lo scenario | Usa l'id completo, es. `baule@torre` |
| "Non ne vedo qui intorno..." su un oggetto di scenario | Oggetto di scenario cercato in un'altra stanza | Lo scenario si trova solo nella sua `location` |
| Messaggi di sistema sbagliati | Lista `messages` riordinata o accorciata | Rigenera il template e ricopia solo i testi |
| `?OUT OF MEMORY` sul C64 | Testi troppo lunghi: i dati hanno eroso la RAM BASIC | Accorcia descrizioni e messaggi, riduci gli hook |
| `info` dà errore disco | Manca il file `<gioco>.ab` (SEQ) sul disco | Aggiungilo con `,s` in c1541; serve `ifiction` nel JSON |

### Convertire un gioco esistente

Se hai un'avventura nel vecchio formato SEQ dell'Adventure Player, `SeqConverter --template=it vecchio.seq` crea `vecchio.json` con stanze (numerate 1, 2, 3…), oggetti e verbi già compilati, da rifinire a mano.

### Riferimenti

- [Repository IFBuilder64](https://github.com/strawberryfield/IFBuilder64) — sorgenti, template in `Common/templates/`, motore in `C64/`
- [Sito dell'autore, Roberto Ceccarelli](https://strawberryfield.altervista.org) — Casasoft
- [Babel / iFiction](https://babel.ifarchive.org) — formato dei metadati e IFID
- [VICE](https://vice-emu.sourceforge.io) — emulatore e `c1541`
- Licenza: GNU AGPL 3.0 o successiva

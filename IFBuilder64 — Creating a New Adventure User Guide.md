# IFBuilder64 — Creating a New Adventure: User Guide

Oct 26, 2026 · @Roberto

## Introduction

With IFBuilder64 you write a Commodore 64 text adventure as a single JSON file (rooms, items, doors, verbs, messages), and the tool turns it into the data files read by the *Adventure Player 2026* engine, written in BASIC V2 plus machine-language routines.

The repository has four parts:

| Part | Folder | Role |
| --- | --- | --- |
| IFBuilder64 | `IFBuilder64/` | .NET command-line program: creates the JSON template, exports the schema and builds the game into `.prg`/`.seq` files |
| Common | `Common/` | JSON model (`Game`, `Room`, `Item`, `Door`, `Verb`, `IFiction`) and English/Italian templates for verbs, messages and directions |
| C64 engine | `C64/` | Loader, BASIC parser, ML routines (`lm.prg`) and the customizable hooks in `C64/custom/` |
| SeqConverter | `SeqConverter/` | Converts old SEQ-format data files into the new `game.json` |

This guide follows the usual path: template, writing the world, custom logic, building and testing in an emulator.

## Requirements and installation

You need the .NET 10 SDK for IFBuilder64 and a C64 toolchain for the engine; the builder on its own produces the data, not a playable disk.

| Tool | Purpose | Notes |
| --- | --- | --- |
| .NET 10 SDK | Build and run IFBuilder64 | `TargetFramework` = `net10.0` |
| Git | Clone the repository | Optional |
| BASIC preprocessor for `{uses:}`, `{def:}`, `{:label}` syntax (C64List) | Turn `loader.bas` and `parser.bas` into `.prg` files | Required by the BASIC source syntax |
| 64tass | Rebuild `lm.prg` | Only if you change the assembly; `lm.prg` is already included |
| VICE (or another emulator) + c1541 | Create the `.d64` image and test the game | SD2IEC on real hardware also works |

1. Clone the repository: `git clone https://github.com/strawberryfield/IFBuilder64.git`
2. Build: `dotnet build -c Release` from the root folder.
3. Check: `dotnet run --project IFBuilder64 -- --help` prints the available options.

`--template` reads its files from a `templates/` subfolder of the **current directory**. The build copies it next to the executable (`IFBuilder64/bin/Release/net10.0/templates`), so either run the template command from there or copy `templates/` into your project folder.

Command-line options:

| Option | Effect |
| --- | --- |
| `--template=en` (or `it`) | Creates a `game.json` prefilled with verbs, messages and directions |
| `-o`, `--out=<name>` | Output file name (template/schema) or prefix of the built files |
| `--schema` | Writes the JSON schema of the model (`schema.json`) |
| `-q`, `--quiet` | Hides the banner |
| `-h`, `--help` | Shows help |
| `<file.json>` | Builds the game; wildcards are accepted |

## Core concepts

An adventure is a JSON object with eleven top-level properties; rooms, items and doors refer to each other **by name**, not by index.

| Property | Type | Content |
| --- | --- | --- |
| `intro` | text | Opening screen shown at startup |
| `rooms` | list of `Room` | The rooms of the world |
| `initialroom` | text | Name of the starting room |
| `items` | list of `Item` | Portable items and scenery |
| `doors` | list of `Door` | Openable passages between two rooms |
| `verbs` | list of `Verb` | Words recognized by the parser and their action code |
| `directions` | 6 texts | Direction names: north, south, east, west, up, down |
| `messages` | texts | Engine system messages (55, indexed from 0) |
| `usermessages` | texts | Your own messages, called from the BASIC hooks |
| `maxload` | number | How many items the player can carry at once (default 10) |
| `ifiction` | object | Bibliographic metadata (title, author, IFID…) |

Rules the engine takes for granted:

- **The `player` room always comes first.** It represents the inventory: an item with `"location": "player"` is being carried. The template already creates it.
- **The first item stays first.** The template creates `me` ("As good-looking as ever."); don't move it.
- **Order in the file doesn't matter.** The builder sorts rooms by name (after `player`) and items with portable ones first, then scenery, grouped by room.
- **Names are what the player types.** An item's name (`lamp`) is the word typed after the verb; a room's name is an internal identifier, never shown.
- **Every index fits in one byte.** The value 255 means "none", so rooms, items, doors and verbs are capped at 254 each, and C64 memory is the real practical limit.
- **Text is 40-column PETSCII.** `\r` is a line break, `\f` clears the screen; the color codes `\u001c` (red), `\u001f` (blue) and `\u0090` (black, the text color) highlight words. By convention, examinable items are shown in red in descriptions.

## Creating a new project

Always start from the template: it already holds the verbs (38 in English, 45 in Italian), the 55 system messages and the 6 directions in the chosen language, so you only write the world.

1. Create a working folder, e.g. `C:\Adventures\castle`, and copy the `templates/` folder from the build output into it.
2. Generate the template, naming it `<name>.<language>.json`:

   ```
   IFBuilder64 --template=en -o castle.en.json
   ```
3. Generate the schema to get completion and validation in your editor:

   ```
   IFBuilder64 --schema -o schema.json
   ```

   In VS Code add `"$schema": "./schema.json"` at the top of the file, or map it in `settings.json` via `json.schemas`.
4. Fill in `intro`, `initialroom` and `ifiction` (title, author, `ifid`). A valid IFID is an uppercase GUID, for example generated with `[guid]::NewGuid()` in PowerShell.
5. Add rooms, items and doors (next section), then build and test often: each build takes under a second.

The `<name>.<language>` convention matters: the C64 loader builds file names as `name.en` or `name.it` based on the language picked at startup, so a bilingual game is two twin JSON files (`castle.en.json` and `castle.it.json`) sharing the same internal room and item names.

Minimal skeleton after the template (verbs and messages omitted):

```json
{
  "intro": "\fTHE CASTLE\r\rYou stand before the drawbridge...",
  "initialroom": "bridge",
  "maxload": 6,
  "rooms": [
    { "name": "player", "description": "" },
    { "name": "bridge", "description": "You are on the drawbridge." }
  ],
  "items": [
    { "name": "me", "description": "As good-looking as ever.", "location": "player" }
  ],
  "doors": [],
  "usermessages": [],
  "ifiction": {
    "title": "The Castle",
    "author": "John Smith",
    "ifid": "3F2A9C1E-5B7D-4E8A-9C21-7D4E5F6A8B90",
    "language": "en",
    "firstpublished": 2026
  }
}
```

## Defining the game world

The world is built from four lists: rooms with their free exits, doors for passages that open, items, and verbs/messages if you want to customize the template ones.

### Rooms

Each room has a `name` (internal identifier), a `description` (text shown) and up to six exits `north`, `south`, `east`, `west`, `up`, `down`, whose value is the **name** of the destination room.

```json
{ "name": "courtyard", "description": "You are in the courtyard. In the middle there is a \u001cwell\u0090.",
  "north": "tower", "south": "bridge" }
```

- Exits are one-way: to come back, also declare the reverse exit in the destination room.
- A misspelled destination name raises no error: the exit simply doesn't exist (255).
- The engine lists visible exits and items on its own after the description; don't repeat them in the text.

### Doors

A door links two rooms and starts closed. Each direction property states **from which room** you go through the door when moving in that direction.

```json
{ "name": "gate", "description": "An oak gate studded with iron.",
  "denyopen": "The gate is locked.", "unlockitem": "key", "open": false,
  "north": "courtyard", "south": "tower" }
```

Here going north from the `courtyard` leads to the `tower`, and going south from the `tower` leads to the `courtyard`, but only after `open gate`. The rules:

- **Don't declare the exit in the rooms**: the door writes both exits when it opens and clears them when it closes.
- `unlockitem` is the **name** of the item the player must be carrying to open it; without it they see `denyopen` (or system message 27).
- The door's name is used as the object of verbs (`examine gate`, `open gate`, `close gate`).

### Items

Items come in two kinds: **portable** (can be taken and dropped) and **scenery** (`"notportable": true`, they stay in their room and are only found there).

| Field | Meaning |
| --- | --- |
| `name` | Word the player types |
| `description` | Text for `examine` |
| `location` | Name of the room where it is; `player` = carried by the player |
| `initem` | Id of the container holding it (instead of `location`) |
| `notportable` | `true` = scenery |
| `container` | Can hold other items (`insert`) |
| `openable` / `closeable` / `open` | Can be opened / closed / starts open |
| `opendescription` | Text shown when opened and on `examine` while open |
| `switchable` / `switchedon` | Can be switched on and off / starts switched on |
| `unlockitem` | Name of the item needed to open or switch it on |

An item's **id** is its name for portable items and `name@room` for scenery: two rooms can therefore both have a scenery `table`. The id is used in `initem` and in the BASIC hooks.

```json
{ "name": "chest", "description": "An old closed chest.",
  "opendescription": "The chest is open.", "location": "tower",
  "notportable": true, "container": true, "openable": true, "closeable": true,
  "unlockitem": "key" },
{ "name": "scroll", "description": "It reads: XYZZY won't help you.",
  "initem": "chest@tower" }
```

An item inside a container becomes visible only when the container is open. All portable item names must be distinct.

### Verbs

Each verb is a `command` (the word typed) + `action` (engine action code) pair. Synonyms are simply several verbs with the same code.

| Code | Action | English template verbs |
| --- | --- | --- |
| 0–5 | Movement | `n`/`north`, `s`/`south`, `e`/`east`, `w`/`west`, `u`/`up`, `down` |
| 6 | Go (with a direction) | `g`, `go` |
| 7 | Look / examine | `l`, `look`, `x`, `examine` |
| 8 / 9 | Take / drop | `t`, `take` / `d`, `drop` |
| 10 / 11 | Open / close | `open` / `close` |
| 12 / 13 | Switch on / off | `power` / `unpower` |
| 14 | Insert into a container | `insert` |
| 15 | Inventory | `i`, `inventory` |
| 16 | Magic word | `xyzzy` |
| 17 / 18 | Help / list commands | `help` / `commands` |
| 19 / 20 | Save / load | `save` / `load` |
| 21 | Undo (up to 4 moves) | `undo` |
| 22 | Quit | `quit` |
| 23 | About the game | `about` |
| 24 | Record commands to disk | `record` |
| 25 | List exits | `exits` |

You can add synonyms (for example `get` with code 8) or remove verbs, but don't invent new codes: an unknown code produces the "don't know how to do that" message.

### Messages

`messages` are the engine's 55 texts and must stay **in the same order**, because the parser calls them by position (0 = "I don't see any around here...", 43 = "You win!", 44 = "You lost."). You can change their wording. `usermessages` is yours: each text's position is the number you use to call it from the hooks.

## Game logic: the BASIC hooks

The JSON describes the world; puzzles, win conditions and events are written in BASIC in the files under `C64/custom/`, which the preprocessor inserts at specific points of `parser.bas`.

| File | When it runs | Useful variables |
| --- | --- | --- |
| `defs.bas` | At build time, before everything else | Your own `{def:}` and `{var:}` |
| `init.bas` | At startup, after colors are set | You can redefine `{var:ColorText}`, `{var:ColorObject}`… |
| `pre_going.bas` | Every time a room is shown (entering, `look`, restore) | `{var:CurrentLocation}` |
| `pre_examine.bas` | Before `examine <item>` | `{var:ObjectId}`, `{var:DoorId}` |
| `pre_takedrop.bas` | Before `take` / `drop` | `{var:ObjectId}`; `tl` = current room (take) or 0 (drop) |
| `pre_openclose.bas` | Before `open` / `close` (items and doors) | `tl` = 4 open, 8 close |
| `post_open.bas` | After an item has opened | `{var:ObjectId}` |
| `pre_open_door.bas` | Before opening a closed door | `{var:DoorId}` |
| `pre_switch.bas` | Before `power` / `unpower` | `tl` = 0 on, 1 off |
| `pre_insert.bas` | Before `insert` | `{var:ObjectId}` = item being inserted |

For `{var:ObjectId}` and `{var:DoorId}`, 255 (`W`) means "not found". The constants `Z`=0, `U`=1, `W`=255 are already defined.

### Referring to rooms and items

The build generates `<name>.def.bas` with one constant per room and item:

```
{def:room_courtyard=3}
{def:item_key=2}
{def:item_chest@tower=7}
```

In code you use them as `{usedef:room_courtyard}`. The hooks include `bin/game.def.bas`: change that reference (or copy the file) so it points at your game's `.def.bas`.

### Reading and changing state

| Data | Read with | Values |
| --- | --- | --- |
| Item location | `peek({var:itemsdbaddr}+{usedef:item_key})` | 0 = carried by the player, otherwise room index, 255 = in a container or out of play |
| Item state | `peek({var:itemsoptionsdbaddr}+id)` | bit 1 scenery, 2 open, 4 openable, 8 closeable, 16 switched on, 32 switchable, 64 container |
| Door open | `peek({var:doorsoptionsdbaddr}+index)` | 1 open, 0 closed (index = position in the `doors` list) |

To make an item appear in the current room, `poke {var:itemsdbaddr}+{usedef:item_key}, {var:CurrentLocation}` is enough. If the change should be undoable, call `gosub {:save_undo}` first.

### Leaving the hook

- **Let the normal action continue**: do nothing; the hook ends on its `{:..._continue}` label.
- **Show your own message and block the action**: `{var:Message}=0: goto {:show_user_message}` (0 = first entry of `usermessages`).
- **Win / lose**: `goto {:end_win}` or `goto {:end_loose}`; the engine offers undo, new game, load or quit.

### Examples

Win by entering the treasure room (`pre_going.bas`):

```
	if {var:CurrentLocation}={usedef:room_treasure} then {:end_win}
```

The key appears the first time the well is examined (`pre_examine.bas`):

```
	if {var:ObjectId}<>{usedef:item_well@courtyard} then {:pre_examine_continue}
	if peek({var:itemsdbaddr}+{usedef:item_key})<>W then {:pre_examine_continue}
	gosub {:save_undo}: poke {var:itemsdbaddr}+{usedef:item_key}, {var:CurrentLocation}
	{var:Message}=1: goto {:show_user_message}
```

For this second example the key is declared without a `location` (so it starts at 255, out of play), and `usermessages[1]` holds the text, e.g. "At the bottom of the well a \\u001ckey\\u0090 glitters!".

## Building, making the disk and testing

One command turns the JSON into nine files; then you adapt the loader and hooks, preprocess the BASIC sources and put everything on a `.d64` image.

### 1. Build the data

```
IFBuilder64 castle.en.json
```

Without `-o`, the file prefix is the JSON name minus its extension (`castle.en.`). The builder prints one line per file created:

| File | Content | Name on disk |
| --- | --- | --- |
| `castle.en.in.prg` | Intro text | `castle.en.in` |
| `castle.en.cf.prg` | Configuration: entity counts and addresses (loaded at $02A7) | `castle.en.cf` |
| `castle.en.dt.prg` | All texts: messages, descriptions, names, verbs, directions | `castle.en.dt` |
| `castle.en.db.prg` | Initial state: exits, item and door locations and states (up to $C9FF) | `castle.en.db` |
| `castle.en.ab.seq` | Data for the `about` command (title, author, IFID, date) | `castle.en.ab` (SEQ) |
| `castle.en.ifid.seq` | Uppercase IFID | optional |
| `castle.en.iFiction.xml` | Babel/iFiction record for IF archives | not on the disk |
| `castle.en.mem.bas` | Lowers the top of BASIC RAM to make room for the data | included by the loader |
| `castle.en.def.bas` | `room_*` and `item_*` constants for the hooks | included by the hooks |

Texts are allocated downward from $BFFF: if the game is too large, the RAM left to BASIC shrinks until it's no longer enough.

### 2. Adapt the loader and hooks

`C64/loader.bas` still references the author's sample game. Change:

- `{uses:bin/latinorum.it.mem.bas}` → your `mem.bas` (in a bilingual game use the one for the language with more text);
- `"latinorum."` in `{:intro1}` → `"castle."`;
- the credit lines ("The Strawberry Field presents", author name) in `{:intro1}` and `{:intro2}`;
- the `bin/game.def.bas` reference in the hooks under `C64/custom/`.

The `sd2iec` symbol (defined with `{def:sd2iec}`) skips the "star turbo" fast loader, which isn't included in the repository.

### 3. Preprocess the BASIC

Convert `loader.bas` and `parser.bas` into `.prg` programs with the preprocessor, running it from the `C64/` folder so the relative `{uses:}` paths resolve. `lm/lm.prg` is prebuilt; you only need 64tass (`make` in `C64/lm`) if you change the assembly.

### 4. Create the disk

The loader loads, in order: `lm`, `<game>.in`, `<game>.cf`, `cover`, `<game>.dt`, then types `LOAD"PARSER",8` + `RUN` by itself; the parser loads `<game>.db`. With VICE's c1541:

```
c1541 -format "castle,01" d64 castle.d64 ^
  -write loader.prg castle ^
  -write lm\lm.prg lm ^
  -write parser.prg parser ^
  -write cover.prg cover ^
  -write castle.en.in.prg castle.en.in ^
  -write castle.en.cf.prg castle.en.cf ^
  -write castle.en.dt.prg castle.en.dt ^
  -write castle.en.db.prg castle.en.db ^
  -write castle.en.ab.seq "castle.en.ab,s"
```

Repeat the last five lines with `castle.it.*` for the Italian version. The `cover` file (title screen copied to $A000) isn't in the repository and must be prepared separately.

### 5. Test in VICE

`x64sc -autostart castle.d64` starts the loader. Pick the language with `E` or `I`, then test at least: `commands`, every exit of every room, every door closed and open, `take`/`drop` up to `maxload`, `undo`, `save` and `load`. The `record` command writes the typed commands to disk: handy for replaying a test session after every change.

## Complete example: "The Castle"

Four rooms, one door, one puzzle and one win condition: the player examines the well, finds the key, opens the gate, takes the scroll from the chest and climbs to the top of the tower.

Map:

```
              [top]
                | up/down
             [tower]      (chest holding the scroll)
                | gate (key)
           [courtyard]    (well: reveals the key)
                |
             [bridge]     <- start
```

`castle.en.json` (the `verbs`, `messages` and `directions` lists stay as in the template):

```json
{
  "intro": "\fTHE CASTLE\r\rAn ancient abandoned castle. They say you can see the sea from the top of the tower.\r",
  "initialroom": "bridge",
  "maxload": 5,
  "rooms": [
    { "name": "player", "description": "" },
    { "name": "bridge", "description": "You are on the drawbridge. The courtyard lies to the north.",
      "north": "courtyard" },
    { "name": "courtyard", "description": "A courtyard overgrown with weeds. In the middle stands an old \u001cwell\u0090.",
      "south": "bridge" },
    { "name": "tower", "description": "The foot of the tower. A staircase leads up. In a corner there is a \u001cchest\u0090.",
      "up": "top" },
    { "name": "top", "description": "The top of the tower. The sea shimmers on the horizon.",
      "down": "tower" }
  ],
  "doors": [
    { "name": "gate", "description": "An oak gate studded with iron.",
      "denyopen": "The gate is locked.", "unlockitem": "key", "open": false,
      "north": "courtyard", "south": "tower" }
  ],
  "items": [
    { "name": "me", "description": "As good-looking as ever.", "location": "player" },
    { "name": "key", "description": "A big rusty key." },
    { "name": "scroll", "description": "It reads: the best view is up high.",
      "initem": "chest@tower" },
    { "name": "well", "description": "A deep, dry well.",
      "location": "courtyard", "notportable": true },
    { "name": "chest", "description": "A wooden chest, closed.", "opendescription": "The chest is open.",
      "location": "tower", "notportable": true, "container": true,
      "openable": true, "closeable": true }
  ],
  "usermessages": [
    "Without the scroll you don't know what to look for up here. You go back down.",
    "At the bottom of the well a \u001ckey\u0090 glitters!"
  ],
  "ifiction": {
    "title": "The Castle", "author": "John Smith", "language": "en",
    "ifid": "3F2A9C1E-5B7D-4E8A-9C21-7D4E5F6A8B90", "firstpublished": 2026
  }
}
```

The key has no `location`: it starts out of play, and the `pre_examine.bas` hook shown in the game logic section makes it appear. The gate isn't listed among the exits of `courtyard` and `tower` because it writes them itself when it opens.

`pre_going.bas` — reaching the top wins only while carrying the scroll:

```
{uses:bin/castle.en.def.bas}
'
	if {var:CurrentLocation}<>{usedef:room_top} then {:pre_going_continue}
	if peek({var:itemsdbaddr}+{usedef:item_scroll})=Z then {:end_win}
	{usedef:print_string}, {var:usrmsgaddr}, 0: print
	{var:CurrentLocation}={usedef:room_tower}: poke {var:curroomdbaddr},{var:CurrentLocation}

{:pre_going_continue}
```

Walkthrough: `n`, `examine well`, `take key`, `open gate`, `n`, `open chest`, `take scroll`, `u`.

## Troubleshooting

Almost every error comes from a name spelled differently in two places: the builder doesn't report missing references, it turns them into 255.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `--template` fails with file not found | No `templates/` in the current directory | Run from the executable's folder or copy `templates/` |
| `Unknown template language` | Language not recognized | Use `en`, `english`, `inglese`, `it`, `italian` or `italiano` |
| `Unable to parse game file` or a JSON exception | Invalid JSON or a required field missing | `name` and `description` are required for rooms, items and doors; validate against the schema |
| An exit doesn't show up | Wrong destination room name | Check case and spaces: the match is exact |
| The door never opens | `unlockitem` doesn't match the item's `name`, or the item isn't carried | The player must *carry* the key; having it in the room isn't enough |
| A door that starts open doesn't let you through | With `"open": true` the exits aren't written | Also declare the exits in both rooms |
| An item in a container can't be seen | Container closed, or `initem` without `@room` for scenery | Use the full id, e.g. `chest@tower` |
| "I don't see any around here..." on scenery | Scenery looked for in another room | Scenery is only found in its own `location` |
| Wrong system messages | `messages` list reordered or shortened | Regenerate the template and copy only the texts |
| `?OUT OF MEMORY` on the C64 | Texts too long: the data has eaten into BASIC RAM | Shorten descriptions and messages, trim the hooks |
| `about` gives a disk error | The `<game>.ab` (SEQ) file is missing from the disk | Add it with `,s` in c1541; it needs `ifiction` in the JSON |

### Converting an existing game

If you have an adventure in the old Adventure Player SEQ format, `SeqConverter --template=en old.seq` creates `old.json` with rooms (numbered 1, 2, 3…), items and verbs already filled in, ready to be polished by hand.

### References

- [IFBuilder64 repository](https://github.com/strawberryfield/IFBuilder64) — sources, templates in `Common/templates/`, engine in `C64/`
- [Author's site, Roberto Ceccarelli](https://strawberryfield.altervista.org) — Casasoft
- [Babel / iFiction](https://babel.ifarchive.org) — metadata format and IFID
- [VICE](https://vice-emu.sourceforge.io) — emulator and `c1541`
- License: GNU AGPL 3.0 or later

An Italian edition of this guide is also available: [IFBuilder64 — Manuale per creare una nuova avventura](https://claude.ai/artifact/NtYEj2i27bc992geMA1E1B).

# DEACT — Deck Edit And Creation Tool

A Windows desktop app (WPF, .NET 5) for preparing **Magic: The Gathering** decks
for **Tabletop Simulator**. DEACT resolves each card on **Scryfall**, lets you pick
the language and the exact art you want, and produces a TTS *Saved Object* `.json`
where every card has its own individual image.

---

## What it does

DEACT offers three ways to build or fix a TTS deck:

### 1. Generate — build a TTS deck from a decklist *(recommended)*
Paste a Magic decklist and DEACT builds a brand-new TTS Saved Object from scratch:

- Resolves every card against the **current Scryfall API**, reading the real
  `image_uris` / `card_faces` URLs (never reconstructing them by hand).
- Fetches the art in your **preferred language**, falling back to English when a
  card isn't printed in that language.
- Lets you **pin one or more sets / series** so art is pulled from those printings
  first (in priority order).
- Emits **one individual image per card** (no composite sheets) — `NumWidth=1`,
  `NumHeight=1`, one `CustomDeck` entry per unique card.
- Handles **double-faced / modal cards** through TTS *States*, so they flip in-game.

This is the main flow since the in-game TTS Scryfall importer was disabled.

### 2. Import — edit an existing TTS deck *(legacy)*
Load a TTS Saved Object `.json` you already have and re-pick the language / art for
each card. Saving writes the changes back through typed, round-trip-safe models
(every field TTS wrote is preserved).

### 3. Merge — combine two decks
Load two TTS deck files and merge them, reusing the art already chosen for shared cards.

### 4. Set Card Back — quick utility
Replace the card-back image URL across a deck file in one step.

---

## Decklist format

One card per line. Quantity and the set/collector number are optional:

```
1 Atraxa, Praetors' Voice (2X2) 145
2x Sol Ring (C21) 263
10 Forest
1 Lightning Bolt
```

- `(SET) NUMBER` pins an exact printing; without it, DEACT uses your language and
  pinned series to choose.
- Foil markers (`*F*`) and bracket tags (`[Ramp]`) are stripped.
- Blank lines, comments (`//`, `#`) and section headers (`Commander`,
  `Sideboard (15)`, `Deck`, …) are ignored.

---

## Using the Generate flow

1. From the home screen, click **Generate**.
2. Paste your decklist.
3. Choose a **language** and, optionally, pin one or more **series**.
4. Click **Generate Deck**. DEACT resolves each card and shows them in a gallery.
5. Tweak any card's art individually (the per-card art picker), then **Save** the `.json`.
6. In TTS: *Objects → Saved Objects* → your deck.

The saved `.json` goes, by default, to
`Documents\My Games\Tabletop Simulator\Saves\Saved Objects`.

---

## Gallery indicators

While reviewing a deck, the header and each card tile surface a few cues:

- **Card counter** (header) — e.g. `100 cards (89 unique)`, counting duplicates, so
  you can confirm at a glance that you pasted a complete deck (e.g. a 100-card
  Commander deck).
- **⇄ R/V badge** (top-left of a tile) — the card is **double-faced / modal** and
  will flip to its back face in TTS.
- **Language badge** (top-right, e.g. `EN`) — the art was found in a language
  **other than your preferred one**. No badge means it matched your preferred language.
- **Status dot** (top-right) — orange: no art found in the selected language
  (previous image kept); red: the fetch failed or was rate-limited.

Changed your mind on language or series after generating? Use **Re-DL** in the
gallery to re-fetch every card, or **Apply** to pull art from a comma-separated
list of set codes.

---

## Notes

- Scryfall asks for ~1 request per 100 ms; DEACT paces and caches requests, so large
  decks resolve without hitting rate limits. Re-running the same deck is fast thanks
  to a **24-hour image cache** (under `%ProgramData%\DEACT`).
- Pinning sets/series pre-loads those printings in bulk, and both faces of a card
  come from a single lookup, so generation stays fast even for large decks.
- Card images are referenced directly from Scryfall URLs; nothing is re-hosted.
- The app targets **Windows** (WPF, .NET 5).

---

## Building

Requires the **.NET SDK** (the project builds with the .NET 6 SDK targeting
`net5.0-windows`).

```sh
dotnet build TTSDeckEditAndCreationTool/TTSDeckEditAndCreationTool.csproj -c Release
```

Or open `TTSDeckEditAndCreationTool.sln` in Visual Studio and run.

---

## Credits

Deck/JSON structure and Magic resolution logic are inspired by the open-source
[`tts-deckconverter`](https://github.com/jeandeaual/tts-deckconverter) (Go, MIT) —
used as a read-only reference, not a port. Card data and images come from
[Scryfall](https://scryfall.com).

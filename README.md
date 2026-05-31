# DEACT
Tool for editing and adding decks used in Tabletop Simulator

# Version 0.4.0
Big update: DEACT can now **generate** a TTS deck from scratch out of a plain decklist, instead of only editing an imported file.

Why? The in-game TTS Scryfall importer (the old source of these JSON files) was disabled, so there's no longer a file to import. Now you paste a decklist and DEACT builds the Saved Object itself, placing the art you choose. The old Import / Merge / Set Card Back flows are all still there and unchanged.

New: Generate from a decklist
1. Click **Generate** on the home page.
2. Paste your decklist (one card per line). Quantity and set/collector number are optional, e.g.:
   - `1 Atraxa, Praetors' Voice (2X2) 145`
   - `2x Sol Ring (C21) 263`
   - `10 Forest`
   - `1 Lightning Bolt`
   Foil markers (`*F*`), tags (`[Ramp]`), comments (`//`, `#`) and section headers (`Commander`, `Sideboard (15)`, ...) are ignored automatically.
3. Pick a **language** and, optionally, pin one or more **series** so art is pulled from those sets first.
4. Click **Generate Deck**. Each card is resolved on Scryfall (reading the real image URLs, never guessing them), shown in the gallery, then **Save** the JSON into your TTS Saved Objects folder.
5. The deck is built as one pile with an individual image per card. Double-faced / modal cards flip in-game (handled via TTS states).

New gallery indicators
- **Card counter** in the header, e.g. `100 cards (89 unique)` (counts duplicates) so you can confirm you pasted a full deck.
- **R/V badge** on a card means it's double-faced.
- **Language badge** (e.g. `EN`) means that card's art was found in a language other than the one you asked for. This also fixes the old issue where English fallbacks showed no warning at all.
- Status dot stays: orange = no art in the chosen language, red = fetch failed / rate-limited. Missing-art cards are logged to `C:/ProgramData/DEACT/DEACT_fetch_errors.log`.

Other improvements
- **Series picker** (multi-select) on the import and generate screens and in the gallery; pin several sets and they're tried in priority order.
- **Faster & gentler on Scryfall**: image-URL caching is back (24h), pinned sets are bulk-preloaded, and both faces of a card come from a single request. Large decks resolve far faster and re-runs are near-instant.
- A loading overlay shows live progress (X/N, elapsed time, and a countdown if Scryfall briefly rate-limits).
- Saved images are upgraded to Scryfall's full-resolution PNG.

# Version 0.3.5
When chosing a card style there is now a language dropdown in the bottom right. *note if the card has no results from the selected language you will get an error popup. Future todo would be to handle that more gracefully.

BIG NOTE: Card style caching was removed with this change in favor of time. I will add it back some day but its really not needed the time save is pretty minimal since its just caching the URL's.

# Added Open Source
Added the full project source. Feel free to grab it and look it over or modify as you see fit.
The project is .net with wpf, you will most likely want to just use visual studio and open the .sln

# Version 0.3.4
Fixed crashing when opening card style for a card that does not exist so you can add art for custom cards. Fixed issue where card style window was showing front of card art for the backside of cards. If this is still showing issues for you be sure to delete your cache (instructions below).

# Version 0.3.3
Added merge option. Merge takes an old deck as reference for card art then a new deck list and applies any of the old art to the new list.

Why Merge?
- Merge saves time when making new lists that are just alterations of previous lists.
- Previously when you made a new list you would have to re-enter all the art for that new list.
- Now you can use the merge option with your old list and new list and cut right to changing the new cards.

How to Merge?
1. Select the merge option from the home page.
2. Enter or browse for the path to your OLD list (this is the list that already has all your custom art selected).
3. Enter or browse for the path to your NEW list (this is the new list that you wish to add art to)
4. Click import, DEACT will automatically take any matching cards from the old list and use them for the new list as it loads in.
5. All done! You can continue editing but make sure you SAVE.

# Version 0.3.2
Added support for cards with content on their backs. Transform and modal cards will now have their back faces listed in the card list and will properly query scryfall for the back face in the style selection window.

Card art is not showing. Whats the cache?
- URLS are cached per card when you first query it. 
- These cached entries expire after 24 hrs.
- If for some reason you need to clear your cache it is located in C:/ProgramData/DEACT (just delete the text file in there)

# Version 0.3.0
Added style selection window. New card info on hover (Image icon to open style window, amount of card in deck, zone of card (companion/commander or Library)). More style improvements.

Selecting A Card Style
1. Once your deck is imported hover on the card you wish to change the style of.
2. When hovered 3 items will appear at the bottom of the card.
3. Select the image icon on the left.
4. A new window will open with all the styles queried from scryfall (or grabbed from cache if you have looked at this card before)
5. Click on the one you wish to chose, or you can add and chose a custom URL at the bottom of the window.
6. All done. Save your deck.


# Version 0.2.1
Style improvements. logic cleanup for card back changer. Added deck import and individual card editing.

How to import a deck
1. Click on Import on the main page
2. Use browse to select your deck or paste its file path in the path line
3. Click Import

How to use the Deck Builder window
- Top left will allow you to enter a custom card back url
- Each card will be displayed (enlarge window as needed)
- You can alter the Face URL for any card by changing the URL below the image
- Click save in the top right when finished

# Version 0.1.1
First published version only includes deck card back changing ability.

How To Use the Card Back changer
1. Click on the image above set card back to go to the card back page.
2. Enter file path or navigate to it by clicking on the browse button.
3. Enter new image back URL
4. Click GO, error or success message and details will be shown.


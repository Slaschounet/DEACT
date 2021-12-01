# DEACT
Tool for editing and adding decks used in Tabletop Simulator

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


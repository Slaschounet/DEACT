using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TTSDeckEditAndCreationTool.Model
{
    /// <summary>
    /// Builds a Tabletop Simulator "Saved Object" from a resolved decklist (the
    /// generation flow: decklist -> Scryfall -> art choice -> JSON). Each card gets
    /// an individual image (NumWidth=1, NumHeight=1, one CustomDeck entry), so there
    /// is no composite sheet and no per-card URL collision to worry about.
    ///
    /// Layout (per the user's choice, everything in one pile):
    ///   ObjectStates[0] = DeckCustom pile
    ///     CustomDeck : { "1": img1, "2": img2, ... }   one entry per UNIQUE card
    ///     DeckIDs    : [100, 100, 200, ...]            CardID repeated per copy (quantity)
    ///     ContainedObjects : one CardCustom per copy, sharing the unique image
    ///   A single card (1 card x1) is emitted directly as a CardCustom, with no pile.
    ///
    /// Double-faced/modal cards (DeckCard.BackFaceURL set) carry their verso in
    /// States["2"], mirroring tts-deckconverter (tts/builder.go).
    /// Mirrors that reference's structure and default transforms/scales.
    /// </summary>
    public static class TtsDeckBuilder
    {
        // Standard MTG card scaling: TTS cards are ~56x80mm at scale 1, real cards are
        // 63.5x88.9mm. Same constants as the reference (tts/builder.go).
        private const double StandardScaleX = 63.5 / 56.0;
        private const double StandardScaleZ = 88.9 / 80.0;

        public static GenSavedObject Build(IEnumerable<DeckCard> cards, string backUrl, string saveName = "Generated Deck")
        {
            backUrl = UpgradeUrlToPng(backUrl);

            // Expand to (uniqueCard, copies). The incoming DeckCards are already deduped
            // by name with a Count, which is exactly one CustomDeck entry per unique image.
            List<DeckCard> unique = cards?.Where(c => c != null && !string.IsNullOrWhiteSpace(c.FaceURL)).ToList()
                                    ?? new List<DeckCard>();

            int totalCopies = unique.Sum(c => c.Count <= 0 ? 1 : c.Count);

            // Single card, single copy -> emit a lone CardCustom (no pile), like the reference.
            if (unique.Count == 1 && totalCopies == 1)
            {
                DeckCard only = unique[0];
                GenObject single = BuildCardObject(only, 100, "1", backUrl, isInsideDeck: false);
                return new GenSavedObject { SaveName = saveName, ObjectStates = { single } };
            }

            var deck = new GenObject
            {
                Name = "DeckCustom",
                Nickname = saveName,
                Transform = new GenTransform
                {
                    RotY = 180,
                    RotZ = 180,
                    ScaleX = StandardScaleX,
                    ScaleY = 1,
                    ScaleZ = StandardScaleZ
                },
                ColorDiffuse = new GenColorDiffuse(),
                CustomDeck = new Dictionary<string, GenCustomDeck>(),
                DeckIDs = new List<int>(),
                ContainedObjects = new List<GenObject>()
            };

            int n = 1;
            foreach (DeckCard card in unique)
            {
                int copies = card.Count <= 0 ? 1 : card.Count;
                int cardId = 100 * n;
                string deckIdKey = n.ToString();

                deck.CustomDeck[deckIdKey] = new GenCustomDeck
                {
                    FaceURL = UpgradeUrlToPng(card.FaceURL),
                    BackURL = backUrl
                };

                for (int i = 0; i < copies; i++)
                {
                    deck.DeckIDs.Add(cardId);
                    deck.ContainedObjects.Add(BuildCardObject(card, cardId, deckIdKey, backUrl, isInsideDeck: true));
                }

                n++;
            }

            return new GenSavedObject { SaveName = saveName, ObjectStates = { deck } };
        }

        private static GenObject BuildCardObject(DeckCard card, int cardId, string deckIdKey, string backUrl, bool isInsideDeck)
        {
            var obj = new GenObject
            {
                Name = "CardCustom",
                Nickname = card.Cardname ?? card.Nickname ?? "",
                Transform = new GenTransform
                {
                    RotY = 180,
                    RotZ = 0,
                    ScaleX = StandardScaleX,
                    ScaleY = 1,
                    ScaleZ = StandardScaleZ
                },
                ColorDiffuse = new GenColorDiffuse(),
                CardID = cardId,
                CustomDeck = new Dictionary<string, GenCustomDeck>
                {
                    [deckIdKey] = new GenCustomDeck
                    {
                        FaceURL = UpgradeUrlToPng(card.FaceURL),
                        BackURL = backUrl
                    }
                }
            };

            // Double-faced/modal card: emit the verso as a self-contained state keyed "2".
            // The state has its own CardID/CustomDeck (sheet 1, index 0), isolated from the
            // parent deck — mirrors the reference's recursive alternate-state build.
            if (!string.IsNullOrWhiteSpace(card.BackFaceURL))
            {
                obj.States = new Dictionary<string, GenObject>
                {
                    ["2"] = new GenObject
                    {
                        Name = "CardCustom",
                        Nickname = card.Cardname ?? card.Nickname ?? "",
                        Transform = new GenTransform
                        {
                            RotY = 180,
                            RotZ = 0,
                            ScaleX = StandardScaleX,
                            ScaleY = 1,
                            ScaleZ = StandardScaleZ
                        },
                        ColorDiffuse = new GenColorDiffuse(),
                        CardID = 100,
                        CustomDeck = new Dictionary<string, GenCustomDeck>
                        {
                            ["1"] = new GenCustomDeck
                            {
                                FaceURL = UpgradeUrlToPng(card.BackFaceURL),
                                BackURL = backUrl
                            }
                        }
                    }
                };
            }

            return obj;
        }

        // Prefer Scryfall's full-resolution PNG over the smaller jpg variants.
        // Mirrors DeckBuilderViewModel.UpgradeUrlToPng (kept here so the Model layer
        // has no dependency on the ViewModel layer).
        private static string UpgradeUrlToPng(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            url = Regex.Replace(url, "/(small|normal|large)/", "/png/");
            url = Regex.Replace(url, "\\.jpg", ".png");
            return url;
        }
    }
}

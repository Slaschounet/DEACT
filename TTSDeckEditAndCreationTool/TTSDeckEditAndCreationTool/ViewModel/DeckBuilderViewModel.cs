﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Net.Http;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.Store;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.View;
using System.Text.RegularExpressions;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    public class DeckBuilderViewModel : ViewModelBase
    {
        private ObservableCollection<CardBuilderViewModel> _deckCards { get; set; }

        private static readonly HttpClient httpClient = new HttpClient();

        static DeckBuilderViewModel()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "DEACT/1.0 (contact@example.com)");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public ObservableCollection<CardBuilderViewModel> DeckCards
        {
            get
            {
                if (_deckCards == null)
                {
                    return _deckCards = new ObservableCollection<CardBuilderViewModel>();
                }
                else
                {
                    return _deckCards;
                }
            }
            set
            {
                _deckCards = value;
                OnPropertyChanged();
            }
        }

        private string _cardBackURL { get; set; }

        public string PreferredLanguage { get; set; } = "fr";

        private int _preferredLanguageCount;
        private int _defaultLanguageCount;
        private int _errorCount;

        public string ImportSummary
        {
            get
            {
                return $"{_preferredLanguageCount} {PreferredLanguage} cards, {_defaultLanguageCount} en, {_errorCount} errors";
            }
        }

        public string CardBackURL
        {
            get
            {
                return _cardBackURL;
            }
            set
            {
                _cardBackURL = DisplayCardBackURL = value;
                OnPropertyChanged(nameof(DisplayCardBackURL));
            }
        }

        public string DisplayCardBackURL { get; set; }
        private Dictionary<string, DeckCard> CardLookup = new Dictionary<string, DeckCard>();
        private Dictionary<string, string> CardArt = new Dictionary<string, string>(); //for merging decklists

        public ICommand SaveDeckCommand { get; }
        public ICommand ApplySetStylesCommand { get; }

        private string _setAbbreviations;
        public string SetAbbreviations
        {
            get { return _setAbbreviations; }
            set { _setAbbreviations = value; OnPropertyChanged(); }
        }

        private string _deckPath { get; set; }
        private string _deckJson { get; set; }
        private string _oldCardBackURL { get; set; }
        private bool _isUpdated;
        public bool IsUpdated
        {
            get => _isUpdated;
            set
            {
                _isUpdated = value;
                OnPropertyChanged();
            }
        }

        public DeckBuilderViewModel()
        {
            SaveDeckCommand = new BuilderSaveDeckCommand(this);
            ApplySetStylesCommand = new ApplySetStylesCommand(this);
        }

        public async Task MergeFromPaths(string pathNew, string pathOld)
        {
            //By running the old first we setup the card lookup which we will then use to change the art when a card with the same name is loaded in with different face url
            await LoadFromPath(pathOld);
            DeckCards = new ObservableCollection<CardBuilderViewModel>();
            CardLookup = new Dictionary<string, DeckCard>();
            await LoadFromPath(pathNew);
        }

        /// <summary>
        /// Loads in cards from given path, creating CardBuilderViewModels for each and adding them to the DeckCards list.
        /// 
        /// TODO: Ideally this whole operation should be refactored and we should either clean up the redundant and verbous operations or create models to better import and handle the data.
        /// </summary>
        /// <param name="path"></param>
        public async Task LoadFromPath(string path)
        {
            _deckPath = path;

            _preferredLanguageCount = 0;
            _defaultLanguageCount = 0;
            _errorCount = 0;
            OnPropertyChanged(nameof(ImportSummary));

            //PART 1 : LOAD IN JSON
            try
            {
                _deckJson = File.ReadAllText(path);
            }
            catch(Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error reading from given file path \n\n " + e.Message);
                return;
            }

            dynamic rawDeck = null;
            try
            {
                rawDeck = JsonSerializer.Deserialize<ExpandoObject>(_deckJson);
            }
            catch (Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error deserializing file \n\n " + e.Message);
                return;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(_deckJson);
                if (doc.RootElement.TryGetProperty("isUpdated", out JsonElement upd))
                {
                    IsUpdated = upd.GetBoolean();
                }
                else
                {
                    IsUpdated = false;
                }
                OnPropertyChanged(nameof(IsUpdated));
            }
            catch
            {
                IsUpdated = false;
                OnPropertyChanged(nameof(IsUpdated));
            }

            //PART 2 : PARSE OUT CARDS
            //TODO: Right now this is pretty brute force on navigating the JSON. Ideally I would like to have a replica model of TTS objects so that not only is this cleaner but we can then move into deck creation and management as well.

            var savedObjs = rawDeck.ObjectStates;

            try
            {
                for (int i = 0; i < savedObjs.GetArrayLength(); i++)
                {
                    JsonElement cObj = savedObjs[i];
                    JsonElement containedobjects;

                    bool isDeck = cObj.TryGetProperty("ContainedObjects", out containedobjects); //if isDeck then the object in the list is a pile of cards rather than a single

                    if (isDeck)
                    {
                        foreach (JsonElement jelement in containedobjects.EnumerateArray())
                        {
                            await ParseJElementCard(jelement);
                        }
                    }
                    else
                    {
                        await ParseJElementCard(cObj);
                    }
                }
            }
            catch (Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error parsing card info \n\n" + e.Message);
                return;
            }

            //PART 3 : LOAD ON SCREEN
            OnPropertyChanged(nameof(DeckCards));
            OnPropertyChanged(nameof(CardBackURL));
        }

        private async Task ParseJElementCard(JsonElement jle)
        {
            JsonElement cardstates;

            bool hasStates = jle.TryGetProperty("States", out cardstates); //check if the card has states. (relevant for cards with content on the back face)

            await ParseJElementCardFace(jle);

            if (hasStates)
            {
                //this means its the BACK of the card
                foreach (JsonProperty jproperty in cardstates.EnumerateObject())
                {
                    await ParseJElementCardFace(jproperty.Value, true);
                }
            }
        }

        private async Task ParseJElementCardFace(JsonElement jle, bool isBack = false)
        {
            JsonElement nickname, customdeck, faceurl = new JsonElement(), cardid;

            jle.TryGetProperty("Nickname", out nickname);
            jle.TryGetProperty("CardID", out cardid);
            jle.TryGetProperty("CustomDeck", out customdeck);

            foreach (JsonProperty jproperty in customdeck.EnumerateObject())
            {
                jproperty.Value.TryGetProperty("FaceURL", out faceurl);
                if (string.IsNullOrWhiteSpace(_oldCardBackURL))
                {
                    JsonElement cardback;
                    jproperty.Value.TryGetProperty("BackURL", out cardback);
                    string backValue = cardback.GetString();
                    if (!string.IsNullOrWhiteSpace(backValue))
                    {
                        CardBackURL = _oldCardBackURL = cardback.GetString();
                    }
                }
            }
            if (CardLookup.ContainsKey(nickname.GetString()))
            {
                CardLookup[nickname.GetString()].Count++;
            }
            else
            {
                string nick = nickname.GetString();
                string originalFace = faceurl.GetString();
                string face = originalFace;
                if (CardArt.ContainsKey(nick))
                {
                    face = CardArt[nick];
                }
                else
                {
                    if (!IsUpdated)
                    {
                        var result = await FetchPreferredImage(nick.Split("\n")[0], isBack);
                        string altFace = result?.Url;
                        string usedLang = result?.Language;
                        if (!string.IsNullOrWhiteSpace(altFace))
                        {
                            face = altFace;
                            if (usedLang == PreferredLanguage) _preferredLanguageCount++;
                            else if (usedLang == "en") _defaultLanguageCount++;

                            if (_deckJson != null)
                            {
                                if (!string.IsNullOrWhiteSpace(originalFace))
                                {
                                    _deckJson = _deckJson.Replace(originalFace, face);
                                }
                                else
                                {
                                    _deckJson = _deckJson.Replace("\"FaceURL\": \"\"", "\"FaceURL\": \"" + face + "\"");
                                }
                            }
                        }
                        else
                        {
                            _errorCount++;
                        }
                        OnPropertyChanged(nameof(ImportSummary));
                    }
                    CardArt.Add(nick, face);
                }
                CardBuilderViewModel temp = new CardBuilderViewModel(new DeckCard(nick, cardid.GetInt32(), face, isBack));
                if (!IsUpdated)
                {
                    temp.Card.OldFaceURL = face;
                }
                else
                {
                    temp.Card.OldFaceURL = originalFace;
                }
               
                temp.Card.Cardname = nick.Split('\n')[0];
                if (!CardLookup.ContainsKey(nick)) CardLookup.Add(nick, temp.Card);
                DeckCards.Add(temp);
            }
        }

        public void SaveDeckToPath()
        {
            System.Diagnostics.Debug.WriteLine("====== SAVE START ======");
            
            IsUpdated = true;
            OnPropertyChanged(nameof(IsUpdated));

            if (!string.IsNullOrWhiteSpace(_deckJson))
            {
                if (_deckJson.Contains("\"isUpdated\""))
                {
                    _deckJson = Regex.Replace(_deckJson, "\"isUpdated\"\\s*:\\s*(true|false)", "\"isUpdated\": true");
                }
                else
                {
                    int braceIndex = _deckJson.IndexOf('{');
                    if (braceIndex >= 0)
                    {
                        _deckJson = _deckJson.Insert(braceIndex + 1, "\"isUpdated\": true,");
                    }
                }
            }


            foreach (CardBuilderViewModel cardvm in DeckCards)
            {
                DeckCard card = cardvm.Card;

                System.Diagnostics.Debug.WriteLine($"Card: {card.Cardname}");
                System.Diagnostics.Debug.WriteLine($"Old URL: {card.OldFaceURL}");
                System.Diagnostics.Debug.WriteLine($"New URL: {card.FaceURL}");
                System.Diagnostics.Debug.WriteLine($"Deck JSON contains old? {_deckJson.Contains(card.OldFaceURL)}");

                if (card.FaceURL != card.OldFaceURL)
                {
                    if (!string.IsNullOrWhiteSpace(card.OldFaceURL))
                    {
                        _deckJson = _deckJson.Replace(card.OldFaceURL, card.FaceURL);
                    }
                    else
                    {
                        _deckJson = _deckJson.Replace("\"FaceURL\": \"\"", "\"FaceURL\": \"" + card.FaceURL + "\"");
                    }
                    System.Diagnostics.Debug.WriteLine("-> Replaced URL in JSON");
                }
            }

            if (_oldCardBackURL != CardBackURL && !string.IsNullOrWhiteSpace(CardBackURL))
            {
                System.Diagnostics.Debug.WriteLine($"Old Back URL: {_oldCardBackURL}");
                System.Diagnostics.Debug.WriteLine($"New Back URL: {CardBackURL}");
                _deckJson = _deckJson.Replace("BackURL\": \"" + _oldCardBackURL, "BackURL\": \"" + CardBackURL);
            }

            // Upgrade URLs to png
            _deckJson = UpgradeImageUrlsToPng(_deckJson);

            // Write the file
            File.WriteAllText(_deckPath, _deckJson);
            System.Diagnostics.Debug.WriteLine("====== SAVE COMPLETE ======");

            FeedbackPopupViewModel.Instance.DisplaySmileMessage("Deck Saved Successfully");
        }

        private static string UpgradeImageUrlsToPng(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            json = Regex.Replace(json, "/(small|normal|large)/", "/png/");

            return json;
        }

        private async Task<FetchedImageResult> FetchPreferredImage(string cardName, bool isBack)
        {
            List<string> languages = new List<string>();
            if (!string.IsNullOrWhiteSpace(PreferredLanguage))
            {
                languages.Add(PreferredLanguage);
            }
            if (PreferredLanguage != "en") languages.Add("en");

            foreach (string lang in languages)
            {
                try
                {
                    string urlName = cardName.Replace(' ', '_');
                    string baseUrl = "https://api.scryfall.com/cards/search?q=!" + urlName + " lang:" + lang + "&unique=prints";

                    HttpResponseMessage res = await httpClient.GetAsync(baseUrl);
                    if (res.IsSuccessStatusCode)
                    {
                        string data = await res.Content.ReadAsStringAsync();
                        JsonElement root = JsonSerializer.Deserialize<JsonElement>(data);
                        if (root.TryGetProperty("data", out JsonElement cardInfos))
                        {
                            foreach (JsonElement cardInfo in cardInfos.EnumerateArray())
                            {
                                JsonElement cardImages, cardImage, cardFaces;

                                if (cardInfo.TryGetProperty("image_uris", out cardImages))
                                {
                                    if (!cardImages.TryGetProperty("normal", out cardImage))
                                    {
                                        cardImages.TryGetProperty("small", out cardImage);
                                    }
                                }
                                else if (cardInfo.TryGetProperty("card_faces", out cardFaces))
                                {
                                    cardFaces[isBack ? 1 : 0].TryGetProperty("image_uris", out cardImages);
                                    cardImages.TryGetProperty("normal", out cardImage);
                                }
                                else
                                {
                                    continue;
                                }

                                return new FetchedImageResult
                                {
                                    Url = cardImage.GetString(),
                                    Language = lang
                                };
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        public async Task ApplySetStyles()
        {
            if (string.IsNullOrWhiteSpace(SetAbbreviations) || DeckCards == null) return;

            List<string> sets = SetAbbreviations.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            foreach (var cardvm in DeckCards)
            {
                foreach (string set in sets)
                {
                    var result = await FetchImageForSet(cardvm.Card.Cardname, set, cardvm.Card.BackFace);
                    if (result != null && !string.IsNullOrWhiteSpace(result.Url))
                    {
                        cardvm.UpdateCardFaceURL(result.Url);
                        break;
                    }
                }
            }
        }

        private async Task<FetchedImageResult> FetchImageForSet(string cardName, string setAbbrev, bool isBack)
        {
            try
            {
                string urlName = cardName.Replace(' ', '_');
                string baseUrl = $"https://api.scryfall.com/cards/search?q=!{urlName}+set:{setAbbrev}+lang:{PreferredLanguage}&unique=prints";
                HttpResponseMessage res = await httpClient.GetAsync(baseUrl);
                if (res.IsSuccessStatusCode)
                {
                    string data = await res.Content.ReadAsStringAsync();
                    JsonElement root = JsonSerializer.Deserialize<JsonElement>(data);
                    if (root.TryGetProperty("data", out JsonElement cardInfos))
                    {
                        foreach (JsonElement cardInfo in cardInfos.EnumerateArray())
                        {
                            JsonElement cardImages, cardImage, cardFaces;

                            if (cardInfo.TryGetProperty("image_uris", out cardImages))
                            {
                                if (!cardImages.TryGetProperty("normal", out cardImage))
                                {
                                    cardImages.TryGetProperty("small", out cardImage);
                                }
                            }
                            else if (cardInfo.TryGetProperty("card_faces", out cardFaces))
                            {
                                cardFaces[isBack ? 1 : 0].TryGetProperty("image_uris", out cardImages);
                                cardImages.TryGetProperty("normal", out cardImage);
                            }
                            else
                            {
                                continue;
                            }

                            return new FetchedImageResult
                            {
                                Url = cardImage.GetString(),
                                Language = PreferredLanguage
                            };
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}

public class FetchedImageResult
{
    public string Url { get; set; }
    public string Language { get; set; }
}

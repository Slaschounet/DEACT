using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.Store;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.View;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    public class DeckBuilderViewModel : ViewModelBase
    {
        private List<CardBuilderViewModel> _deckCards { get; set; }

        public List<CardBuilderViewModel> DeckCards
        {
            get
            {
                if(_deckCards == null)
                {
                    return _deckCards = new List<CardBuilderViewModel>();
                }
                else
                {
                    return _deckCards;
                }
            }
            set
            {
                _deckCards = value;
            }
        }

        private string _cardBackURL { get; set; }

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

        private string _deckPath { get; set; }
        private string _deckJson { get; set; }
        private string _oldCardBackURL { get; set; }

        public DeckBuilderViewModel()
        {
            SaveDeckCommand = new BuilderSaveDeckCommand(this);
        }

        public void MergeFromPaths(string pathNew, string pathOld)
        {
            //By running the old first we setup the card lookup which we will then use to change the art when a card with the same name is loaded in with different face url
            LoadFromPath(pathOld);
            DeckCards = new List<CardBuilderViewModel>();
            CardLookup = new Dictionary<string, DeckCard>();
            LoadFromPath(pathNew);
        }

        /// <summary>
        /// Loads in cards from given path, creating CardBuilderViewModels for each and adding them to the DeckCards list.
        /// 
        /// TODO: Ideally this whole operation should be refactored and we should either clean up the redundant and verbous operations or create models to better import and handle the data.
        /// </summary>
        /// <param name="path"></param>
        public void LoadFromPath(string path)
        {
            _deckPath = path;

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


            //PART 2 : PARSE OUT CARDS

            //LARGE PREFACE: THERE IS A LOT OF REDUNDANCY HERE. Ideally this all should be reconfigured in some way.
            // - The best option would be to create models for the data so that the json can just be converted and objects than can be handled correctly.

            var savedObjs = rawDeck.ObjectStates;

            try
            {
                for (int i = 0; i < savedObjs.GetArrayLength(); i++)
                {
                    JsonElement cObj = savedObjs[i];

                    JsonElement nickname, customdeck, statescustomdeck, cardstates, faceurl = new JsonElement(), cardid, containedobjects;

                    bool isDeck = cObj.TryGetProperty("ContainedObjects", out containedobjects);

                    if (isDeck)
                    {
                        foreach (JsonElement jelement in containedobjects.EnumerateArray())
                        {
                            //clean up just to avoid issues
                            nickname = new JsonElement();
                            faceurl = new JsonElement();
                            cardid = new JsonElement();
                            customdeck = new JsonElement();
                            cardstates = new JsonElement();

                            //grab initially available information
                            jelement.TryGetProperty("Nickname", out nickname);
                            jelement.TryGetProperty("CardID", out cardid);
                            jelement.TryGetProperty("CustomDeck", out customdeck);

                            bool hasStates = jelement.TryGetProperty("States", out cardstates); //check if the card has states. (relevant for cards with content on the back face)

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
                            if (CardLookup.ContainsKey(faceurl.GetString()))
                            {
                                CardLookup[faceurl.GetString()].Count++;
                            }
                            else
                            {
                                string nick = nickname.GetString();
                                string face = faceurl.GetString();
                                if (CardArt.ContainsKey(nick))
                                {
                                    face = CardArt[nick];
                                }
                                else
                                {
                                    CardArt.Add(nick, face);
                                }
                                CardBuilderViewModel temp = new CardBuilderViewModel(new DeckCard(nick, cardid.GetInt32(), face));
                                temp.Card.Cardname = nick.Split('\n')[0];
                                CardLookup.Add(face, temp.Card);
                                DeckCards.Add(temp);
                            }

                            if (hasStates)
                            {
                                //this means its the BACK of the card
                                foreach (JsonProperty jproperty in cardstates.EnumerateObject())
                                {
                                    jproperty.Value.TryGetProperty("Nickname", out nickname);
                                    jproperty.Value.TryGetProperty("CardID", out cardid);
                                    jproperty.Value.TryGetProperty("CustomDeck", out statescustomdeck);

                                    foreach (JsonProperty statejproperty in statescustomdeck.EnumerateObject())
                                    {
                                        statejproperty.Value.TryGetProperty("FaceURL", out faceurl);
                                    }

                                    if (CardLookup.ContainsKey(faceurl.GetString()))
                                    {
                                        CardLookup[faceurl.GetString()].Count++;
                                    }
                                    else
                                    {
                                        string nick = nickname.GetString();
                                        string face = faceurl.GetString();
                                        if (CardArt.ContainsKey(nick))
                                        {
                                            face = CardArt[nick];
                                        }
                                        else
                                        {
                                            CardArt.Add(nick, face);
                                        }
                                        CardBuilderViewModel temp = new CardBuilderViewModel(new DeckCard(nick, cardid.GetInt32(), face, 'L', 1, true));
                                        temp.Card.Cardname = nick.Split('\n')[0];
                                        CardLookup.Add(face, temp.Card);
                                        DeckCards.Add(temp);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        cObj.TryGetProperty("Nickname", out nickname);
                        cObj.TryGetProperty("CardID", out cardid);
                        cObj.TryGetProperty("CustomDeck", out customdeck);
                        bool hasStates = cObj.TryGetProperty("States", out cardstates);
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

                        if (CardLookup.ContainsKey(faceurl.GetString()))
                        {
                            CardLookup[faceurl.GetString()].Count++;
                        }
                        else
                        {
                            string nick = nickname.GetString();
                            string face = faceurl.GetString();
                            if (CardArt.ContainsKey(nick))
                            {
                                face = CardArt[nick];
                            }
                            else
                            {
                                CardArt.Add(nick, face);
                            }
                            CardBuilderViewModel temp = new CardBuilderViewModel(new DeckCard(nickname.GetString(), cardid.GetInt32(), face, 'C'));
                            temp.Card.Cardname = nick.Split('\n')[0];
                            CardLookup.Add(face, temp.Card);
                            DeckCards.Add(temp);
                        }

                        if (hasStates)
                        {
                            //This means its the BACK of the card
                            foreach (JsonProperty jproperty in cardstates.EnumerateObject())
                            {
                                jproperty.Value.TryGetProperty("Nickname", out nickname);
                                jproperty.Value.TryGetProperty("CardID", out cardid);
                                jproperty.Value.TryGetProperty("CustomDeck", out statescustomdeck);

                                foreach (JsonProperty statejproperty in statescustomdeck.EnumerateObject())
                                {
                                    statejproperty.Value.TryGetProperty("FaceURL", out faceurl);
                                }

                                if (CardLookup.ContainsKey(faceurl.GetString()))
                                {
                                    CardLookup[faceurl.GetString()].Count++;
                                }
                                else
                                {
                                    string nick = nickname.GetString();
                                    string face = faceurl.GetString();
                                    if (CardArt.ContainsKey(nick))
                                    {
                                        face = CardArt[nick];
                                    }
                                    else
                                    {
                                        CardArt.Add(nick, face);
                                    }
                                    CardBuilderViewModel temp = new CardBuilderViewModel(new DeckCard(nick, cardid.GetInt32(), face, 'C', 1, true));
                                    temp.Card.Cardname = nick.Split('\n')[0];
                                    CardLookup.Add(face, temp.Card);
                                    DeckCards.Add(temp);
                                }
                            }
                        }
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

        public void SaveDeckToPath()
        {
            foreach(CardBuilderViewModel cardvm in DeckCards)
            {
                DeckCard card = cardvm.Card;
                if(card.FaceURL != card.OldFaceURL)
                {
                    _deckJson = _deckJson.Replace(card.OldFaceURL, card.FaceURL);
                }
            }

            if(_oldCardBackURL != CardBackURL && !string.IsNullOrWhiteSpace(CardBackURL))
            {
                _deckJson = _deckJson.Replace("BackURL\": \"" + _oldCardBackURL, "BackURL\": \"" + CardBackURL);
            }

            //replace small image url's with normal size
            _deckJson = _deckJson.Replace("/small/", "/normal/");

            File.WriteAllText(_deckPath, _deckJson);
            FeedbackPopupViewModel.Instance.DisplaySmileMessage("Deck Saved Successfully");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.Store;
using TTSDeckEditAndCreationTool.View;
using TTSDeckEditAndCreationTool.Commands;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    public class StyleSelectionViewModel : ViewModelBase
    {
        public DeckCard Card;
        public CardBuilderViewModel CardBuilderVM;

        StyleSelectionView StyleWindow { get; set; }

        private CardStyleViewModel CustomCardTempalte { get; set; }

        private CardStyles _styles;

        public CardStyles Styles
        {
            get
            {
                if (_styles == null) _styles = new CardStyles(Card.Cardname);
                return _styles;
            }
            set
            {
                _styles = value;
                OnPropertyChanged(nameof(Styles));
                SetPrintsFromStyles();
            }
        }

        private List<CardStyleViewModel> _prints { get; set; }
        public List<CardStyleViewModel> Prints
        {
            get
            {
                if (_prints == null)
                {
                    _prints = new List<CardStyleViewModel>();
                }
                return _prints;
            }
            set
            {
                _prints = value;
            }
        }

        public string CustomURL { get; set; }
        public ICommand ChoseCustom { get; set; }

        public StyleSelectionViewModel(DeckCard card, CardBuilderViewModel cbvm)
        {
            Card = card;
            CardBuilderVM = cbvm;
            CustomCardTempalte = new CardStyleViewModel(new CardStyleInfo("Custom", ""), this);
            ChoseCustom = new StyleSelectedCommand(this);
        }

        public void StyleSelected(string styleURL)
        {
            if (styleURL == "Custom") styleURL = CustomURL;
            StyleWindow.Close();
            CardBuilderVM.UpdateCardFaceURL(styleURL);
        }

        private void SetPrintsFromStyles()
        {
            Prints = new List<CardStyleViewModel>();
            foreach(CardStyleInfo cardstyle in Styles.Prints)
            {
                Prints.Add(new CardStyleViewModel(cardstyle, this));
            }
            OnPropertyChanged(nameof(Prints));
        }

        public void ManualRefreshVisuals()
        {
            OnPropertyChanged(nameof(Prints));
            OnPropertyChanged(nameof(Styles));
        }


        public void OpenWindow()
        {
            //Load or fetch style data
            LoadOrFetch();

            //Open view window
            StyleWindow = new StyleSelectionView();
            StyleWindow.DataContext = this;
            StyleWindow.ShowDialog();
        }

        private async void LoadOrFetch()
        {
            //Check if card art exists in stor, otherwise fetch and add to stor

            if(CardStyleCache.CardList.ContainsKey(Card.Cardname) && CardStyleCache.CardList[Card.Cardname].LastFetched > DateTime.UtcNow.AddDays(-2))
            {
                Styles = CardStyleCache.CardList[Card.Cardname];
            }
            else
            {
                if (CardStyleCache.CardList.ContainsKey(Card.Cardname)) CardStyleCache.CardList.Remove(Card.Cardname);
                string URLname = Card.Cardname.Replace(' ', '_');
                string baseUrl = "https://api.scryfall.com/cards/search?q=!" + URLname + "&unique=prints";
                //if not in stor
                FetchAndAdd(baseUrl);
            }
        }

        private async void FetchAndAdd(string baseUrl)
        {
            JsonElement hasNext = new JsonElement();
            JsonElement nextUrl = new JsonElement();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();
                            if (content != null)
                            {
                                JsonElement testObject, cardInfos;
                                testObject = JsonSerializer.Deserialize<JsonElement>(data);
                                testObject.TryGetProperty("data", out cardInfos);
                                testObject.TryGetProperty("has_more", out hasNext);
                                testObject.TryGetProperty("next_page", out nextUrl);

                                foreach (JsonElement cardInfo in cardInfos.EnumerateArray())
                                {
                                    JsonElement setAbrrev, cardImages, cardImage, cardFaces;

                                    cardInfo.TryGetProperty("set", out setAbrrev);
                                    if(cardInfo.TryGetProperty("image_uris", out cardImages))
                                    {
                                        if (!cardImages.TryGetProperty("small", out cardImage))
                                        {
                                            cardImages.TryGetProperty("normal", out cardImage);
                                        }
                                    }
                                    else
                                    {
                                        cardInfo.TryGetProperty("card_faces", out cardFaces);//for flip cards
                                        cardFaces[Card.BackFace ? 1 : 0].TryGetProperty("image_uris", out cardImages);
                                        cardImages.TryGetProperty("normal", out cardImage);
                                    }

                                    CardStyleInfo newCardStyleInfo = new CardStyleInfo(setAbrrev.GetString(), cardImage.GetString());

                                    Styles.Prints.Add(newCardStyleInfo);
                                }
                            }
                            else
                            {
                                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error No Data Found");
                                Console.WriteLine("NO Data----------");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error Fetching and Parsing Card Data \n\n " + exception.Message);
                Console.WriteLine("Exception Hit------------");
                Console.WriteLine(exception);
            }

            if(hasNext.ValueKind != JsonValueKind.Null && hasNext.GetBoolean())
            {
                FetchAndAdd(nextUrl.GetString());
            }
            else
            {
                if (Styles.Prints.Count > 0)
                {
                    Styles.LastFetched = DateTime.UtcNow;
                    CardStyleCache.CardList.Add(Card.Cardname, Styles);
                    CardStyleCache.SaveList();
                }

                SetPrintsFromStyles();
            }
        }
    }
}

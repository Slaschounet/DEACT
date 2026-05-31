using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
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

        // Re-serialize the deck through the typed models when saving.
        // WriteIndented: readable, diffable output.
        // UnsafeRelaxedJsonEscaping: keep accents (é) and slashes literal instead of \uXXXX / \/.
        // WhenWritingNull: a nullable property absent from the source file is not re-emitted as null.
        private static readonly JsonSerializerOptions SaveOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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
        public ICommand RefetchLanguageCommand { get; }

        /// <summary>Series picker; when a set is pinned, fetches pull art from that set. Shared with the import screen.</summary>
        public SetSelectorViewModel SetSelector { get; } = new SetSelectorViewModel();

        /// <summary>Label for the re-download button, showing which language will be fetched.</summary>
        public string RefetchLanguageButtonText => $"Re-DL {PreferredLanguage}";

        private bool _isBusy;
        /// <summary>True while a bulk fetch (Re-DL / Apply) is running. Buttons bind to its inverse.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
        public bool IsNotBusy => !_isBusy;

        private string _fetchStatus;
        /// <summary>Live progress / result message for bulk fetches, shown next to the buttons.</summary>
        public string FetchStatus
        {
            get => _fetchStatus;
            set { _fetchStatus = value; OnPropertyChanged(); }
        }

        private string _busyText;
        /// <summary>Centered overlay text during a bulk fetch: progress, elapsed time, backoff countdown.</summary>
        public string BusyText
        {
            get => _busyText;
            set { _busyText = value; OnPropertyChanged(); }
        }

        // --- Busy/progress overlay state (import & Re-DL) ---
        private System.Windows.Threading.DispatcherTimer _busyTimer;
        private DateTime _busyStart;
        private DateTime? _backoffEndsAt;   // when the current Scryfall backoff wait ends
        private int _progressDone;
        private int _progressTotal;
        private string _busyVerb = "Downloading images";

        // Set by the active VM while busy so the static rate-limit gate can report a backoff.
        internal static Action<int> BackoffNotifier;

        private void BeginBusy(int total, string verb)
        {
            _busyVerb = verb;
            _progressTotal = total;
            _progressDone = 0;
            _busyStart = DateTime.Now;
            _backoffEndsAt = null;
            BackoffNotifier = ms => _backoffEndsAt = DateTime.Now.AddMilliseconds(ms);
            IsBusy = true;

            UpdateBusyText();
            _busyTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _busyTimer.Tick += (s, e) => UpdateBusyText();
            _busyTimer.Start();
        }

        private void StepProgress()
        {
            _progressDone++;
            UpdateBusyText();
        }

        private void EndBusy()
        {
            _busyTimer?.Stop();
            _busyTimer = null;
            BackoffNotifier = null;
            _backoffEndsAt = null;
            IsBusy = false;
        }

        private void UpdateBusyText()
        {
            int elapsed = (int)(DateTime.Now - _busyStart).TotalSeconds;
            string line1 = $"{_busyVerb}…  {_progressDone}/{_progressTotal}";
            string line3 = $"Elapsed: {elapsed}s";

            if (_backoffEndsAt.HasValue && _backoffEndsAt.Value > DateTime.Now)
            {
                int remain = (int)Math.Ceiling((_backoffEndsAt.Value - DateTime.Now).TotalSeconds);
                BusyText = $"{line1}\nScryfall rate limit reached — resuming in {remain}s…\n{line3}";
            }
            else
            {
                BusyText = $"{line1}\n{line3}";
            }
        }

        private string _setAbbreviations;
        public string SetAbbreviations
        {
            get { return _setAbbreviations; }
            set { _setAbbreviations = value; OnPropertyChanged(); }
        }

        private string _deckPath { get; set; }
        private TtsSaveFile _save; //typed, round-trip-safe model of the loaded deck; the single source of truth for saving
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
            RefetchLanguageCommand = new RefetchLanguageCommand(this);
            _ = SetSelector.LoadAsync(); //populate the series picker (cached, fire-and-forget)
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
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch(Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error reading from given file path \n\n " + e.Message);
                return;
            }

            try
            {
                _save = JsonSerializer.Deserialize<TtsSaveFile>(json);
            }
            catch (Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error deserializing file \n\n " + e.Message);
                return;
            }

            if (_save?.ObjectStates == null)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error parsing card info \n\n No ObjectStates found in file");
                return;
            }

            IsUpdated = _save.IsUpdated ?? false;
            OnPropertyChanged(nameof(IsUpdated));

            //PART 2 : PARSE OUT CARDS
            // Only decks not yet processed actually hit Scryfall; show the overlay just for those.
            int totalCards = _save.ObjectStates.Sum(o => o.IsDeck ? (o.ContainedObjects?.Count ?? 0) : 1);
            bool showOverlay = !IsUpdated;
            if (showOverlay) BeginBusy(totalCards, "Downloading images");

            try
            {
                // Bulk-load any selected series first so the per-card loop hits the cache.
                if (showOverlay) await PreloadSelectedSetsAsync();

                foreach (TtsObject obj in _save.ObjectStates)
                {
                    //a deck is a pile of cards rather than a single card
                    if (obj.IsDeck)
                    {
                        foreach (TtsObject contained in obj.ContainedObjects)
                        {
                            await ParseCard(contained);
                            if (showOverlay) StepProgress();
                        }
                    }
                    else
                    {
                        await ParseCard(obj);
                        if (showOverlay) StepProgress();
                    }
                }
            }
            catch (Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error parsing card info \n\n" + e.Message);
                return;
            }
            finally
            {
                if (showOverlay)
                {
                    CardStyleCache.SaveFaceCache(); //persist newly fetched urls for next time
                    EndBusy();
                }
            }

            //PART 3 : LOAD ON SCREEN
            OnPropertyChanged(nameof(DeckCards));
            OnPropertyChanged(nameof(CardBackURL));
        }

        private async Task ParseCard(TtsObject card)
        {
            await ParseCardFace(card);

            //cards with content on the back face expose it through States
            if (card.States != null)
            {
                foreach (TtsObject state in card.States.Values)
                {
                    await ParseCardFace(state, true);
                }
            }
        }

        private async Task ParseCardFace(TtsObject card, bool isBack = false)
        {
            string nick = card.Nickname ?? "";
            int cardId = card.CardID ?? 0;

            string originalFace = "";
            if (card.CustomDeck != null)
            {
                foreach (TtsCustomDeckEntry entry in card.CustomDeck.Values)
                {
                    originalFace = entry.FaceURL ?? "";
                    if (string.IsNullOrWhiteSpace(_oldCardBackURL) && !string.IsNullOrWhiteSpace(entry.BackURL))
                    {
                        CardBackURL = _oldCardBackURL = entry.BackURL;
                    }
                }
            }

            if (CardLookup.ContainsKey(nick))
            {
                CardLookup[nick].Count++;
                return;
            }

            string face = originalFace;
            FetchOutcome outcome = FetchOutcome.Success; //drives the per-tile status dot
            if (CardArt.ContainsKey(nick))
            {
                face = CardArt[nick];
            }
            else
            {
                if (!IsUpdated)
                {
                    var result = await FetchPreferredImage(nick.Split('\n')[0], isBack);
                    string altFace = result?.Url;
                    string usedLang = result?.Language;
                    if (!string.IsNullOrWhiteSpace(altFace))
                    {
                        face = altFace;
                        if (usedLang == PreferredLanguage) _preferredLanguageCount++;
                        else if (usedLang == "en") _defaultLanguageCount++;
                        //the fetched url is applied to the model at save time, keyed by the
                        //card's original FaceURL (see SaveDeckToPath), so nothing is mutated here
                    }
                    else
                    {
                        _errorCount++;
                        outcome = result?.Outcome ?? FetchOutcome.Error;
                    }
                    OnPropertyChanged(nameof(ImportSummary));
                }
                CardArt.Add(nick, face);
            }

            CardBuilderViewModel temp = new CardBuilderViewModel(new DeckCard(nick, cardId, face, isBack));
            //OldFaceURL is always the url present in the file; the save maps it onto every
            //matching CustomDeck entry (pile + per-card + states) and onto the new FaceURL
            temp.Card.OldFaceURL = originalFace;
            temp.Card.Cardname = nick.Split('\n')[0];
            temp.FetchStatus = outcome; //orange (NotFound) / red (Error/RateLimited) dot on the tile
            if (!CardLookup.ContainsKey(nick)) CardLookup.Add(nick, temp.Card);
            DeckCards.Add(temp);
        }

        public void SaveDeckToPath()
        {
            if (_save == null)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("No deck loaded to save");
                return;
            }

            IsUpdated = true;
            _save.IsUpdated = true;

            // Map each edited card's original FaceURL to its new one. Cards sharing the
            // same original url (e.g. duplicate basic lands) are updated together, which
            // matches the deduped one-tile-per-name model. Empty originals can't be
            // targeted by url and are left as-is (known limitation, see ParseCardFace).
            Dictionary<string, string> faceMap = new Dictionary<string, string>();
            foreach (CardBuilderViewModel cardvm in DeckCards)
            {
                DeckCard card = cardvm.Card;
                if (card.FaceURL != card.OldFaceURL && !string.IsNullOrWhiteSpace(card.OldFaceURL))
                {
                    faceMap[card.OldFaceURL] = card.FaceURL;
                }
            }

            bool backChanged = _oldCardBackURL != CardBackURL && !string.IsNullOrWhiteSpace(CardBackURL);

            // Apply every change in a single pass over ALL CustomDeck entries
            // (pile-level, per-card, and card States) so both copies of a shared url
            // stay in sync. This is what the old whole-file string.Replace did, but
            // scoped to the right fields instead of blind text substitution.
            foreach (TtsCustomDeckEntry entry in EnumerateCustomDeckEntries())
            {
                if (!string.IsNullOrEmpty(entry.FaceURL) && faceMap.TryGetValue(entry.FaceURL, out string newFace))
                {
                    entry.FaceURL = newFace;
                }
                entry.FaceURL = UpgradeUrlToPng(entry.FaceURL);

                if (backChanged && entry.BackURL == _oldCardBackURL)
                {
                    entry.BackURL = CardBackURL;
                }
                entry.BackURL = UpgradeUrlToPng(entry.BackURL);
            }

            try
            {
                string json = JsonSerializer.Serialize(_save, SaveOptions);
                File.WriteAllText(_deckPath, json);
            }
            catch (Exception e)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage("Error saving file \n\n " + e.Message);
                return;
            }

            FeedbackPopupViewModel.Instance.DisplaySmileMessage("Deck Saved Successfully");
        }

        /// <summary>Walks every CustomDeck entry in the deck: pile-level, per-card, and inside card States.</summary>
        private IEnumerable<TtsCustomDeckEntry> EnumerateCustomDeckEntries()
        {
            if (_save?.ObjectStates == null) yield break;

            foreach (TtsObject obj in _save.ObjectStates)
            {
                foreach (TtsCustomDeckEntry e in CustomDeckOf(obj)) yield return e;

                if (obj.ContainedObjects != null)
                {
                    foreach (TtsObject card in obj.ContainedObjects)
                    {
                        foreach (TtsCustomDeckEntry e in CustomDeckOf(card)) yield return e;

                        if (card.States != null)
                            foreach (TtsObject state in card.States.Values)
                                foreach (TtsCustomDeckEntry e in CustomDeckOf(state)) yield return e;
                    }
                }
            }
        }

        private static IEnumerable<TtsCustomDeckEntry> CustomDeckOf(TtsObject obj)
        {
            return obj?.CustomDeck != null ? obj.CustomDeck.Values : Enumerable.Empty<TtsCustomDeckEntry>();
        }

        private static string UpgradeUrlToPng(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            url = Regex.Replace(url, "/(small|normal|large)/", "/png/");
            url = Regex.Replace(url, "\\.jpg", ".png");
            return url;
        }

        // Scryfall asks for 50-100ms between requests, but it also uses a token bucket:
        // an initial burst is tolerated, then a slower sustained rate is enforced with
        // HTTP 429. We serialize all calls behind this gate (so bursts can't happen),
        // space them out, and on 429 back off exponentially — honoring the Retry-After
        // header when present — for several attempts before giving up.
        private static readonly System.Threading.SemaphoreSlim _scryfallGate = new System.Threading.SemaphoreSlim(1, 1);
        // /cards/search is a "heavy" endpoint limited to ~2 req/s. Pacing just under that
        // (~1.8 req/s) means we never trip a 429 — far faster overall than bursting and
        // eating 30s penalties. The retry/backoff below stays as a safety net.
        private const int ScryfallDelayMs = 550;       // base spacing between successful calls (~1.8 req/s)
        private const int ScryfallMaxRetries = 4;      // extra attempts on 429

        private static async Task<HttpResponseMessage> GetWithRateLimitAsync(string url)
        {
            await _scryfallGate.WaitAsync();
            try
            {
                int backoffMs = 500;
                HttpResponseMessage res = await httpClient.GetAsync(url);

                for (int attempt = 0; attempt < ScryfallMaxRetries && (int)res.StatusCode == 429; attempt++)
                {
                    // Respect Scryfall's Retry-After (seconds) if it tells us how long to wait.
                    int waitMs = backoffMs;
                    if (res.Headers.RetryAfter?.Delta is TimeSpan ra && ra.TotalMilliseconds > 0)
                    {
                        waitMs = (int)ra.TotalMilliseconds;
                    }
                    res.Dispose();
                    BackoffNotifier?.Invoke(waitMs); // let the overlay show a countdown
                    await Task.Delay(waitMs);
                    backoffMs *= 2; // 500, 1000, 2000, 4000...
                    res = await httpClient.GetAsync(url);
                }
                return res;
            }
            finally
            {
                // Hold the spacing inside the lock so the *next* caller waits too.
                await Task.Delay(ScryfallDelayMs);
                _scryfallGate.Release();
            }
        }

        /// <summary>
        /// Bulk-loads every selected series into the face cache up front. A set search
        /// (set:CODE) returns ~175 cards per page, so a whole series costs a handful of
        /// requests instead of one-per-card. After this runs, FetchPreferredImage resolves
        /// most cards straight from the cache (no network), turning a ~430s import into ~15s.
        /// Cards in none of the selected sets simply fall through to the normal per-card path.
        /// </summary>
        private async Task PreloadSelectedSetsAsync()
        {
            List<string> setCodes = SetSelector?.SelectedSetCodes;
            if (setCodes == null || setCodes.Count == 0) return;

            List<string> languages = new List<string> { PreferredLanguage };
            if (PreferredLanguage != "en") languages.Add("en");

            foreach (string setCode in setCodes)
            {
                foreach (string lang in languages)
                {
                    string url = $"https://api.scryfall.com/cards/search?q=set:{setCode}+lang:{lang}&unique=prints";
                    while (!string.IsNullOrEmpty(url))
                    {
                        if (IsBusy) FetchStatus = $"Preloading set {setCode.ToUpper()} [{lang}]…";
                        url = await PreloadPageAsync(url, lang, setCode);
                    }
                }
            }
        }

        /// <summary>Fetches one search page, caches each card's face url(s), returns the next page url or null.</summary>
        private async Task<string> PreloadPageAsync(string url, string lang, string setCode)
        {
            try
            {
                using HttpResponseMessage res = await GetWithRateLimitAsync(url);
                if (!res.IsSuccessStatusCode) return null;

                string data = await res.Content.ReadAsStringAsync();
                JsonElement root = JsonSerializer.Deserialize<JsonElement>(data);
                if (!root.TryGetProperty("data", out JsonElement cards) || cards.ValueKind != JsonValueKind.Array)
                    return null;

                foreach (JsonElement card in cards.EnumerateArray())
                {
                    if (!card.TryGetProperty("name", out JsonElement nameEl)) continue;
                    // Deck nicknames use only the front-face name, so key the cache on that.
                    string keyName = (nameEl.GetString() ?? "").Split(new[] { " // " }, StringSplitOptions.None)[0];
                    if (string.IsNullOrWhiteSpace(keyName)) continue;

                    if (card.TryGetProperty("image_uris", out JsonElement imgs))
                    {
                        string u = PickNormal(imgs);
                        if (u != null) CardStyleCache.StoreCachedFace(keyName, lang, false, u, setCode);
                    }
                    else if (card.TryGetProperty("card_faces", out JsonElement faces) && faces.ValueKind == JsonValueKind.Array)
                    {
                        var faceList = faces.EnumerateArray().ToList();
                        if (faceList.Count > 0 && faceList[0].TryGetProperty("image_uris", out JsonElement f0))
                        {
                            string u = PickNormal(f0);
                            if (u != null) CardStyleCache.StoreCachedFace(keyName, lang, false, u, setCode);
                        }
                        if (faceList.Count > 1 && faceList[1].TryGetProperty("image_uris", out JsonElement f1))
                        {
                            string u = PickNormal(f1);
                            if (u != null) CardStyleCache.StoreCachedFace(keyName, lang, true, u, setCode);
                        }
                    }
                }

                if (root.TryGetProperty("has_more", out JsonElement more) && more.ValueKind == JsonValueKind.True
                    && root.TryGetProperty("next_page", out JsonElement next))
                {
                    return next.GetString();
                }
            }
            catch
            {
                // best effort: a failed preload just means those cards hit the per-card path
            }
            return null;
        }

        private static string PickNormal(JsonElement imageUris)
        {
            if (imageUris.TryGetProperty("normal", out JsonElement n)) return n.GetString();
            if (imageUris.TryGetProperty("small", out JsonElement s)) return s.GetString();
            return null;
        }

        private async Task<FetchedImageResult> FetchPreferredImage(string cardName, bool isBack)
        {
            List<string> languages = new List<string>();
            if (!string.IsNullOrWhiteSpace(PreferredLanguage))
            {
                languages.Add(PreferredLanguage);
            }
            if (PreferredLanguage != "en") languages.Add("en");

            // Series to try, in priority order. Empty selection -> a single null = "any set".
            List<string> setCodes = SetSelector?.SelectedSetCodes;
            if (setCodes == null || setCodes.Count == 0) setCodes = new List<string> { null };

            // Track why we failed so the caller can tell "rate-limited" from "no art in
            // this language/set". RateLimited wins over Error wins over NotFound for reporting.
            bool sawRateLimit = false;
            bool sawError = false;

            // Outer loop = chosen series (priority order); inner = languages.
            // First printing found in any chosen set, preferred language first, wins.
            foreach (string setCode in setCodes)
            {
                foreach (string lang in languages)
                {
                    // Cache hit: skip the network and the rate-limit gate entirely.
                    string cached = CardStyleCache.GetCachedFace(cardName, lang, isBack, setCode);
                    if (!string.IsNullOrWhiteSpace(cached))
                    {
                        return new FetchedImageResult { Url = cached, Language = lang, Outcome = FetchOutcome.Success };
                    }

                    try
                    {
                        string urlName = cardName.Replace(' ', '_');
                        string setFilter = string.IsNullOrWhiteSpace(setCode) ? "" : " set:" + setCode;
                        string baseUrl = "https://api.scryfall.com/cards/search?q=!" + urlName + " lang:" + lang + setFilter + "&unique=prints";

                        using HttpResponseMessage res = await GetWithRateLimitAsync(baseUrl);
                        if ((int)res.StatusCode == 429)
                        {
                            sawRateLimit = true;
                            continue;
                        }
                        if (res.IsSuccessStatusCode)
                        {
                            string data = await res.Content.ReadAsStringAsync();
                            JsonElement root = JsonSerializer.Deserialize<JsonElement>(data);
                            if (root.TryGetProperty("data", out JsonElement cardInfos) && cardInfos.ValueKind == JsonValueKind.Array)
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

                                    string foundUrl = cardImage.GetString();
                                    CardStyleCache.StoreCachedFace(cardName, lang, isBack, foundUrl, setCode);
                                    return new FetchedImageResult
                                    {
                                        Url = foundUrl,
                                        Language = lang,
                                        Outcome = FetchOutcome.Success
                                    };
                                }
                            }
                            // 200 OK but no usable print in this language/set: try the next
                            // language, then the next chosen set.
                        }
                        else
                        {
                            sawError = true;
                        }
                    }
                    catch
                    {
                        sawError = true;
                    }
                }
            }

            FetchOutcome outcome = sawRateLimit ? FetchOutcome.RateLimited
                                 : sawError ? FetchOutcome.Error
                                 : FetchOutcome.NotFound;
            return new FetchedImageResult { Outcome = outcome };
        }

        /// <summary>
        /// Re-fetches every card's art in the current <see cref="PreferredLanguage"/> and updates
        /// the gallery in place. This is an explicit user action (the "Re-download" button), so it
        /// runs regardless of <see cref="IsUpdated"/> — unlike the import, which skips fetching for
        /// decks already marked processed. The new urls are committed to the file on the next Save.
        /// </summary>
        public async Task RefreshImagesInPreferredLanguage()
        {
            if (DeckCards == null || IsBusy) return;

            _preferredLanguageCount = 0;
            _defaultLanguageCount = 0;
            _errorCount = 0;
            OnPropertyChanged(nameof(ImportSummary));

            int notFound = 0;
            int rateLimited = 0;
            var issues = new List<string>();

            BeginBusy(DeckCards.Count, $"Re-downloading {PreferredLanguage}");
            try
            {
                // Bulk-load any selected series first so the per-card loop hits the cache.
                await PreloadSelectedSetsAsync();

                foreach (CardBuilderViewModel cardvm in DeckCards)
                {
                    StepProgress();

                    var result = await FetchPreferredImage(cardvm.Card.Cardname, cardvm.Card.BackFace);
                    if (result != null && !string.IsNullOrWhiteSpace(result.Url))
                    {
                        cardvm.UpdateCardFaceURL(result.Url);
                        cardvm.FetchStatus = FetchOutcome.Success;
                        if (result.Language == PreferredLanguage) _preferredLanguageCount++;
                        else if (result.Language == "en") _defaultLanguageCount++;
                    }
                    else
                    {
                        _errorCount++;
                        FetchOutcome outcome = result?.Outcome ?? FetchOutcome.Error;
                        cardvm.FetchStatus = outcome;
                        if (outcome == FetchOutcome.NotFound) notFound++;
                        else if (outcome == FetchOutcome.RateLimited) rateLimited++;
                        issues.Add($"{cardvm.Card.Cardname}\t{outcome}");
                    }
                    OnPropertyChanged(nameof(ImportSummary));
                }

                string summary = $"Done: {_preferredLanguageCount} {PreferredLanguage}, {_defaultLanguageCount} en, " +
                                 $"{notFound} not found, {rateLimited} rate-limited";
                if (issues.Count > 0)
                {
                    string logPath = LogFetchIssues(issues, rateLimited, notFound);
                    summary += $" — see log: {logPath}";
                }
                FetchStatus = summary;
            }
            finally
            {
                CardStyleCache.SaveFaceCache(); //persist newly fetched urls for next time
                EndBusy();
            }
        }

        /// <summary>
        /// Appends the failed cards (with their cause) to a log file under %ProgramData%\DEACT
        /// so the user can tell rate-limiting (transient, retry later) from genuinely missing
        /// art in the chosen language. Returns the log path for display.
        /// </summary>
        private string LogFetchIssues(List<string> issues, int rateLimited, int notFound)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DEACT");
                Directory.CreateDirectory(folder);
                string logPath = Path.Combine(folder, "DEACT_fetch_errors.log");

                var sb = new StringBuilder();
                sb.AppendLine($"==== Re-download [{PreferredLanguage}] @ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====");
                sb.AppendLine($"{rateLimited} rate-limited (transient — retry later), {notFound} not found in '{PreferredLanguage}'.");
                foreach (string line in issues) sb.AppendLine(line);
                sb.AppendLine();

                File.AppendAllText(logPath, sb.ToString());
                return logPath;
            }
            catch
            {
                return "(could not write log)";
            }
        }

        public async Task ApplySetStyles()
        {
            if (string.IsNullOrWhiteSpace(SetAbbreviations) || DeckCards == null || IsBusy) return;

            BeginBusy(DeckCards.Count, "Applying set styles");
            try
            {
                List<string> sets = SetAbbreviations.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                foreach (var cardvm in DeckCards)
                {
                    StepProgress();
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
                FetchStatus = "Set styles applied.";
            }
            finally
            {
                EndBusy();
            }
        }

        private async Task<FetchedImageResult> FetchImageForSet(string cardName, string setAbbrev, bool isBack)
        {
            try
            {
                string urlName = cardName.Replace(' ', '_');
                string baseUrl = $"https://api.scryfall.com/cards/search?q=!{urlName}+set:{setAbbrev}+lang:{PreferredLanguage}&unique=prints";
                HttpResponseMessage res = await GetWithRateLimitAsync(baseUrl);
                if (res.IsSuccessStatusCode)
                {
                    string data = await res.Content.ReadAsStringAsync();
                    JsonElement root = JsonSerializer.Deserialize<JsonElement>(data);
                    if (root.TryGetProperty("data", out JsonElement cardInfos) && cardInfos.ValueKind == JsonValueKind.Array)
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

public enum FetchOutcome
{
    Success,        // image found in a requested language
    NotFound,       // server answered OK but no print exists in the requested language(s)
    RateLimited,    // server returned HTTP 429 even after retries
    Error           // network/parse failure
}

public class FetchedImageResult
{
    public string Url { get; set; }
    public string Language { get; set; }
    public FetchOutcome Outcome { get; set; } = FetchOutcome.Success;
}

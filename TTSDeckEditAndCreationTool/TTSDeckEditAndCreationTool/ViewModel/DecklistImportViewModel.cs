using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.Store;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    /// <summary>
    /// Generation entry screen: the user pastes a Magic decklist (optionally with a
    /// preferred language and pinned series), and the app resolves each card against
    /// Scryfall and builds a TTS Saved Object from scratch. Mirrors the structure of
    /// <see cref="ImportDeckViewModel"/> but takes pasted text instead of a TTS file.
    /// </summary>
    public class DecklistImportViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;

        public DeckBuilderViewModel DeckBuilderInstance { get; }

        private string _decklistText = "";
        public string DecklistText
        {
            get => _decklistText;
            set { _decklistText = value; OnPropertyChanged(); }
        }

        /// <summary>Series picker, shared with the gallery (pinned sets steer art resolution).</summary>
        public SetSelectorViewModel SetSelector => DeckBuilderInstance.SetSelector;

        // Same language set as the import screen.
        private readonly Dictionary<string, string> _languageMap = new Dictionary<string, string>
        {
            { "English", "en" },
            { "French", "fr" },
            { "German", "de" },
            { "Italian", "it" },
            { "Spanish", "es" },
            { "Portuguese", "pt" },
            { "Japanese", "ja" },
            { "Korean", "ko" },
            { "Russian", "ru" },
            { "Simplified Chinese", "zhs" },
            { "Traditional Chinese", "zht" },
        };

        public IEnumerable<string> Languages => _languageMap.Keys;

        private string _selectedLanguage = "French";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); }
        }

        public ICommand GenerateCommand { get; }

        public DecklistImportViewModel(NavigationStore navigationStore)
        {
            _navigationStore = navigationStore;
            DeckBuilderInstance = new DeckBuilderViewModel();
            GenerateCommand = new GenerateDeckFromListCommand(this);
        }

        public async Task GenerateDeckAndNavigate()
        {
            List<ParsedDeckLine> lines = DecklistParser.Parse(DecklistText);
            if (lines.Count == 0)
            {
                FeedbackPopupViewModel.Instance.DisplayErrorMessage(
                    "No cards found. Paste a decklist like:\n\n1 Sol Ring (C21) 263\n10 Forest");
                return;
            }

            DeckBuilderInstance.PreferredLanguage = _languageMap[SelectedLanguage];

            _navigationStore.CurrentViewModel = DeckBuilderInstance;
            await DeckBuilderInstance.LoadFromDecklist(lines);
        }
    }
}

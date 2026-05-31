using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.Model;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    public class CardBuilderViewModel : ViewModelBase
    {
        public DeckCard Card { get; set; }
        StyleSelectionViewModel styleViewVM { get; set; }

        public ICommand OrderOpenStyleWindow { get; set; }

        private FetchOutcome _fetchStatus = FetchOutcome.Success;
        /// <summary>Result of the last bulk fetch for this card; drives the warning/error icon.</summary>
        public FetchOutcome FetchStatus
        {
            get => _fetchStatus;
            set
            {
                _fetchStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowNotFoundIcon));
                OnPropertyChanged(nameof(ShowErrorIcon));
                OnPropertyChanged(nameof(StatusTooltip));
            }
        }

        /// <summary>Orange icon: no print exists for this card in the requested language.</summary>
        public bool ShowNotFoundIcon => _fetchStatus == FetchOutcome.NotFound;

        /// <summary>Red icon: the fetch failed (network/parse error or persistent rate-limit).</summary>
        public bool ShowErrorIcon => _fetchStatus == FetchOutcome.Error || _fetchStatus == FetchOutcome.RateLimited;

        public string StatusTooltip
        {
            get
            {
                switch (_fetchStatus)
                {
                    case FetchOutcome.NotFound: return "No art found in the selected language (kept previous image).";
                    case FetchOutcome.RateLimited: return "Rate-limited by Scryfall — try again later.";
                    case FetchOutcome.Error: return "Fetch failed (network or parse error).";
                    default: return null;
                }
            }
        }

        /// <summary>True when this card carries a distinct back face (double-faced / modal),
        /// i.e. the generator will emit it as a flippable card via States. Drives the DFC badge.</summary>
        public bool IsDoubleFaced => Card != null && !string.IsNullOrWhiteSpace(Card.BackFaceURL);

        private string _languageWarning;
        /// <summary>
        /// Upper-case language code (e.g. "EN") shown as a badge when the resolved art is NOT
        /// in the preferred language — a heads-up that this card fell back. Null when it matched
        /// the preferred language (or language is unknown).
        /// </summary>
        public string LanguageWarning
        {
            get => _languageWarning;
            set
            {
                _languageWarning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLanguageWarning));
                OnPropertyChanged(nameof(LanguageTooltip));
            }
        }

        public bool ShowLanguageWarning => !string.IsNullOrEmpty(_languageWarning);

        public string LanguageTooltip => ShowLanguageWarning
            ? $"Art is in {_languageWarning}, not your preferred language."
            : null;

        public CardBuilderViewModel(DeckCard card)
        {
            Card = card;

            styleViewVM = new StyleSelectionViewModel(Card, this);

            OrderOpenStyleWindow = new OpenCardStyleWindowCommand(this);
        }

        /// <summary>Notifies the UI that the card's copy count changed (deduped duplicates).</summary>
        public void NotifyCountChanged() => OnPropertyChanged(nameof(Card));

        public void UpdateCardFaceURL(string newFaceURL)
        {
            Card.FaceURL = newFaceURL;
            OnPropertyChanged(nameof(Card));
        }

        public void OpenStyleWindow()
        {
            styleViewVM.OpenWindow();
        }
    }
}

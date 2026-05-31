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

        public CardBuilderViewModel(DeckCard card)
        {
            Card = card;

            styleViewVM = new StyleSelectionViewModel(Card, this);

            OrderOpenStyleWindow = new OpenCardStyleWindowCommand(this);
        }

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

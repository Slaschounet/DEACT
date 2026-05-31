using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    /// <summary>
    /// Forces a re-download of every card's art in the deck's PreferredLanguage,
    /// even for decks already marked processed (isUpdated). Bound to the gallery's
    /// "Re-DL" button. See DeckBuilderViewModel.RefreshImagesInPreferredLanguage.
    /// </summary>
    class RefetchLanguageCommand : CommandBase
    {
        private readonly DeckBuilderViewModel _deckBuilderViewModel;

        public RefetchLanguageCommand(DeckBuilderViewModel deckBuilderViewModel)
        {
            _deckBuilderViewModel = deckBuilderViewModel;
        }

        public override async void Execute(object parameter)
        {
            if (_deckBuilderViewModel != null)
            {
                await _deckBuilderViewModel.RefreshImagesInPreferredLanguage();
            }
        }
    }
}

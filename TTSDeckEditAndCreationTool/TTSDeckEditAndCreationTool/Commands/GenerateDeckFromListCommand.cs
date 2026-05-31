using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    /// <summary>Parses the pasted decklist and navigates to the gallery to resolve/generate it.</summary>
    public class GenerateDeckFromListCommand : CommandBase
    {
        private readonly DecklistImportViewModel _viewModel;

        public GenerateDeckFromListCommand(DecklistImportViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public override async void Execute(object parameter)
        {
            await _viewModel.GenerateDeckAndNavigate();
        }
    }
}

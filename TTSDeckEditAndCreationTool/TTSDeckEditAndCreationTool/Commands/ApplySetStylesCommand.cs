using System;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class ApplySetStylesCommand : CommandBase
    {
        private readonly DeckBuilderViewModel _deckBuilderViewModel;

        public ApplySetStylesCommand(DeckBuilderViewModel deckBuilderViewModel)
        {
            _deckBuilderViewModel = deckBuilderViewModel;
        }

        public override async void Execute(object parameter)
        {
            if (_deckBuilderViewModel != null)
            {
                await _deckBuilderViewModel.ApplySetStyles();
            }
        }
    }
}

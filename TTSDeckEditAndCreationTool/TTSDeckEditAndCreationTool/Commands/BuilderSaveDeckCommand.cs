using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class BuilderSaveDeckCommand : CommandBase
    {
        private DeckBuilderViewModel _deckBuilderViewModel;
        public BuilderSaveDeckCommand(DeckBuilderViewModel deckBuilderViewModel)
        {
            _deckBuilderViewModel = deckBuilderViewModel;
        }
        public override void Execute(object parameter)
        {
            _deckBuilderViewModel.SaveDeckToPath();
        }
    }
}

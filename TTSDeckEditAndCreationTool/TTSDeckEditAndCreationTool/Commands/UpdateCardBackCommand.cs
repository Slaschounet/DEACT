using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class UpdateCardBackCommand : CommandBase
    {
        private readonly QuickBGViewModel _quickBGViewModel;

        public UpdateCardBackCommand(QuickBGViewModel quickBGViewModel)
        {
            _quickBGViewModel = quickBGViewModel;
        }
        public override void Execute(object parameter)
        {
            _quickBGViewModel?.ChangeDeckCardBacks();
        }
    }
}

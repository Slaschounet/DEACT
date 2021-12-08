using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class StyleSelectionRefreshCommand : CommandBase
    {
        StyleSelectionViewModel StyleSelectionVM;

        public StyleSelectionRefreshCommand(StyleSelectionViewModel ssvm)
        {
            StyleSelectionVM = ssvm;
        }
        public override void Execute(object parameter)
        {
            StyleSelectionVM.ManualRefreshVisuals();
        }
    }
}

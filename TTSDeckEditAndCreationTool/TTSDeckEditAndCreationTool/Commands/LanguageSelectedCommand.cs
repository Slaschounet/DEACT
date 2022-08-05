using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    public class SelectLanguageCommand : CommandBase
    {
        StyleSelectionViewModel StyleSelectionVM { get; set; }
        public SelectLanguageCommand(StyleSelectionViewModel ssvm)
        {
            StyleSelectionVM = ssvm;
        }

        public override void Execute(object parameter)
        {
            StyleSelectionVM.RefreshSelections();
        }
    }
}

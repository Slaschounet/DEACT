using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class StyleSelectedCommand : CommandBase
    {
        StyleSelectionViewModel StyleSelectionVM { get; set; }
        string URL { get; set; }

        public StyleSelectedCommand(StyleSelectionViewModel ssvm)
        {
            StyleSelectionVM = ssvm;
        }

        public StyleSelectedCommand(StyleSelectionViewModel ssvm, string url)
        {
            StyleSelectionVM = ssvm;
            URL = url;
        }

        public override void Execute(object parameter)
        {
            StyleSelectionVM.StyleSelected(parameter.ToString());
        }
    }
}

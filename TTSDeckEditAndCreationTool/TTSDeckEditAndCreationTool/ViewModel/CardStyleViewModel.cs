using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    public class CardStyleViewModel : ViewModelBase
    {
        public CardStyleInfo CardInfo { get; set; }
        StyleSelectionViewModel StyleSelectionVM { get; set; }

        public string SetAbbreviation
        {
            get
            {
                return CardInfo.SetAbbreviation;
            }
        }
        public string FaceURL
        {
            get
            {
                return CardInfo.URL;
            }
        }

        public ICommand SelectStyle { get; set; }

        public CardStyleViewModel(CardStyleInfo cardStyleinfo, StyleSelectionViewModel ssvm)
        {
            CardInfo = cardStyleinfo;
            StyleSelectionVM = ssvm;
            SelectStyle = new StyleSelectedCommand(StyleSelectionVM, CardInfo.URL);
        }
    }
}

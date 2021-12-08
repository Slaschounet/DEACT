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

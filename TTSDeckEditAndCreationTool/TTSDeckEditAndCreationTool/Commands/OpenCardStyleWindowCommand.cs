using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class OpenCardStyleWindowCommand : CommandBase
    {
        CardBuilderViewModel Card { get; set; }
        public OpenCardStyleWindowCommand(CardBuilderViewModel card)
        {
            Card = card;
        }
        public override void Execute(object parameter)
        {
            Card.OpenStyleWindow();
        }
    }
}

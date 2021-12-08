using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class ImportAndConvertCommand : CommandBase
    {
        ImportDeckViewModel _importDeckViewModel;
        MergeDeckViewModel _mergeDeckViewModel;

        public ImportAndConvertCommand(ImportDeckViewModel importDeckViewModel)
        {
            _importDeckViewModel = importDeckViewModel;
        }

        public ImportAndConvertCommand(MergeDeckViewModel mergeDeckViewModel)
        {
            _mergeDeckViewModel = mergeDeckViewModel;
        }

        public override void Execute(object parameter)
        {
            _importDeckViewModel?.ConvertPathToDeckAndNavigate();
            _mergeDeckViewModel?.ConvertPathToDeckAndNavigate();
        }
    }
}

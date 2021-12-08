using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool.Commands
{
    class BrowseFilePathCommand : CommandBase
    {
        private readonly QuickBGViewModel _quickBGViewModel;
        private readonly ImportDeckViewModel _importDeckViewModel;
        private readonly MergeDeckViewModel _mergeDeckViewModel;
        private readonly bool MergeOption = false; //Used to distinguish between the two different files used for Merge

        public BrowseFilePathCommand(QuickBGViewModel quickBGViewModel)
        {
            _quickBGViewModel = quickBGViewModel;
        }
        public BrowseFilePathCommand(ImportDeckViewModel importDeckViewModel)
        {
            _importDeckViewModel = importDeckViewModel;
        }

        public BrowseFilePathCommand(MergeDeckViewModel mergeDeckViewModel, bool mergeOption)
        {
            _mergeDeckViewModel = mergeDeckViewModel;
            MergeOption = mergeOption;
        }

        public override void Execute(object parameter)
        {
            _quickBGViewModel?.OpenFileBrowser();
            _importDeckViewModel?.OpenFileBrowser();
            _mergeDeckViewModel?.OpenFileBrowser(MergeOption);
        }
    }
}

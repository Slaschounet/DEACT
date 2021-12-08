using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.Store;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    class HomeViewModel : ViewModelBase
    {
        public ICommand SwitchToQBGCommand { get; }
        public ICommand SwitchToDeckImportCommand { get; }
        public ICommand SwitchToDeckMergeCommand { get; }
        public HomeViewModel(NavigationStore navigationStore)
        {
            SwitchToQBGCommand = new NavigateCommand<QuickBGViewModel>(navigationStore, () => new QuickBGViewModel(navigationStore));
            SwitchToDeckImportCommand = new NavigateCommand<ImportDeckViewModel>(navigationStore, () => new ImportDeckViewModel(navigationStore));
            SwitchToDeckMergeCommand = new NavigateCommand<MergeDeckViewModel>(navigationStore, () => new MergeDeckViewModel(navigationStore));
        }
    }
}

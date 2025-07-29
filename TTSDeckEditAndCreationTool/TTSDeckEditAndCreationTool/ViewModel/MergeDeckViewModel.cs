using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Store;
using TTSDeckEditAndCreationTool.ViewModel;
using TTSDeckEditAndCreationTool.Commands;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Dynamic;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    /// <summary>
    /// View Model for the Merge deck option.
    /// Form will have 2 Deck addresses, one old one new.
    /// Once selected both addresses are passed to the Deck Builder VM to handle processing.
    /// 
    /// This is effectively an altered clone of the Import Deck VM. See there for more info.
    /// </summary>
    class MergeDeckViewModel : ViewModelBase
    {
        private string _newDeckFilePath;
        public string NewDeckFilePath
        {
            get { return string.IsNullOrWhiteSpace(_newDeckFilePath) ? "New Deck File Path..." : _newDeckFilePath; }
            set { _newDeckFilePath = value; }
        }

        private string _oldDeckFilePath;
        public string OldDeckFilePath
        {
            get { return string.IsNullOrWhiteSpace(_oldDeckFilePath) ? "Old Deck File Path..." : _oldDeckFilePath; }
            set { _oldDeckFilePath = value; }
        }

        private DeckInfoStore deckInfo { get; set; }

        private DeckBuilderViewModel _deckBuilderViewModel { get; set; }

        private Dictionary<string, string> LanguageConverter = new Dictionary<string, string>
        {
            {"French", "fr"},
            {"English", "en"},
            {"Japanese", "ja"},
            {"Spanish", "es"},
            {"Portuguese", "pt"},
            {"Korean", "ko"},
            {"German", "de"},
            {"Italian", "it"},
            {"Russian", "ru"},
            {"Simplified Chinese", "zhs"},
            {"Traditional Chinese", "zht"}
        };

        public IEnumerable<string> Languages
        {
            get { return LanguageConverter.Keys; }
        }

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get { return string.IsNullOrEmpty(_selectedLanguage) ? "French" : _selectedLanguage; }
            set { _selectedLanguage = value; }
        }

        public ICommand NewBrowseFileCommand { get; }
        public ICommand OldBrowseFileCommand { get; }
        public ICommand ImportSelectedPathCommand { get; }
        public ICommand NavigateToDeckViewCommand { get; set; }
        public MergeDeckViewModel(NavigationStore navigationStore)
        {
            if (deckInfo == null) deckInfo = new DeckInfoStore();
            _deckBuilderViewModel = new DeckBuilderViewModel();

            NewBrowseFileCommand = new BrowseFilePathCommand(this, false);
            OldBrowseFileCommand = new BrowseFilePathCommand(this, true);
            ImportSelectedPathCommand = new ImportAndConvertCommand(this);
            NavigateToDeckViewCommand = new NavigateCommand<DeckBuilderViewModel>(navigationStore, () => _deckBuilderViewModel);
        }

        public void OpenFileBrowser(bool old = false)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            //get user specific path to save time
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            userName = userName.Split("\\")[1];
            openFileDlg.InitialDirectory = @"C:\Users\" + userName.ToString() + @"\Documents\My Games\Tabletop Simulator\Saves\Saved Objects";

            openFileDlg.Multiselect = false;

            Nullable<bool> result = openFileDlg.ShowDialog();

            //selection made
            if (result == true)
            {
                if(old)
                {
                    OldDeckFilePath = openFileDlg.FileName.ToString();
                    OnPropertyChanged(nameof(OldDeckFilePath));
                }
                else
                {
                    NewDeckFilePath = openFileDlg.FileName.ToString();
                    OnPropertyChanged(nameof(NewDeckFilePath));
                }
            }
        }

        public void ConvertPathToDeckAndNavigate()
        {
            deckInfo.DeckPath = NewDeckFilePath;

            if (LanguageConverter.ContainsKey(SelectedLanguage))
            {
                _deckBuilderViewModel.PreferredLanguage = LanguageConverter[SelectedLanguage];
            }
            else
            {
                _deckBuilderViewModel.PreferredLanguage = "fr";
            }

            NavigateToDeckViewCommand.Execute(null);

            _deckBuilderViewModel?.MergeFromPaths(NewDeckFilePath, OldDeckFilePath);
        }
    }
}

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
    /// Import Deck View Model.
    /// This VM is for the Import View which allows the user to select a Deck's file address to import into the Deck Builder.
    /// 
    /// View: ImportDeckView.xaml
    /// </summary>
    class ImportDeckViewModel : ViewModelBase
    {
        private string _deckFilePath;
        public string DeckFilePath
        {
            get { return string.IsNullOrWhiteSpace(_deckFilePath) ? "File Path..." : _deckFilePath; }
            set { _deckFilePath = value; }
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
            get
            {
                return string.IsNullOrEmpty(_selectedLanguage) ? "French" : _selectedLanguage;
            }
            set
            {
                _selectedLanguage = value;
            }
        }

        public ICommand BrowseFileCommand { get; } //Command to Open File Browser and Populate DeckFilePath textbox on selection
        public ICommand ImportSelectedPathCommand { get; } //Command to execute the import operation. Hands off the new DeckInfoStore to the DeckBuilderVM to begin the import process.
        public ICommand NavigateToDeckViewCommand { get; set; } //Standard Navigation Command to move to the Deck Builder
        public ImportDeckViewModel(NavigationStore navigationStore)
        {
            if (deckInfo == null) deckInfo = new DeckInfoStore();
            _deckBuilderViewModel = new DeckBuilderViewModel();

            BrowseFileCommand = new BrowseFilePathCommand(this);
            ImportSelectedPathCommand = new ImportAndConvertCommand(this);
            NavigateToDeckViewCommand = new NavigateCommand<DeckBuilderViewModel>(navigationStore, () => _deckBuilderViewModel); //Setup navigation command for Deck Builder
        }

        /// <summary>
        /// Open File Browser and set DeckFilePath to path of selection.
        /// This is called from the BrowseFileCommand. See: Commands/BrowseFilePathCommand.cs
        /// </summary>
        public void OpenFileBrowser()
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
                DeckFilePath = openFileDlg.FileName.ToString();
                OnPropertyChanged(nameof(DeckFilePath));
            }
        }

        /// <summary>
        /// Set DeckInfoStores info, Navigate to DeckBuilder, and Call Deck Builders LoadFromPath to import the deckpath.
        /// This is called from the ImportSelectedPathCommand. See: Commands/ImportAndConvertCommand.cs
        /// </summary>
        public async Task ConvertPathToDeckAndNavigate()
        {
            deckInfo.DeckPath = DeckFilePath;
            if(LanguageConverter.ContainsKey(SelectedLanguage))
            {
                _deckBuilderViewModel.PreferredLanguage = LanguageConverter[SelectedLanguage];
            }
            else
            {
                _deckBuilderViewModel.PreferredLanguage = "fr";
            }

            NavigateToDeckViewCommand.Execute(null);

            if (_deckBuilderViewModel != null)
            {
                await _deckBuilderViewModel.LoadFromPath(DeckFilePath);
            }
        }
    }
}

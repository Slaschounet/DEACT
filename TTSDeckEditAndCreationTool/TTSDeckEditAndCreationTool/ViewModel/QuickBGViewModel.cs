using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.Store;
using System.Text.Json;
using System.IO;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    class QuickBGViewModel : ViewModelBase
    {
        private string _deckFilePath;
        public string DeckFilePath 
        {
            get { return string.IsNullOrWhiteSpace(_deckFilePath) ? "File Path..." : _deckFilePath; }
            set { _deckFilePath = value; }
        }

        private string _backImageURL;
        public string BackImageURL
        {
            get { return string.IsNullOrWhiteSpace(_backImageURL) ? "" : _backImageURL; }
            set { _backImageURL = value; }
        }

        private string _outputTextBlock;
        public string OutputTextBlock
        {
            get { return string.IsNullOrWhiteSpace(_outputTextBlock) ? "" : _outputTextBlock; }
            set { _outputTextBlock = value; }
        }

        public ICommand LoadFilePath { get; }
        public ICommand UpdateImageCommand { get; }

        public QuickBGViewModel(NavigationStore navigationStore)
        {
            LoadFilePath = new BrowseFilePathCommand(this);
            UpdateImageCommand = new UpdateCardBackCommand(this);
        }

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

        public void ChangeDeckCardBacks()
        {
            string jsonString;

            if (string.IsNullOrWhiteSpace(BackImageURL)) return;

            //read in json file
            try
            {
                jsonString = File.ReadAllText(DeckFilePath);
            }
            catch(Exception e)
            {
                OutputTextBlock += "Issue reading in file" + "\n" + e.ToString();
                OnPropertyChanged(nameof(OutputTextBlock));
                return;
            }
            //parse and change card url
            try
            {
                int startIndex = -1;
                startIndex = jsonString.IndexOf("\"BackURL\": \"");

                if(startIndex == -1)
                {
                    OutputTextBlock += "Issue parsing deck json";
                    OnPropertyChanged(nameof(OutputTextBlock));
                    return;
                }

                int endIndex = startIndex += 12;//move pointer to start of url data

                bool urlFound = false;
                while(!urlFound)
                {
                    if(jsonString[endIndex] == '\"')
                    {
                        urlFound = true;
                    }
                    else
                    {
                        endIndex++;
                    }
                }

                string replacementBase = jsonString.Substring(startIndex, endIndex - startIndex - 1);

                OutputTextBlock += "replacement target set to: " + replacementBase + "\n";
                OutputTextBlock += "replacement string set to: " + BackImageURL + "\n";

                jsonString = jsonString.Replace("BackURL\": \"" + replacementBase, "BackURL\": \"" + BackImageURL);
            }
            catch(Exception e)
            {
                OutputTextBlock += "Issue parsing json" + "\n" + e.ToString();
                OnPropertyChanged(nameof(OutputTextBlock));
                return;
            }

            //save
            try
            {
                File.WriteAllText(DeckFilePath, jsonString);
            }
            catch(Exception e)
            {
                OutputTextBlock += "Issue saving json" + "\n" + e.ToString();
                OnPropertyChanged(nameof(OutputTextBlock));
                return;
            }

            OutputTextBlock += "Card back URL suceesfully changed";
            OnPropertyChanged(nameof(OutputTextBlock));
        }
    }
}

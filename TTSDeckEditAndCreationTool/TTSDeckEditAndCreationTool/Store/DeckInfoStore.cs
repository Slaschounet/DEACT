using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTSDeckEditAndCreationTool.Store
{
    class DeckInfoStore
    {
        private string _deckPath;
        public string DeckPath
        {
            get => _deckPath;
            set
            {
                _deckPath = value;
                OnCurrentViewModelChanged();
            }
        }

        public event Action CurrentViewModelChanged;

        private void OnCurrentViewModelChanged()
        {
            CurrentViewModelChanged?.Invoke();
        }
    }
}

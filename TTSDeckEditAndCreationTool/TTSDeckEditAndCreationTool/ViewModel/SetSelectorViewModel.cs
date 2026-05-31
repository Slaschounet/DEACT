using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TTSDeckEditAndCreationTool.Commands;
using TTSDeckEditAndCreationTool.Model;
using TTSDeckEditAndCreationTool.Store;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    /// <summary>
    /// Backs the series picker. Typing in the editable combo filters the full set list
    /// (contains-match on name or code); picking an entry ADDS it to <see cref="SelectedSets"/>
    /// rather than replacing, so several series can be chosen (e.g. all the Final Fantasy sets).
    /// Selected series show as removable chips.
    ///
    /// At fetch time the chosen sets are tried in order: for each card the first set that has
    /// a printing (in the requested language) wins. Cards in none of the sets keep their art
    /// and get an orange dot. Empty selection = no constraint (any set).
    ///
    /// Shared between the import screen and the gallery (same instance on the deck VM), so a
    /// selection made before import is still active for a later Re-DL.
    /// </summary>
    public class SetSelectorViewModel : ViewModelBase
    {
        private const int MaxResults = 80;

        private List<MagicSet> _all = new List<MagicSet>();

        /// <summary>Series chosen by the user, in priority order.</summary>
        public ObservableCollection<MagicSet> SelectedSets { get; } = new ObservableCollection<MagicSet>();

        public ICommand RemoveSetCommand { get; }

        public SetSelectorViewModel()
        {
            RemoveSetCommand = new RelayCommand(p =>
            {
                if (p is MagicSet s && SelectedSets.Contains(s))
                {
                    SelectedSets.Remove(s);
                    OnPropertyChanged(nameof(HasSelection));
                }
            });
        }

        private string _searchText = "";
        /// <summary>Free text bound to the editable combo; drives <see cref="FilteredSets"/>.</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredSets));
            }
        }

        private MagicSet _comboSelection;
        /// <summary>
        /// Transient binding for the combo's SelectedItem. Picking an item adds it to
        /// <see cref="SelectedSets"/> then resets, so the combo always reads empty and is
        /// ready for the next search.
        /// </summary>
        public MagicSet ComboSelection
        {
            get => _comboSelection;
            set
            {
                _comboSelection = value;
                if (value != null)
                {
                    if (!SelectedSets.Any(s => s.Code == value.Code))
                    {
                        SelectedSets.Add(value);
                        OnPropertyChanged(nameof(HasSelection));
                    }
                    // reset combo + search for the next pick
                    _comboSelection = null;
                    OnPropertyChanged(nameof(ComboSelection));
                    SearchText = "";
                }
            }
        }

        public bool HasSelection => SelectedSets.Count > 0;

        /// <summary>Set codes to fetch from (lower-case), in priority order. Empty = no constraint.</summary>
        public List<string> SelectedSetCodes =>
            SelectedSets.Where(s => !string.IsNullOrWhiteSpace(s.Code))
                        .Select(s => s.Code.ToLower())
                        .ToList();

        public IEnumerable<MagicSet> FilteredSets
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_searchText))
                    return _all.Take(MaxResults);

                string q = _searchText.Trim().ToLower();
                return _all
                    .Where(s => (s.Name != null && s.Name.ToLower().Contains(q))
                             || (s.Code != null && s.Code.ToLower().Contains(q)))
                    .Take(MaxResults);
            }
        }

        /// <summary>Loads the set catalog (cached) and refreshes the list.</summary>
        public async Task LoadAsync()
        {
            await SetCatalog.EnsureLoadedAsync();
            _all = SetCatalog.Sets.ToList();
            OnPropertyChanged(nameof(FilteredSets));
        }
    }
}

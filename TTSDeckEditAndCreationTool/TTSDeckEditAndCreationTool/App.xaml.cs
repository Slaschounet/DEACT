using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TTSDeckEditAndCreationTool.Store;
using TTSDeckEditAndCreationTool.ViewModel;

namespace TTSDeckEditAndCreationTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// 
    /// Initiates Navigation Store. Sets the CurrentViewModel to a new HomeViewModel instance. Initializes CardStyleCache. Opens new MainWindow with new MainViewModel.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            NavigationStore navigationStore = new NavigationStore();

            navigationStore.CurrentViewModel = new HomeViewModel(navigationStore);

            CardStyleCache.Initialize();

            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(navigationStore)
            };

            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}

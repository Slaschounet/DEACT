using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TTSDeckEditAndCreationTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void QuickBG_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            //get user specific path to save time
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            userName = userName.Split("\\")[1];
            openFileDlg.InitialDirectory = @"C:\Users\"+userName.ToString()+@"\Documents\My Games\Tabletop Simulator\Saves\Saved Objects";

            openFileDlg.Multiselect = false;

            Nullable<bool> result = openFileDlg.ShowDialog();

            //selection made
            if (result == true)
            {
                //open quick change window with selected file
            }
        }

        private void TopRow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

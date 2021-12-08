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

namespace TTSDeckEditAndCreationTool.View
{
    /// <summary>
    /// Interaction logic for CardBuilderView.xaml
    /// </summary>
    public partial class CardBuilderView : UserControl
    {
        public CardBuilderView()
        {
            InitializeComponent();
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            LowBar.Height = new GridLength(1, GridUnitType.Star);
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            LowBar.Height = new GridLength(0);
        }
    }
}

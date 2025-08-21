using System.Windows;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class WooWindow : Window
    {
        public WooWindow()
        {
            InitializeComponent();
            DataContext = new WooViewModel();
        }
    }
}

using System.Windows;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}

using System.Windows;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(string title, string message)
        {
            InitializeComponent();
            DataContext = new ErrorViewModel { Title = title, Message = message };
        }

        public ErrorWindow(ErrorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

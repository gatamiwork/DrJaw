using System.Windows;

namespace DrJaw.Views.Common
{
    public partial class WooConSet : Window
    {
        public WooConSet()
        {
            InitializeComponent();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Подключение...");
        }
    }
}
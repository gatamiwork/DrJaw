using DrJaw.Models;
using System.Windows;
using System.Windows.Controls;

namespace DrJaw.Views.Controls
{
    public partial class MainMenu : UserControl
    {
        public MainMenu()
        {
            InitializeComponent();
        }
        public void ChangeUser(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                MessageBox.Show("Главное окно не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var window = new DrJaw.Views.Common.UserLogin();
            window.Owner = Window.GetWindow(this);
            var result = window.ShowDialog();

            if (result == true)
            {
                // Пользователь сменился — обновляем панель
                if (Storage.CurrentUser != null)
                    mainWindow.ShowRolePanel(Storage.CurrentUser.Role);
            }
        }
        public void Lom_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.Common.Lom();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
    }
}

using DrJaw.Models;
using DrJaw.Views.Admin;
using DrJaw.Views.Cloud;
using DrJaw.Views.Common;
using DrJaw.Views.Controls;
using DrJaw.Views.User;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DrJaw.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Storage.CurrentUser != null)
            {
                ShowRolePanel(Storage.CurrentUser.Role);
            }
            else
            {
                MessageBox.Show("Пользователь не задан", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void ShowRolePanel(string role)
        {
            // Очистка текущей панели, если требуется
            if (RoleContent.Content is ISwitchUserPanel oldPanel)
            {
                oldPanel.CleanupBeforeUnload();
            }

            if (RoleContent.Content != null)
            {
                // Анимация исчезновения
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, e) =>
                {
                    // После исчезновения — заменить контент
                    RoleContent.Content = GetPanelByRole(role);

                    // Анимация появления
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                    (RoleContent.Content as UserControl).BeginAnimation(OpacityProperty, fadeIn);
                };

                (RoleContent.Content as UserControl).BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                // если контент пустой — просто появление
                RoleContent.Content = GetPanelByRole(role);
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                (RoleContent.Content as UserControl).BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private UserControl GetPanelByRole(string role)
        {
            switch (role.ToUpper())
            {
                case "ADMIN": return new AdminPanel();
                case "CLOUD": return new CloudPanel();
                default: return new UserPanel();
            }
        }
    }
}
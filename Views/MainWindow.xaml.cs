using DrJaw.Models;
using DrJaw.Views.Admin;
using DrJaw.Views.Cloud;
using DrJaw.Views.Common;
using DrJaw.Views.Controls;
using DrJaw.Views.User;
using DrJaw.Utils;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DrJaw.Views
{
    public partial class MainWindow : Window
    {
        public string WindowTitle { get; private set; } = "Доктор ювелирка";
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            this.KeyDown += Window_KeyDown;
            var role = Storage.CurrentUser?.Role ?? "USER";
            ShowRolePanel(role);
        }
        public void ShowRolePanel(string role)
        {
            // 1) очистка предыдущей панели (если есть)
            if (RoleContent.Content is ICleanup oldCleanup)
            {
                try { oldCleanup.Cleanup(); } catch { /* ignore */ }
            }
            // 2) плавная замена с fade
            var newContent = GetPanelByRole(role);

            var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(180)));
            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180)));

            if (RoleContent.Content is UserControl oldUC)
            {
                fadeOut.Completed += (_, __) =>
                {
                    RoleContent.Content = newContent;
                    if (RoleContent.Content is UserControl newUC)
                        newUC.BeginAnimation(OpacityProperty, fadeIn);
                };
                oldUC.BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                RoleContent.Content = newContent;
                (RoleContent.Content as UserControl)?.BeginAnimation(OpacityProperty, fadeIn);
            }

            // 3) обновить заголовок окна/статус
            UpdateTitle();
        }
        private void UpdateTitle()
        {
            var user = Storage.CurrentUser?.Name ?? "—";
            var mart = Storage.CurrentMart?.Name ?? "—";
            WindowTitle = $"Доктор ювелирка — {user} @ {mart}";
            // если Title у окна не на биндинге:
            this.Title = WindowTitle;
            // если Title на биндинге:
            // OnPropertyChanged(nameof(WindowTitle));  // если у тебя есть базовый INotifyPropertyChanged
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5 && RoleContent.Content is IRefreshable r)
            {
                r.Refresh();
                e.Handled = true;
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
        // На закрытии приложения подчистим текущую панель
        protected override void OnClosed(EventArgs e)
        {
            if (RoleContent.Content is ICleanup c)
            {
                try { c.Cleanup(); } catch { }
            }
            base.OnClosed(e);
        }
    }
}

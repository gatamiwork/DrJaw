using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class CartWindow : Window
    {
        private readonly IWindowService _windows;
        public CartWindow(IMssqlRepository repo, IUserSessionService session, IWindowService windows)
        {
            InitializeComponent();

            var vm = new CartViewModel(repo, session);
            DataContext = vm;

            vm.ErrorOccurred += (_, msg) =>
                MessageBox.Show(this, msg, "Корзина", MessageBoxButton.OK, MessageBoxImage.Error);

            vm.CloseRequested += (_, ok) =>
            {
                void DoClose()
                {
                    try { DialogResult = ok; } catch { /* если окно не модальное — игнор */ }
                    Close();
                }

                if (!IsLoaded)
                    Loaded += (_, __) => DoClose();
                else
                    Dispatcher.BeginInvoke(new Action(DoClose));
            };

            // Стартуем загрузку ПОСЛЕ показа окна
            Loaded += (_, __) => vm.RefreshCommand.Execute(null);
        }
    }
}

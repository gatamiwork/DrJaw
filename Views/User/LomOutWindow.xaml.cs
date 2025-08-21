using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class LomOutWindow : Window
    {
        public LomOutWindow(IMssqlRepository repo, IUserSessionService session)
        {
            InitializeComponent();

            var vm = new LomOutViewModel(repo, session);
            DataContext = vm;

            vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            vm.ErrorOccurred += (_, msg) => MessageBox.Show(this, msg, "Отгрузка лома", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class LomInWindow : Window
    {
        public LomInWindow(IMssqlRepository repo, IUserSessionService session)
        {
            InitializeComponent();

            var vm = new LomInViewModel(repo, session);
            DataContext = vm;

            vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            vm.ErrorOccurred += (_, msg) => MessageBox.Show(this, msg, "Приём лома", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

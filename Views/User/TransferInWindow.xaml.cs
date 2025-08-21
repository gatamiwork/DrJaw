using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class TransferInWindow : Window
    {
        public TransferInWindow(IMssqlRepository repo, IUserSessionService session)
        {
            InitializeComponent();

            var vm = new TransferInViewModel(repo, session);
            DataContext = vm;

            vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            vm.ErrorOccurred += (_, msg) => MessageBox.Show(this, msg, "Приёмка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

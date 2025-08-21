using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class DeleteItemWindow : Window
    {
        public DeleteItemWindow(IMssqlRepository repo, IUserSessionService session,
                                System.Collections.Generic.IEnumerable<DrJaw.Models.DGMSSQLItem> items)
        {
            InitializeComponent();

            var vm = new DeleteItemsViewModel(repo, session, items);
            vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            vm.ErrorOccurred += (_, msg) => MessageBox.Show(this, msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            DataContext = vm;
        }
    }
}

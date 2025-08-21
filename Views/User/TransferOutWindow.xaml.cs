using System.Collections.Generic;
using System.Windows;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class TransferOutWindow : Window
    {
        public TransferOutWindow(IMssqlRepository repo, IUserSessionService session, IReferenceDataService refData, IEnumerable<DGMSSQLItem> items)
        {
            InitializeComponent();

            var vm = new TransferOutViewModel(repo, session, refData, items);
            DataContext = vm;

            vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            vm.ErrorOccurred += (_, msg) => MessageBox.Show(this, msg, "Перемещение", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

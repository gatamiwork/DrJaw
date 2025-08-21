using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class LomWindow : Window
    {
        public LomWindow(IMssqlRepository repo, IUserSessionService session)
        {
            InitializeComponent();
            var vm = new LomViewModel(repo, session);
            DataContext = vm;

            vm.ErrorOccurred += (_, msg) =>
                MessageBox.Show(this, msg, "Отчёт по лому", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

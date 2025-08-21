using System.Windows;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class ReturnWindow : Window
    {
        public ReturnWindow(IMssqlRepository repo, IUserSessionService session)
        {
            InitializeComponent();
            var vm = new ReturnViewModel(repo, session);
            DataContext = vm;

            vm.ErrorOccurred += (_, msg) =>
                MessageBox.Show(this, msg, "Возвраты", MessageBoxButton.OK, MessageBoxImage.Error);

            vm.CloseRequested += (_, ok) =>
            {
                try { DialogResult = ok; } catch { }
                Close();
            };
        }
    }
}

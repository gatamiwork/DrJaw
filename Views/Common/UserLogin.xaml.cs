using DrJaw.Models;
using DrJaw.ViewModels.Common;
using System.Diagnostics;
using System.Windows;

namespace DrJaw.Views.Common
{
    public partial class UserLogin : Window
    {
        private readonly UserLoginViewModel _vm;
        public UserLogin()
        {
            InitializeComponent();

            _vm = new UserLoginViewModel();
            _vm.RequestClose += ok => { DialogResult = ok; Close(); };
            DataContext = _vm;

            Loaded += (_, __) =>
            {
                if (_vm.LoadCommand.CanExecute(null))
                    _vm.LoadCommand.Execute(null);
            };
        }
        private void AdminPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _vm.AdminPassword = AdminPwd.Password;
        }
    }
}

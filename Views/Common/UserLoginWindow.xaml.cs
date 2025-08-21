using System.Windows;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class UserLoginWindow : Window
    {
        private readonly UserLoginViewModel _vm;

        public UserLoginWindow(IReferenceDataService refData, IUserSessionService session, IMssqlRepository? repoForPwd = null)
        {
            InitializeComponent();

            _vm = new UserLoginViewModel(refData, session, repoForPwd);
            DataContext = _vm;

            // PasswordBox → VM
            AdminPwdBox.PasswordChanged += (_, __) => _vm.AdminPassword = AdminPwdBox.Password;

            // очищаем UI-пароль, если уходим с роли ADMIN
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(UserLoginViewModel.IsAdmin) && !_vm.IsAdmin)
                    AdminPwdBox.Password = string.Empty;
            };

            // закрытие/ошибка
            _vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            _vm.ErrorOccurred += (_, message) =>
            {
                var err = new ErrorWindow("Ошибка входа", message)
                { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                err.ShowDialog();
            };

            // Можно оставить — EnsureLoadedAsync() просто вернёт сразу, если уже загружено
            Loaded += async (_, __) => await _vm.EnsureLoadedAsync();
        }

        public MSSQLUser? SelectedUser => _vm.SelectedUser;
        public MSSQLMart? SelectedMart => _vm.SelectedMart;
        public string? AdminPasswordText => _vm.IsAdmin ? _vm.AdminPassword : null;
    }
}

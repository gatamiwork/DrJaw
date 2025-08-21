using System.Windows;
using DrJaw.Services.Config;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class MSSQLConnectWindow : Window
    {
        private readonly MSSQLConnectViewModel _vm;

        public MSSQLConnectWindow()
        {
            InitializeComponent();
            _vm = new MSSQLConnectViewModel();
            DataContext = _vm;

            // ВАЖНО: PasswordBox сам не биндится — пробрасываем вручную
            PwdBox.PasswordChanged += (_, __) => _vm.Password = PwdBox.Password;

            _vm.CloseRequested += (_, ok) => { DialogResult = ok; Close(); };
            _vm.ErrorOccurred += (_, msg) =>
            {
                var err = new ErrorWindow("Ошибка подключения", msg)
                { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                err.ShowDialog();
            };
            Loaded += (_, __) =>
            {
                var cfg = ConfigService.Load();
                _vm.Server = cfg.Mssql.Server;
                _vm.Database = cfg.Mssql.Database;
                _vm.Username = cfg.Mssql.Username;
                var pwd = Secret.Unprotect(cfg.Mssql.PasswordEnc);
                _vm.Password = pwd;
                // если есть PasswordBox:
                // PwdBox.Password = pwd;
            };
        }

        public string? BuiltConnectionString => _vm.ConnectionString;
    }
}

using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DrJaw.Models;
using DrJaw.Services.Config;
using DrJaw.ViewModels;

namespace DrJaw.ViewModels.Common
{
    public sealed class MSSQLConnectViewModel : ViewModelBase
    {
        private string _server = "";
        public string Server { get => _server; set { if (Set(ref _server, value)) RefreshCanExecute(); } }

        private string _database = "";
        public string Database { get => _database; set { if (Set(ref _database, value)) RefreshCanExecute(); } }

        private string _username = "";
        public string Username { get => _username; set { if (Set(ref _username, value)) RefreshCanExecute(); } }

        private string _password = "";
        public string Password { get => _password; set { if (Set(ref _password, value)) RefreshCanExecute(); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { if (Set(ref _isBusy, value)) RefreshCanExecute(); } }

        private string _status = "";
        public string Status { get => _status; set => Set(ref _status, value); }
        public event EventHandler<string>? ErrorOccurred;
        public string? ConnectionString { get; private set; }
        public AsyncRelayCommand ConnectCommand { get; }

        public event EventHandler<bool>? CloseRequested; // true=OK, false/none=stay

        public MSSQLConnectViewModel()
        {
            ConnectCommand = new AsyncRelayCommand(async _ => await ConnectAsync(), _ => CanConnect());
        }

        private bool CanConnect()
            => !IsBusy
               && !string.IsNullOrWhiteSpace(Server)
               && !string.IsNullOrWhiteSpace(Database)
               && !string.IsNullOrWhiteSpace(Username)
               && !string.IsNullOrWhiteSpace(Password);

        private async Task ConnectAsync()
        {
            try
            {
                IsBusy = true;
                Status = "Пробуем подключиться...";

                var sb = new SqlConnectionStringBuilder
                {
                    DataSource = Server,
                    InitialCatalog = Database,
                    UserID = Username,
                    Password = Password,
                    Encrypt = true,
                    TrustServerCertificate = true
                };
                ConnectionString = sb.ConnectionString;

                await using var cn = new SqlConnection(ConnectionString);
                await cn.OpenAsync();

                // Успех → сохраняем конфиг
                var cfg = new AppConfig
                {
                    Mssql = new MssqlConfig
                    {
                        Server = Server,
                        Database = Database,
                        Username = Username,
                        PasswordEnc = Secret.Protect(Password)
                    }
                };
                ConfigService.Save(cfg);

                Status = "Успех. Подключение установлено.";
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                Status = "Ошибка: " + ex.Message;
                ErrorOccurred?.Invoke(this, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private void RefreshCanExecute()
        {
            ConnectCommand.RaiseCanExecuteChanged();
        }
    }

}

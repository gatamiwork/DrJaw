using DrJaw.Models;
using DrJaw.Utils;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DrJaw.ViewModels.Common
{
    public class MSSQLConSetViewModel : INotifyPropertyChanged
    {
        private string _server = "";
        private string _database = "";
        private string _username = "";
        private string _password = "";
        private bool _isBusy;
        private string? _error;
        public string? Error
        {
            get => _error;
            private set
            {
                if (Set(ref _error, value) && !string.IsNullOrWhiteSpace(value))
                    SnackbarQueue.Enqueue(value); // показать снизу
            }
        }
        private CancellationTokenSource? _cts;
        public ISnackbarMessageQueue SnackbarQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(1));

        public string Server { get => _server; set => Set(ref _server, value); }
        public string Database { get => _database; set => Set(ref _database, value); }
        public string Username { get => _username; set => Set(ref _username, value); }
        public string Password { get => _password; set => Set(ref _password, value); }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (Set(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                    RaiseCanExecutes();
                }
            }
        }
        public bool IsNotBusy => !IsBusy;
        public ICommand ConnectCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public MSSQLConSetViewModel(AppSettings? initial = null)
        {
            if (initial != null)
            {
                Server = initial.MSSQL.Server ?? "";
                Database = initial.MSSQL.Database ?? "";
                Username = initial.MSSQL.Username ?? "";
                Password = initial.MSSQL.Password ?? "";
            }

            ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        }

        private bool CanConnect() =>
            !IsBusy
            && !string.IsNullOrWhiteSpace(Server)
            && !string.IsNullOrWhiteSpace(Database)
            && !string.IsNullOrWhiteSpace(Username);
        private void RaiseCanExecutes()
        {
            (ConnectCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
            (CancelCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        }

        private string BuildConnectionString() =>
            $"Server={Server};Database={Database};User Id={Username};Password={Password};TrustServerCertificate=True;";
        private async Task ConnectAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            IsBusy = true;
            Error = null;

            try
            {
                MSSQLManager.Initialize(BuildConnectionString());
                var (ok, err) = await MSSQLManager.TestConnectionAsync(_cts.Token);
                if (!ok)
                {
                    Error = string.IsNullOrWhiteSpace(err) ? "Подключение не удалось." : err;
                    return; // не закрываем
                }

                // успех → сохраняем и закрываем
                var s = AppSettingsManager.Load();
                s.MSSQL.Server = Server;
                s.MSSQL.Database = Database;
                s.MSSQL.Username = Username;
                s.MSSQL.Password = Password;
                AppSettingsManager.Save(s);

                RequestClose?.Invoke(true);
            }
            catch (OperationCanceledException)
            {
                Error = "Операция отменена.";
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
        private void Cancel() => _cts?.Cancel();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

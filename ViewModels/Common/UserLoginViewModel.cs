using DrJaw.Models;
using DrJaw.Utils;
using DrJaw.Views.User;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DrJaw.ViewModels.Common
{
    public class UserLoginViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MSSQLUser> Users { get; } = new();
        public ObservableCollection<MSSQLMart> Marts { get; } = new();

        private MSSQLUser? _selectedUser;
        public MSSQLUser? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (Set(ref _selectedUser, value))
                {
                    OnPropertyChanged(nameof(IsAdmin));
                    if (!IsAdmin) AdminPassword = string.Empty; // очистить
                    RaiseCanExecutes();
                }
            }
        }

        private MSSQLMart? _selectedMart;
        public MSSQLMart? SelectedMart
        {
            get => _selectedMart;
            set
            {
                if (Set(ref _selectedMart, value))
                    RaiseCanExecutes(); // <- добавить
            }
        }

        private string _adminPassword = "";
        public string AdminPassword { get => _adminPassword; set => Set(ref _adminPassword, value); }

        public bool IsAdmin =>
            string.Equals(SelectedUser?.Role, "ADMIN", StringComparison.OrdinalIgnoreCase);

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (Set(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                    RaiseCanExecutes(); // <- добавить
                }
            }
        }
        private void RaiseCanExecutes()
        {
            (LoginCommand as RelayCommandBase)?.RaiseCanExecuteChanged();
        }
        public bool IsNotBusy => !IsBusy;

        private string? _error;
        public string? Error
        {
            get => _error;
            private set
            {
                _error = value;
                OnPropertyChanged(); // даже если значение то же самое
                if (!string.IsNullOrWhiteSpace(value))
                    SnackbarQueue.Enqueue(value); // покажем снова
            }
        }

        public ISnackbarMessageQueue SnackbarQueue { get; } =
            new SnackbarMessageQueue(TimeSpan.FromSeconds(1));

        public ICommand LoadCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public UserLoginViewModel()
        {
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            LoginCommand = new RelayCommand(Login, CanLogin);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        }

        private async Task LoadAsync()
        {
            IsBusy = true; Error = null;
            try
            {
                // грузим прямо из Storage.Repo
                var users = await Storage.Repo.LoadUsers();
                var marts = await Storage.Repo.LoadMarts();
                var metal = await Storage.Repo.LoadMetals();

                Users.Clear(); foreach (var u in users) Users.Add(u);
                Marts.Clear(); foreach (var m in marts) Marts.Add(m);

                Storage.Users = users?.ToList() ?? new List<MSSQLUser>();
                Storage.Marts = marts?.ToList() ?? new List<MSSQLMart>();
                Storage.Metals = metal?.ToList() ?? new List<MSSQLMetal>();

                if (Storage.CurrentMetal == null)
                    Storage.CurrentMetal = Storage.Metals.FirstOrDefault();

                // предзаполнение из текущего состояния
                if (Storage.CurrentUser != null)
                    SelectedUser = Users.FirstOrDefault(u => u.Id == Storage.CurrentUser.Id) ?? Users.FirstOrDefault();

                if (Storage.CurrentMart != null)
                    SelectedMart = Marts.FirstOrDefault(m => m.Id == Storage.CurrentMart.Id) ?? Marts.FirstOrDefault();
            }
            catch (Exception ex) { Error = ex.Message; }
            finally { IsBusy = false; }
        }

        private bool CanLogin() =>
            IsNotBusy && SelectedUser != null && SelectedMart != null;

        private void Login()
        {
            if (!CanLogin()) { Error = "Выберите пользователя и магазин."; return; }

            if (IsAdmin && string.IsNullOrWhiteSpace(AdminPassword))
            { Error = "Введите пароль администратора."; return; }

            if (IsAdmin && AdminPassword != SelectedUser!.Password)
            { Error = "Пароль администратора неверный"; return; }

            Storage.CurrentUser = SelectedUser!;
            Storage.CurrentMart = SelectedMart!;

            RequestClose?.Invoke(true);
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (Equals(f, v)) return false;
            f = v; PropertyChanged?.Invoke(this, new(n)); return true;
        }
        protected void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new(n));
    }
}

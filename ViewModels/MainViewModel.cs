using System.Windows.Input;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.Admin;
using DrJaw.ViewModels.Cloud;
using DrJaw.ViewModels.User;

namespace DrJaw.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly IWindowService _windows;
        private readonly IUserSessionService _session;
        private readonly IMssqlRepository _repo;
        private readonly IReferenceDataService _refData; // ← добавили

        private string _windowTitle = "Доктор ювелирка — гость";
        public string WindowTitle { get => _windowTitle; set => Set(ref _windowTitle, value); }

        private object? _rolePanelVm;
        public object? RolePanelVm { get => _rolePanelVm; set => Set(ref _rolePanelVm, value); }
        private MSSQLUser? _lastUser;
        private string? _lastRole;
        private int? _lastMartId;

        public string CurrentRole => _session.CurrentUser?.Role ?? string.Empty;
        public string CurrentUserName => _session.CurrentUser?.Name ?? string.Empty;

        public ICommand OpenLomCommand { get; }
        public ICommand OpenOrdersCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenWooCommand { get; }
        public ICommand ChangeUserCommand { get; }

        public MainViewModel(IWindowService windows, IUserSessionService session, IReferenceDataService refData, IMssqlRepository repo)
        {
            _windows = windows;
            _session = session;
            _refData = refData;
            _repo = repo;

            OpenLomCommand = new RelayCommand(_ => _windows.ShowLom());
            OpenOrdersCommand = new RelayCommand(_ => _windows.ShowOrders());
            OpenSettingsCommand = new RelayCommand(_ => _windows.ShowSettings());
            OpenWooCommand = new RelayCommand(_ => _windows.ShowWoo());
            _lastUser = _session.CurrentUser;
            _lastRole = _session.CurrentUser?.Role;
            _lastMartId = _session.CurrentMart?.Id;

            ChangeUserCommand = new RelayCommand(_ => ChangeUser());

            // реакция на смену пользователя
            _session.Changed += OnSessionChanged;


            OnSessionChanged(this, EventArgs.Empty);

            UpdateRolePanel();
            UpdateTitle();
        }

        private void OnSessionChanged(object? sender, EventArgs e)
        {
            var curUser = _session.CurrentUser;
            var curRole = curUser?.Role;
            var curMartId = _session.CurrentMart?.Id;

            bool userChanged = !Equals(curUser?.Id, _lastUser?.Id);
            bool roleChanged = !string.Equals(curRole, _lastRole, StringComparison.OrdinalIgnoreCase);
            bool martChanged = !Equals(curMartId, _lastMartId);

            if (userChanged || roleChanged || martChanged)
            {
                _lastUser = curUser;
                _lastRole = curRole;
                _lastMartId = curMartId;
                UpdateTitle(); // ← обновляем заголовок окна
                UpdateRolePanel();   // ← пересоздаём панель ТОЛЬКО в этих случаях
                OnPropertyChanged(nameof(CurrentRole));
                OnPropertyChanged(nameof(CurrentUserName));
            }
        }
        private void ChangeUser()
        {
            var res = _windows.ShowUserLoginDialog();
            if (res is null) return;
            _session.SignIn(res.User, res.Mart); // ← сохраняем Mart в сессии
        }

        private void UpdateRolePanel()
        {
            var role = _session.CurrentUser?.Role?.ToUpperInvariant() ?? "USER";
            RolePanelVm = role switch
            {
                "ADMIN" => new AdminPanelViewModel(),
                "CLOUD" => new CloudPanelViewModel(),
                _ => new UserPanelViewModel(_windows, _refData, _repo, _session) // ← сюда
            };
        }

        private void UpdateTitle()
        {
            var userName = _session.CurrentUser?.Name ?? "гость";
            var role = _session.CurrentUser?.Role?.ToUpperInvariant() ?? "USER";

            if (role == "USER")
            {
                var martName = _session.CurrentMart?.Name ?? "—";
                WindowTitle = $"Доктор ювелирка — {userName} @ {martName}";
                StatusBar = $"{userName}  @  {martName}";
            }
            else
            {
                WindowTitle = $"Доктор ювелирка — {userName}";
                StatusBar = $"{userName}  ({role})";
            }
        }
        // MainViewModel.cs (внутри класса)
        private string _statusBar = "";
        public string StatusBar
        {
            get => _statusBar;
            set => Set(ref _statusBar, value);
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels;

namespace DrJaw.ViewModels.Common
{
    public sealed class UserLoginViewModel : ViewModelBase
    {
        private readonly IReferenceDataService _refData;
        private readonly IMssqlRepository? _repoForPwd;
        private readonly IUserSessionService _session;

        public ReadOnlyObservableCollection<MSSQLUser> Users => _refData.Users;
        public ReadOnlyObservableCollection<MSSQLMart> Marts => _refData.Marts;
        public ReadOnlyObservableCollection<MSSQLMetal> Metals => _refData.Metals;

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        public UserLoginViewModel(IReferenceDataService refData, IUserSessionService session, IMssqlRepository? repoForPwd = null)
        {
            _refData = refData ?? throw new ArgumentNullException(nameof(refData));
            _repoForPwd = repoForPwd;
            _session = session ?? throw new ArgumentNullException(nameof(session));

            OkCommand = new AsyncRelayCommand(async _ => await OnOkAsync(), _ => CanOk());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));
        }

        //public async Task EnsureLoadedAsync()
        //{
        //    await _refData.EnsureLoadedAsync();
        //    RaiseCanExec(); // пересчитать OkCommand.CanExecute()
        //}
        public async Task EnsureLoadedAsync()
        {
            await _refData.EnsureLoadedAsync();
            EnsureDefaultsForRole();        // подставит первый металл и/или магазин
            RaiseCanExec();
        }

        private MSSQLUser? _selectedUser;
        public MSSQLUser? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (Set(ref _selectedUser, value))
                {
                    UpdateRoleFlags();
                    EnsureDefaultsForRole();
                    RaiseCanExec();
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
                {
                    if (IsUser && SelectedMetal is null && Metals.Count > 0)// если пользователь, то подставляем первый металл
                        SelectedMetal = Metals[0];                          // иначе оставляем null
                    RaiseCanExec();
                }
            }
        }
        private MSSQLMetal? _selectedMetal;
        public MSSQLMetal? SelectedMetal
        {
            get => _selectedMetal;
            set
            {
                if (Set(ref _selectedMetal, value)) RaiseCanExec();
            }
        }

        private string _adminPassword = "";
        public string AdminPassword
        {
            get => _adminPassword;
            set { if (Set(ref _adminPassword, value)) RaiseCanExec(); }
        }

        private bool _isUser;
        public bool IsUser { get => _isUser; private set => Set(ref _isUser, value); }

        private bool _isAdmin;
        public bool IsAdmin { get => _isAdmin; private set => Set(ref _isAdmin, value); }

        public AsyncRelayCommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task OnOkAsync()
        {
            if (SelectedUser is null) return;

            if (IsAdmin)
            {
                if (_repoForPwd is null)
                {
                    ErrorOccurred?.Invoke(this, "Не настроена проверка пароля.");
                    return;
                }

                var ok = await _repoForPwd.CheckPasswordAsync(SelectedUser.Id, AdminPassword);
                if (!ok)
                {
                    ErrorOccurred?.Invoke(this, "Неверный пароль администратора.");
                    return;
                }
            }

            _session.SignIn(SelectedUser, IsAdmin ? null : SelectedMart);
            CloseRequested?.Invoke(this, true);
        }

        private void UpdateRoleFlags()
        {
            var role = SelectedUser?.Role?.ToUpperInvariant() ?? "USER";
            IsUser = role == "USER";
            IsAdmin = role == "ADMIN";
        }

        private void EnsureDefaultsForRole()
        {
            if (IsUser && SelectedMart is null && Marts.Count > 0)
                SelectedMart = Marts[0];

            if (!IsAdmin) AdminPassword = "";
        }

        private bool CanOk()
        {
            if (SelectedUser is null) return false;
            if (IsUser && SelectedMart is null) return false;
            if (IsAdmin)
            {
                if (_repoForPwd is null) return false;
                if (string.IsNullOrWhiteSpace(AdminPassword)) return false;
            }
            return true;
        }

        private void RaiseCanExec() => OkCommand.RaiseCanExecuteChanged();

        public void SetInitialSelection(MSSQLUser? user, MSSQLMart? mart, MSSQLMetal? metal = null)
        {
            if (user != null)
            {
                var u = Users.FirstOrDefault(x => x.Id == user.Id);
                if (u != null) SelectedUser = u;
            }
            if (IsUser && mart != null)
            {
                var m = Marts.FirstOrDefault(x => x.Id == mart.Id);
                if (m != null) SelectedMart = m;
            }
            if (IsUser && metal != null)
            {
                var me = Metals.FirstOrDefault(x => x.Id == metal.Id);
                if (me != null) SelectedMetal = me;
            }
        }
    }
}

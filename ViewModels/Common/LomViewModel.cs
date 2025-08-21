using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.MSSQL;

namespace DrJaw.ViewModels.Common
{
    public sealed class LomViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private CancellationTokenSource? _reloadCts;
        private bool _suspendAutoReload;

        public ObservableCollection<MSSQLMart> Marts { get; } = new();
        public ObservableCollection<MSSQLUser> Users { get; } = new();
        public ObservableCollection<LomRowViewModel> Rows { get; } = new();

        private MSSQLMart? _selectedMart;
        public MSSQLMart? SelectedMart
        {
            get => _selectedMart;
            set { if (Set(ref _selectedMart, value) && !_suspendAutoReload) DebouncedReload(); }
        }

        private MSSQLUser? _selectedUser;
        public MSSQLUser? SelectedUser
        {
            get => _selectedUser;
            set { if (Set(ref _selectedUser, value) && !_suspendAutoReload) DebouncedReload(); }
        }

        private DateTime _fromDate = DateTime.Today.AddMonths(-1);
        public DateTime FromDate
        {
            get => _fromDate;
            set { if (Set(ref _fromDate, value)) DebouncedReload(); }
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set { if (Set(ref _toDate, value)) DebouncedReload(); }
        }

        // Totals
        private decimal _openingWeight;
        public decimal OpeningWeight { get => _openingWeight; private set => Set(ref _openingWeight, value); }

        private decimal _openingSum;
        public decimal OpeningSum { get => _openingSum; private set => Set(ref _openingSum, value); }

        private decimal _periodNetWeight;
        public decimal PeriodNetWeight { get => _periodNetWeight; private set => Set(ref _periodNetWeight, value); }

        private decimal _periodNetSum;
        public decimal PeriodNetSum { get => _periodNetSum; private set => Set(ref _periodNetSum, value); }

        private decimal _closingWeight;
        public decimal ClosingWeight { get => _closingWeight; private set => Set(ref _closingWeight, value); }

        private decimal _closingSum;
        public decimal ClosingSum { get => _closingSum; private set => Set(ref _closingSum, value); }

        public event EventHandler<string>? ErrorOccurred;

        public LomViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            try
            {
                _suspendAutoReload = true;

                // Справочники
                var marts = await _repo.GetMartsAsync();
                Marts.Clear();
                // sentinel "Все магазины"
                var allMarts = new MSSQLMart { Id = 0, Name = "Все магазины" };
                Marts.Add(allMarts);
                foreach (var m in marts) Marts.Add(m);

                var users = await _repo.GetUsersAsync();
                Users.Clear();
                // sentinel "Все пользователи"
                var allUsers = new MSSQLUser { Id = 0, Name = "Все пользователи" };
                Users.Add(allUsers);
                foreach (var u in users) Users.Add(u);

                // Дефолт: «все»
                SelectedMart = allMarts;
                SelectedUser = allUsers;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка инициализации: {ex.Message}");
            }
            finally
            {
                _suspendAutoReload = false;
            }

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (FromDate > ToDate) return;

            try
            {
                // martId: 0 = все (репозиторий должен игнорировать фильтр по магазину при 0)
                var martId = SelectedMart?.Id ?? 0;
                // userId: null = все пользователи
                int? userId = (SelectedUser != null && SelectedUser.Id != 0) ? SelectedUser.Id : (int?)null;

                var (opW, opS) = await _repo.GetLomOpeningAsync(FromDate, martId, userId);
                OpeningWeight = opW;
                OpeningSum = opS;

                var list = await _repo.GetLomMovementsAsync(FromDate, ToDate, martId, userId);

                Rows.Clear();
                foreach (var r in list)
                    Rows.Add(new LomRowViewModel(r));

                RecalcTotals();
            }
            catch (NotImplementedException)
            {
                ErrorOccurred?.Invoke(this, "Методы отчёта по лому ещё не реализованы в репозитории: GetLomOpeningAsync / GetLomMovementsAsync.");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка загрузки отчёта: {ex.Message}");
            }
        }

        private void RecalcTotals()
        {
            PeriodNetWeight = Rows.Sum(r => r.IsIn ? r.Weight : -r.Weight);
            PeriodNetSum = Rows.Sum(r => r.IsIn ? r.Amount : -r.Amount);

            ClosingWeight = OpeningWeight + PeriodNetWeight;
            ClosingSum = OpeningSum + PeriodNetSum;
        }

        private async void DebouncedReload()
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();
            var token = _reloadCts.Token;
            try
            {
                await Task.Delay(200, token);
                if (!token.IsCancellationRequested)
                    await LoadAsync();
            }
            catch (TaskCanceledException) { }
        }
    }

    public sealed class LomRowViewModel : ViewModelBase
    {
        public DateTime Date { get; }
        public bool IsIn { get; }
        public string DirectionText => IsIn ? "Приём" : "Отгрузка";

        public decimal Weight { get; }
        public decimal? PricePerGram { get; }
        public decimal Amount { get; }

        public string? UserName { get; }
        public string? Comment { get; }

        public LomRowViewModel(LomDto dto)
        {
            Date = dto.Date;
            IsIn = dto.IsIn;
            Weight = dto.Weight;
            PricePerGram = dto.PricePerGram;
            Amount = dto.Amount;
            UserName = dto.UserName;
            Comment = dto.Comment;
        }
    }
}

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels;

namespace DrJaw.ViewModels.User
{
    public sealed class LomOutViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;

        public LomOutViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            SaveCommand = new AsyncRelayCommand(async _ => await OnSaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

            Weight = "";
        }

        private string _weight = "";
        public string Weight
        {
            get => _weight;
            set { if (Set(ref _weight, (value ?? "").Trim())) SaveCommand.RaiseCanExecuteChanged(); }
        }

        private static decimal? ParseDec(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Replace(',', '.');
            return decimal.TryParse(s,
                                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                                    CultureInfo.InvariantCulture,
                                    out var v)
                 ? v : (decimal?)null;
        }

        public decimal? WeightDec => ParseDec(Weight);

        public AsyncRelayCommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        private bool CanSave()
        {
            var w = WeightDec;
            return w.HasValue && w.Value > 0m;
        }

        private async Task OnSaveAsync()
        {
            try
            {
                var userId = _session.CurrentUser?.Id
                             ?? throw new InvalidOperationException("Не выбран пользователь.");
                var martId = _session.CurrentMart?.Id
                             ?? throw new InvalidOperationException("Не выбран магазин.");

                var weight2 = Math.Round(WeightDec!.Value, 2, MidpointRounding.AwayFromZero);

                // receiving = false (отгрузка), цена за грамм не нужна -> null
                await _repo.CreateLomAsync(
                    userId: userId,
                    martId: martId,
                    receiving: false,
                    weight: weight2,
                    pricePerGram: null
                );

                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка отгрузки лома:\n{ex.Message}");
            }
        }
    }
}

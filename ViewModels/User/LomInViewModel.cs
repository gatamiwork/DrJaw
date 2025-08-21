using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using DrJaw.Services;
using DrJaw.Services.MSSQL;

namespace DrJaw.ViewModels.User
{
    public sealed class LomInViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;

        public LomInViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            SaveCommand = new AsyncRelayCommand(async _ => await OnSaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

            Weight = "";
            PricePerGram = "";
        }

        private string _weight = "";
        public string Weight
        {
            get => _weight;
            set
            {
                if (Set(ref _weight, (value ?? "").Trim()))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(Total));
                    OnPropertyChanged(nameof(TotalText));
                }
            }
        }

        private string _pricePerGram = "";
        public string PricePerGram
        {
            get => _pricePerGram;
            set
            {
                if (Set(ref _pricePerGram, (value ?? "").Trim()))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(Total));
                    OnPropertyChanged(nameof(TotalText));
                }
            }
        }

        private static decimal? ParseDec(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Replace(',', '.');
            return decimal.TryParse(s,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var v)
                ? v : (decimal?)null;
        }

        public decimal? WeightDec => ParseDec(Weight);
        public decimal? PricePerGramDec => ParseDec(PricePerGram);

        public decimal Total
        {
            get
            {
                var w = WeightDec ?? 0m;
                var p = PricePerGramDec ?? 0m;
                return Math.Round(w * p, 2, MidpointRounding.AwayFromZero);
            }
        }
        public string TotalText => $"Итого: {Total:N2}";

        public AsyncRelayCommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        private bool CanSave()
        {
            var w = WeightDec;
            var p = PricePerGramDec;
            return w.HasValue && w.Value > 0m
                && p.HasValue && p.Value > 0m;
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
                var price2 = Math.Round(PricePerGramDec!.Value, 2, MidpointRounding.AwayFromZero);

                // Приём лома: receiving = true
                await _repo.CreateLomAsync(
                    userId: userId,
                    martId: martId,
                    receiving: true,
                    weight: weight2,
                    pricePerGram: price2
                );

                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка приёма лома:\n{ex.Message}");
            }
        }
    }
}

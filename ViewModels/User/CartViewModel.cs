using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.MSSQL;

namespace DrJaw.ViewModels.User
{
    public sealed class CartViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private readonly ObservableCollection<CartLineViewModel> _items = new();
        private static readonly int[] _globalBonusOptions = new[] { 0, 3, 5, 7 };
        public ObservableCollection<MSSQLPaymentType> PaymentTypes { get; } = new();
        private MSSQLPaymentType? _selectedPaymentType;
        public IReadOnlyList<int> GlobalBonusOptions => _globalBonusOptions;
        private string _statusText = "";

        public ReadOnlyObservableCollection<CartLineViewModel> Items { get; }

        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand PayCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand AddLomCommand { get; }
        public ICommand RemoveLomCommand { get; }
        public AsyncRelayCommand CancelOrderCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler? AddLomRequested;
        public event EventHandler? RemoveLomRequested;

        public CartViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Items = new ReadOnlyObservableCollection<CartLineViewModel>(_items);

            AddLomCommand = new RelayCommand(_ => AddLomRequested?.Invoke(this, EventArgs.Empty));
            RemoveLomCommand = new RelayCommand(_ => RemoveLomRequested?.Invoke(this, EventArgs.Empty));
            CancelOrderCommand = new AsyncRelayCommand(async _ => await OnCancelOrderAsync());
            PayCommand = new AsyncRelayCommand(async _ => await OnPayAsync(), _ => CanPay());

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());
            CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));
        }

        private async Task LoadAsync()
        {
            try
            {
                var martId = _session.CurrentMart?.Id
                             ?? throw new InvalidOperationException("Не выбран магазин.");

                // 1) Платежи (один раз при открытии достаточно)
                if (PaymentTypes.Count == 0)
                {
                    var pts = await _repo.GetPaymentTypesAsync();   // <-- добавь в репозиторий
                    PaymentTypes.Clear();
                    foreach (var pt in pts) PaymentTypes.Add(pt);
                    SelectedPaymentType = PaymentTypes.FirstOrDefault();
                }

                // 2) Позиции корзины (как было)
                var groups = await _repo.GetReadyToSellGroupsAsync(martId);
                _items.Clear();
                foreach (var g in groups)
                {
                    IEnumerable<int> idList = (g.Ids != null && g.Ids.Count > 0)
                        ? g.Ids.AsEnumerable()
                        : Enumerable.Empty<int>();

                    foreach (var id in idList)
                    {
                        var vm = new CartLineViewModel(
                            id: id,
                            articul: g.Articul ?? "",
                            size: g.Size,
                            stones: g.Stones,
                            price: g.Price,
                            weight: g.Weight,              // если уже возвращаешь вес
                            onChanged: OnLineChanged,
                            onRemoveAsync: OnRemoveAsync);

                        _items.Add(vm);
                    }
                }

                _ = LoadThumbnailsAsync();
                RecalcTotals();
                PayCommand.RaiseCanExecuteChanged();
                AutoCloseIfEmpty();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Не удалось загрузить корзину:\n{ex.Message}");
            }
        }

        private async Task LoadThumbnailsAsync()
        {
            foreach (var line in _items)
            {
                try
                {
                    if (line.Id <= 0) continue;
                    var bytes = await _repo.GetItemImageAsync(line.Id);
                    if (bytes is { Length: > 0 })
                        line.SetThumbnail(BytesToBitmap(bytes));
                }
                catch { /* превью опционально */ }
            }
        }

        private static ImageSource BytesToBitmap(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        private void AutoCloseIfEmpty()
        {
            if (_items.Count == 0)
                CloseRequested?.Invoke(this, false); // false = закрыли без оплаты
        }

        private void OnLineChanged() => RecalcTotals();

        // totals (теперь «кол-во» = число строк)
        private int _totalQty;
        public int TotalQty
        {
            get => _totalQty;
            private set => Set(ref _totalQty, value);
        }
        private int _selectedGlobalBonusPercent = 0;
        public int SelectedGlobalBonusPercent
        {
            get => _selectedGlobalBonusPercent;
            set
            {
                if (Set(ref _selectedGlobalBonusPercent, value))
                    RecalcTotals(); // пересчитываем общий итог, строки оставляем как есть
            }
        }
        public MSSQLPaymentType? SelectedPaymentType
        {
            get => _selectedPaymentType;
            set => Set(ref _selectedPaymentType, value);
        }
        public string StatusText
        {
            get => _statusText;
            set => Set(ref _statusText, value);
        }

        private decimal _totalSum;
        public decimal TotalSum
        {
            get => _totalSum;
            private set => Set(ref _totalSum, value);
        }
        private const int DefaultCartItemStatusId = 1;
        private async Task OnPayAsync()
        {
            try
            {
                var martId = _session.CurrentMart?.Id
                             ?? throw new InvalidOperationException("Не выбран магазин.");
                var userId = _session.CurrentUser?.Id
                             ?? throw new InvalidOperationException("Не выбран пользователь.");
                var paymentTypeId = SelectedPaymentType?.Id
                             ?? throw new InvalidOperationException("Выберите тип оплаты.");
                if (Items.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "В корзине нет позиций для оплаты.");
                    return;
                }

                // глобальная скидка в деньгах на КАЖДУЮ позицию
                decimal ExtraPerItem(decimal price) =>
                    Math.Round(price * SelectedGlobalBonusPercent / 100m, 2, MidpointRounding.AwayFromZero);

                // линии
                var lines = new List<CartCreateLine>(Items.Count);
                decimal cartBonus = 0m;
                decimal totalSum = 0m;

                foreach (var line in Items)
                {
                    var perItemBonus = line.BonusPerUnit + ExtraPerItem(line.Price);
                    perItemBonus = Math.Round(perItemBonus, 2, MidpointRounding.AwayFromZero);

                    var lineSum = Math.Max(0m, line.Price - perItemBonus);
                    lineSum = Math.Round(lineSum, 2, MidpointRounding.AwayFromZero);

                    cartBonus += perItemBonus;
                    totalSum += lineSum;

                    lines.Add(new CartCreateLine(
                        ItemId: line.Id,
                        Bonus: perItemBonus,
                        StatusId: DefaultCartItemStatusId
                    ));
                }

                cartBonus = Math.Round(cartBonus, 2, MidpointRounding.AwayFromZero);
                totalSum = Math.Round(totalSum, 2, MidpointRounding.AwayFromZero);

                var dto = new CartCreate(
                    UserId: userId,
                    MartId: martId,
                    PaymentTypeId: paymentTypeId,
                    PurchaseDateUtc: DateTime.UtcNow,
                    CartBonus: cartBonus,
                    TotalSum: totalSum,
                    LomId: null,                  // если нужен лом — подставишь id
                    Lines: lines
                );

                var cartId = await _repo.CreateCartWithItemsAsync(dto);

                // при желании:
                // StatusText = $"Оформлен заказ №{cartId} на {totalSum:N2}";
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Не удалось провести оплату:\n{ex.Message}");
            }
        }
        private bool CanPay() => Items.Count > 0 && SelectedPaymentType != null;

        public string TotalText => $"Итого: {TotalQty} шт. на сумму {TotalSum:N2}";

        private void RecalcTotals()
        {
            TotalQty = _items.Count;

            decimal TotalForLine(CartLineViewModel line)
            {
                var extra = Math.Round(line.Price * SelectedGlobalBonusPercent / 100m,
                                       2, MidpointRounding.AwayFromZero);
                var perItem = Math.Max(0m, line.Price - line.BonusPerUnit - extra);
                return perItem;
            }

            TotalSum = _items.Sum(TotalForLine);
            OnPropertyChanged(nameof(TotalText));
            PayCommand.RaiseCanExecuteChanged();
        }
        private async Task OnRemoveAsync(CartLineViewModel line)
        {
            try
            {
                await _repo.UnmarkReadyToSoldAsync(new[] { line.Id });
                _items.Remove(line);
                RecalcTotals();
                PayCommand.RaiseCanExecuteChanged();
                AutoCloseIfEmpty();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Не удалось убрать из корзины:\n{ex.Message}");
            }
        }
        private async Task OnCancelOrderAsync()
        {
            try
            {
                var ids = Items.Select(i => i.Id).Distinct().ToList();
                if (ids.Count == 0) { StatusText = "В корзине нет позиций."; return; }

                await _repo.UnmarkReadyToSoldAsync(ids);
                StatusText = "Заказ отменён. Позиции возвращены из корзины.";
                await LoadAsync(); // перезагрузим список
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Не удалось отменить заказ:\n{ex.Message}");
            }
        }
    }

    public sealed class CartLineViewModel : ViewModelBase
    {
        private readonly Action _onChanged;
        private readonly Func<CartLineViewModel, System.Threading.Tasks.Task> _onRemoveAsync;

        public int Id { get; }
        public string Articul { get; }
        public string? Size { get; }
        public string? Stones { get; }
        public decimal Price { get; }
        public decimal Weight { get; }

        // бонус в процентах
        private static readonly int[] _bonusOptions = new[] { 0, 3, 5, 7 };
        public IReadOnlyList<int> BonusOptions => _bonusOptions;

        private int _selectedBonusPercent = 0;
        public int SelectedBonusPercent
        {
            get => _selectedBonusPercent;
            set
            {
                if (Set(ref _selectedBonusPercent, value))
                {
                    OnPropertyChanged(nameof(BonusPerUnit));
                    OnPropertyChanged(nameof(LineTotal));
                    _onChanged();
                }
            }
        }

        public decimal BonusPerUnit =>
            Math.Round(Price * SelectedBonusPercent / 100m, 2, MidpointRounding.AwayFromZero);

        // Одна строка = один товар ⇒ итог = цена - скидка
        public decimal LineTotal => Math.Max(0m, Price - BonusPerUnit);

        private ImageSource? _thumbnail;
        public ImageSource? Thumbnail
        {
            get => _thumbnail;
            private set => Set(ref _thumbnail, value);
        }

        public AsyncRelayCommand RemoveCommand { get; }

        public CartLineViewModel(
            int id,
            string articul,
            string? size,
            string? stones,
            decimal price,
            decimal weight,
            Action onChanged,
            Func<CartLineViewModel, System.Threading.Tasks.Task> onRemoveAsync)
        {
            Id = id;
            Articul = articul ?? "";
            Size = size;
            Stones = stones;
            Price = price;
            Weight = weight;
            _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
            _onRemoveAsync = onRemoveAsync ?? throw new ArgumentNullException(nameof(onRemoveAsync));

            SelectedBonusPercent = 0;
            RemoveCommand = new AsyncRelayCommand(async _ => await _onRemoveAsync(this));
        }

        public void SetThumbnail(ImageSource? img) => Thumbnail = img;
    }

}

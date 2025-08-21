using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.MSSQL;

namespace DrJaw.ViewModels.Common
{
    public sealed class OrdersViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private CancellationTokenSource? _reloadCts;

        public ObservableCollection<MSSQLMart> Marts { get; } = new();
        public ObservableCollection<MSSQLUser> Users { get; } = new();
        private readonly ObservableCollection<OrderLineViewModel> _lines = new();
        public ReadOnlyObservableCollection<OrderLineViewModel> Lines { get; }

        private MSSQLMart? _selectedMart;
        public MSSQLMart? SelectedMart
        {
            get => _selectedMart;
            set { if (Set(ref _selectedMart, value)) DebouncedReload(); }
        }

        private MSSQLUser? _selectedUser;
        public MSSQLUser? SelectedUser
        {
            get => _selectedUser;
            set { if (Set(ref _selectedUser, value)) DebouncedReload(); }
        }

        private DateTime _fromDate = DateTime.Today.AddMonths(-1);
        public DateTime FromDate
        {
            get => _fromDate;
            set { if (Set(ref _fromDate, value)) _ = ReloadAsync(); }
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set { if (Set(ref _toDate, value)) _ = ReloadAsync(); }
        }

        public AsyncRelayCommand RefreshCommand { get; }

        // Итоги
        private decimal _goldWeight, _goldSum;
        private int _goldCount;
        private decimal _silverWeight, _silverSum;
        private int _silverCount;

        // Итоги (отдельные поля)
        public decimal GoldWeight { get; private set; }
        public int GoldCount { get; private set; }
        public decimal GoldSum { get; private set; }

        public decimal SilverWeight { get; private set; }
        public int SilverCount { get; private set; }
        public decimal SilverSum { get; private set; }

        // 10% от суммарной выручки
        public decimal TenPercentSum => Math.Round((GoldSum + SilverSum) * 0.10m, 2);

        public event EventHandler<string>? ErrorOccurred;

        public OrdersViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Lines = new ReadOnlyObservableCollection<OrderLineViewModel>(_lines);
            RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());

            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            try
            {
                // справочники
                var marts = await _repo.GetMartsAsync();
                Marts.Clear();
                Marts.Add(new MSSQLMart { Id = 0, Name = "Все магазины" }); // ALL
                foreach (var m in marts) Marts.Add(m);

                var users = await _repo.GetUsersAsync();
                Users.Clear();
                Users.Add(new MSSQLUser { Id = 0, Name = "Все пользователи" }); // ALL
                foreach (var u in users) Users.Add(u);

                SelectedMart = Marts.FirstOrDefault(); // "Все магазины"
                SelectedUser = Users.FirstOrDefault(); // "Все пользователи"

                await LoadAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка инициализации: {ex.Message}");
            }
        }

        private async Task ReloadAsync()
        {
            if (FromDate > ToDate) return;
            await LoadAsync();
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

        private async Task LoadAsync()
        {
            try
            {
                _lines.Clear();

                int? martId = (SelectedMart == null || SelectedMart.Id == 0) ? (int?)null : SelectedMart.Id;
                int? userId = (SelectedUser == null || SelectedUser.Id == 0) ? (int?)null : SelectedUser.Id;

                for (var d = FromDate.Date; d <= ToDate.Date; d = d.AddDays(1))
                {
                    var dayLines = await _repo.GetSoldItemsByDateAsync(martId, d, userId);

                    foreach (var dto in dayLines)
                    {
                        var vm = new OrderLineViewModel(dto);
                        _lines.Add(vm);
                        _ = LoadThumbAsync(vm);
                    }
                }

                RecalcTotals();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private async Task LoadThumbAsync(OrderLineViewModel line)
        {
            try
            {
                var bytes = await _repo.GetItemImageAsync(line.ItemId);
                if (bytes is { Length: > 0 })
                    line.SetThumbnail(BytesToBitmap(bytes));
            }
            catch { /* превью не критично */ }
        }

        private static ImageSource BytesToBitmap(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 96;   // под размер превью
            bmp.DecodePixelHeight = 96;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private void RecalcTotals()
        {
            bool IsGold(string? m) =>
                !string.IsNullOrWhiteSpace(m) &&
                (m.IndexOf("au", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 m.IndexOf("зол", StringComparison.OrdinalIgnoreCase) >= 0);

            bool IsSilver(string? m) =>
                !string.IsNullOrWhiteSpace(m) &&
                (m.IndexOf("ag", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 m.IndexOf("сереб", StringComparison.OrdinalIgnoreCase) >= 0);

            var gold = _lines.Where(x => IsGold(x.Metal));
            var silver = _lines.Where(x => IsSilver(x.Metal));

            GoldWeight = gold.Sum(x => x.Weight);
            GoldCount = gold.Count();
            GoldSum = gold.Sum(x => x.Price);

            SilverWeight = silver.Sum(x => x.Weight);
            SilverCount = silver.Count();
            SilverSum = silver.Sum(x => x.Price);

            OnPropertyChanged(nameof(GoldWeight));
            OnPropertyChanged(nameof(GoldCount));
            OnPropertyChanged(nameof(GoldSum));
            OnPropertyChanged(nameof(SilverWeight));
            OnPropertyChanged(nameof(SilverCount));
            OnPropertyChanged(nameof(SilverSum));
            OnPropertyChanged(nameof(TenPercentSum));
        }
    }

    public sealed class OrderLineViewModel : ViewModelBase
    {
        public int ItemId { get; }
        public int CartId { get; }
        public string GroupKey { get; }

        public string Articul { get; }
        public string Metal { get; }
        public string? Size { get; }
        public decimal Weight { get; }
        public string? Stones { get; }
        public decimal Price { get; }

        public DateTime PurchaseDate { get; }
        public string PaymentTypeName { get; }

        private ImageSource? _thumbnail;
        public ImageSource? Thumbnail
        {
            get => _thumbnail;
            private set => Set(ref _thumbnail, value);
        }

        public OrderLineViewModel(ReturnItemDto dto)
        {
            ItemId = dto.ItemId;
            CartId = dto.CartId;

            Articul = dto.Articul ?? "";
            Metal = dto.Metal ?? "";
            Size = dto.Size;
            Weight = dto.Weight;
            Stones = dto.Stones;
            Price = dto.Price;

            PurchaseDate = dto.PurchaseDate;
            PaymentTypeName = dto.PaymentTypeName ?? "";

            // Группа: Чек #Id • дата • тип оплаты
            GroupKey = $"Чек #{CartId} • {PurchaseDate:yyyy-MM-dd} • {PaymentTypeName}";
        }

        public void SetThumbnail(ImageSource? img) => Thumbnail = img;
    }
}

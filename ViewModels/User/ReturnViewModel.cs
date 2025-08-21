using System;
using System.Collections.ObjectModel;
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
    public sealed class ReturnViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private readonly ObservableCollection<ReturnLineViewModel> _lines = new();

        public ReadOnlyObservableCollection<ReturnLineViewModel> Lines { get; }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (Set(ref _selectedDate, value))
                    _ = LoadAsync(); // авто-перезагрузка по смене даты
            }
        }

        public AsyncRelayCommand ReturnCommand { get; }
        public ICommand CloseCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        public ReturnViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Lines = new ReadOnlyObservableCollection<ReturnLineViewModel>(_lines);

            ReturnCommand = new AsyncRelayCommand(async _ => await OnReturnAsync(), _ => CanReturn());
            CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var martId = _session.CurrentMart?.Id
                             ?? throw new InvalidOperationException("Не выбран магазин.");

                var list = await _repo.GetSoldItemsByDateAsync(martId, SelectedDate);

                _lines.Clear();
                foreach (var dto in list)
                    _lines.Add(new ReturnLineViewModel(dto, OnLineChanged));

                _ = LoadThumbnailsAsync();
                RaiseFooter();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Не удалось загрузить продажи:\n{ex.Message}");
            }
        }

        private async Task LoadThumbnailsAsync()
        {
            foreach (var line in _lines)
            {
                try
                {
                    var bytes = await _repo.GetItemImageAsync(line.ItemId);
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

        private void OnLineChanged()
        {
            RaiseFooter();
            ReturnCommand.RaiseCanExecuteChanged();
        }

        private bool CanReturn() => _lines.Any(l => l.IsMarked);

        private async Task OnReturnAsync()
        {
            try
            {
                var ids = _lines.Where(l => l.IsMarked).Select(l => l.ItemId).Distinct().ToList();
                if (ids.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "Не выбрано ни одной позиции для возврата.");
                    return;
                }

                await _repo.ReturnItemsAsync(ids);

                // Можно закрыть окно (успешный возврат)
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка возврата:\n{ex.Message}");
            }
        }

        // ==== Итоги (футер) ====
        private int _markedCount;
        public int MarkedCount
        {
            get => _markedCount;
            private set => Set(ref _markedCount, value);
        }

        private decimal _markedSum;
        public decimal MarkedSum
        {
            get => _markedSum;
            private set => Set(ref _markedSum, value);
        }

        public string FooterText => $"К возврату: {MarkedCount} шт. на сумму {MarkedSum:N2}";

        private void RaiseFooter()
        {
            MarkedCount = _lines.Count(l => l.IsMarked);
            MarkedSum = _lines.Where(l => l.IsMarked).Sum(l => l.Price);
            OnPropertyChanged(nameof(FooterText));
        }
    }

    public sealed class ReturnLineViewModel : ViewModelBase
    {
        private readonly Action _onChanged;

        public int ItemId { get; }
        public int CartId { get; }

        public string Articul { get; }
        public string Metal { get; }
        public string? Size { get; }
        public decimal Weight { get; }
        public string? Stones { get; }
        public decimal Price { get; }

        public DateTime PurchaseDate { get; }
        public string PaymentTypeName { get; }

        // Ключ заголовка группы: "Чек #123 • 2025-08-18 • Наличные"
        public string GroupKey { get; }

        private bool _isMarked;
        public bool IsMarked
        {
            get => _isMarked;
            set
            {
                if (Set(ref _isMarked, value))
                    _onChanged();
            }
        }

        private ImageSource? _thumbnail;
        public ImageSource? Thumbnail
        {
            get => _thumbnail;
            private set => Set(ref _thumbnail, value);
        }

        public ReturnLineViewModel(ReturnItemDto dto, Action onChanged)
        {
            _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));

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

            GroupKey = $"Чек #{CartId} • {PurchaseDate:yyyy-MM-dd} • {PaymentTypeName}";
        }

        public void SetThumbnail(ImageSource? img) => Thumbnail = img;
    }
}

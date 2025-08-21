using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.MSSQL;

namespace DrJaw.ViewModels.User
{
    public sealed class TransferInViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;

        private readonly ObservableCollection<TransferInCardItemViewModel> _items = new();
        public ReadOnlyObservableCollection<TransferInCardItemViewModel> Items { get; }

        public AsyncRelayCommand AcceptCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        public TransferInViewModel(IMssqlRepository repo, IUserSessionService session)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Items = new ReadOnlyObservableCollection<TransferInCardItemViewModel>(_items);

            AcceptCommand = new AsyncRelayCommand(async _ => await OnAcceptAsync(), _ => CanAccept());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var martId = _session.CurrentMart?.Id
                    ?? throw new InvalidOperationException("Не выбран магазин.");

                // Берём входящие к текущему магазину
                var groups = await _repo.GetIncomingTransferItemsAsync(martId);
                _items.Clear();

                foreach (var g in groups)
                {
                    var vm = new TransferInCardItemViewModel(g, OnCardChanged);
                    _items.Add(vm);
                }

                _ = LoadThumbnailsAsync();
                RaiseTotals();
                AcceptCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Не удалось загрузить входящие позиции:\n{ex.Message}");
            }
        }

        private void OnCardChanged()
        {
            RaiseTotals();
            AcceptCommand.RaiseCanExecuteChanged();
        }

        // Totals
        private int _totalQty;
        public int TotalQty
        {
            get => _totalQty;
            private set => Set(ref _totalQty, value);
        }

        private decimal _totalSum;
        public decimal TotalSum
        {
            get => _totalSum;
            private set => Set(ref _totalSum, value);
        }

        public string TotalSelectedText => $"К приёмке: {TotalQty} шт. на сумму {TotalSum:N2}";

        private void RaiseTotals()
        {
            var sel = _items.Where(i => i.IsChecked);
            TotalQty = sel.Sum(i => i.ItemCount);
            TotalSum = sel.Sum(i => i.ItemCount * i.Price);
            OnPropertyChanged(nameof(TotalSelectedText));
        }

        private bool CanAccept() => _items.Any(i => i.IsChecked);

        private async Task OnAcceptAsync()
        {
            try
            {
                var martId = _session.CurrentMart?.Id
                             ?? throw new InvalidOperationException("Не выбран магазин.");

                var ids = _items.Where(i => i.IsChecked)
                                .SelectMany(i => i.Ids)
                                .Distinct()
                                .ToList();

                if (ids.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "Не выбрано ни одной позиции.");
                    return;
                }

                await _repo.TransferInItemsByIdsAsync(martId, ids);

                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка при приёмке:\n{ex.Message}");
            }
        }

        private async Task LoadThumbnailsAsync()
        {
            foreach (var card in _items)
            {
                try
                {
                    var firstId = card.FirstId;
                    if (firstId <= 0) continue;

                    var bytes = await _repo.GetItemImageAsync(firstId);
                    if (bytes is { Length: > 0 })
                        card.SetThumbnail(BytesToBitmap(bytes));
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
    }

    public sealed class TransferInCardItemViewModel : ViewModelBase
    {
        private readonly Action _onChanged;

        public string Articul { get; }
        public string Metal { get; }
        public decimal Price { get; }
        public int ItemCount { get; }   // количество штук в группе
        public IReadOnlyList<int> Ids { get; }
        public int FirstId => (Ids?.Count ?? 0) > 0 ? Ids[0] : 0;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (Set(ref _isChecked, value))
                {
                    _onChanged();
                }
            }
        }

        private ImageSource? _thumbnail;
        public ImageSource? Thumbnail
        {
            get => _thumbnail;
            private set => Set(ref _thumbnail, value);
        }

        public TransferInCardItemViewModel(DGMSSQLItem src, Action onChanged)
        {
            _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
            Articul = src.Articul ?? "";
            Metal = src.Metal ?? "";
            Price = src.Price;
            ItemCount = Math.Max(0, src.ItemCount);
            Ids = (src.Ids != null && src.Ids.Count > 0) ? src.Ids.ToList() : Array.Empty<int>();
        }

        public void SetThumbnail(ImageSource? img) => Thumbnail = img;
    }
}

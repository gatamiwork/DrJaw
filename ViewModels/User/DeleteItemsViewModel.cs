using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels;

namespace DrJaw.ViewModels.User
{
    public sealed class DeleteItemsViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private readonly ObservableCollection<DeleteCardItemViewModel> _items = new();

        public ReadOnlyObservableCollection<DeleteCardItemViewModel> Items { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        public DeleteItemsViewModel(IMssqlRepository repo,
                                    IUserSessionService session,
                                    IEnumerable<DGMSSQLItem> sourceItems)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Items = new ReadOnlyObservableCollection<DeleteCardItemViewModel>(_items);

            // наполняем карточки на основе переданных позиций
            foreach (var it in sourceItems)
                _items.Add(new DeleteCardItemViewModel(it, OnCardChanged));

            DeleteCommand = new AsyncRelayCommand(async _ => await OnDeleteAsync(), _ => CanDelete());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

            // неблокирующая подгрузка превью
            _ = LoadThumbnailsAsync();
            RaiseTotals();
        }

        // Итоги
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

        public string TotalSelectedText => $"Выбрано: {TotalQty} шт. на сумму {TotalSum:N2}";

        private void OnCardChanged()
        {
            RaiseTotals();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        private void RaiseTotals()
        {
            TotalQty = _items.Sum(i => i.Quantity);
            TotalSum = _items.Sum(i => i.Quantity * i.Price);
            OnPropertyChanged(nameof(TotalSelectedText));
        }

        private bool CanDelete() => _items.Any(i => i.Quantity > 0);

        private async Task OnDeleteAsync()
        {
            try
            {
                var mart = _session.CurrentMart;
                if (mart is null)
                {
                    ErrorOccurred?.Invoke(this, "Не выбран магазин. Войдите заново и выберите магазин.");
                    return;
                }

                var lines = _items.Where(i => i.Quantity > 0).ToList();
                if (lines.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "Не выбрано ни одной позиции для удаления.");
                    return;
                }

                // собираем конкретные Items.Id (по количеству из карточек)
                var ids = lines.SelectMany(l => l.Ids.Take(l.Quantity)).ToList();
                if (ids.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "Не удалось сформировать список позиций для удаления.");
                    return;
                }

                // один батч-вызов в репозитории (TVP)
                var outcome = await _repo.DeleteItemsByIdsAsync(mart.Id, ids);
                // при желании можно показать outcome.Selected / MovedToCart / Deleted

                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка при удалении: {ex.Message}");
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
                catch
                {
                    // превью не обязательно — тишина
                }
            }
        }

        private static ImageSource BytesToBitmap(byte[] bytes)
        {
            using var ms = new System.IO.MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }

    public sealed class DeleteCardItemViewModel : ViewModelBase
    {
        private readonly Action _onChanged;

        public string Articul { get; }
        public string Metal { get; }
        public decimal Price { get; }

        // все реальные Items.Id из группы (в порядке убывания)
        public IReadOnlyList<int> Ids { get; }

        // для превью
        public int FirstId => (Ids.Count > 0) ? Ids[0] : 0;

        public int MaxQuantity { get; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                var v = Math.Clamp(value, 0, MaxQuantity);
                if (Set(ref _quantity, v))
                {
                    _incCmd.RaiseCanExecuteChanged();
                    _decCmd.RaiseCanExecuteChanged();
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

        private readonly RelayCommand _incCmd;
        private readonly RelayCommand _decCmd;
        public ICommand IncrementQuantityCommand => _incCmd;
        public ICommand DecrementQuantityCommand => _decCmd;

        public DeleteCardItemViewModel(DGMSSQLItem src, Action onChanged)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));
            _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));

            Articul = src.Articul ?? "";
            Metal = src.Metal ?? "";
            Price = src.Price;

            // копируем Ids и гарантируем порядок убывания
            Ids = (src.Ids ?? new List<int>()).OrderByDescending(x => x).ToList();

            MaxQuantity = Math.Max(0, Ids.Count);
            _quantity = 0;

            _incCmd = new RelayCommand(_ => Quantity++, _ => Quantity < MaxQuantity);
            _decCmd = new RelayCommand(_ => Quantity--, _ => Quantity > 0);

            _incCmd.RaiseCanExecuteChanged();
            _decCmd.RaiseCanExecuteChanged();
        }

        public void SetThumbnail(ImageSource? img) => Thumbnail = img;
    }
}

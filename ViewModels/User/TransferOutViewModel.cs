using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using System.Windows.Media;           // ДОБАВЬ
using System.Windows.Media.Imaging;   // ДОБАВЬ
using System.IO;                      // ДОБАВЬ

namespace DrJaw.ViewModels.User
{
    public sealed class TransferOutViewModel : ViewModelBase
    {
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private readonly IReferenceDataService _refData;
        private readonly ObservableCollection<DeleteCardItemViewModel> _items = new();

        public ReadOnlyObservableCollection<DeleteCardItemViewModel> Items { get; }
        public ObservableCollection<MSSQLMart> TargetMarts { get; } = new();

        private MSSQLMart? _selectedTargetMart;
        public MSSQLMart? SelectedTargetMart
        {
            get => _selectedTargetMart;
            set { if (Set(ref _selectedTargetMart, value)) TransferCommand?.RaiseCanExecuteChanged(); }
        }

        public AsyncRelayCommand TransferCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? CloseRequested;
        public event EventHandler<string>? ErrorOccurred;

        public TransferOutViewModel(IMssqlRepository repo,
                                    IUserSessionService session,
                                    IReferenceDataService refData,
                                    IEnumerable<DGMSSQLItem> sourceItems)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _refData = refData ?? throw new ArgumentNullException(nameof(refData));

            Items = new ReadOnlyObservableCollection<DeleteCardItemViewModel>(_items);

            foreach (var it in sourceItems)
                _items.Add(new DeleteCardItemViewModel(it, OnCardChanged));

            _ = LoadThumbnailsAsync(); // ← ДОБАВЬ

            TransferCommand = new AsyncRelayCommand(async _ => await OnTransferAsync(), _ => CanTransfer());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

            // целевой магазин по умолчанию — первый, отличный от текущего
            _refData.Changed += (_, __) => RefreshTargetMarts();
            _session.Changed += (_, __) => RefreshTargetMarts();

            RefreshTargetMarts();
            RaiseTotals();
        }

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

        public string TotalSelectedText => $"К перемещению: {TotalQty} шт. на сумму {TotalSum:N2}";

        private void OnCardChanged()
        {
            RaiseTotals();
            TransferCommand.RaiseCanExecuteChanged();
        }

        private void RaiseTotals()
        {
            TotalQty = _items.Sum(i => i.Quantity);
            TotalSum = _items.Sum(i => i.Quantity * i.Price);
            OnPropertyChanged(nameof(TotalSelectedText));
        }

        private bool CanTransfer()
            => _items.Any(i => i.Quantity > 0)
               && SelectedTargetMart != null
               && SelectedTargetMart.Id != _session.CurrentMart?.Id;

        private async Task OnTransferAsync()
        {
            try
            {
                var fromMart = _session.CurrentMart?.Id
                               ?? throw new InvalidOperationException("Не выбран исходный магазин.");
                var toMart = SelectedTargetMart?.Id
                             ?? throw new InvalidOperationException("Не выбран магазин назначения.");
                if (toMart == fromMart)
                {
                    ErrorOccurred?.Invoke(this, "Магазин назначения совпадает с текущим.");
                    return;
                }

                var lines = _items.Where(i => i.Quantity > 0).ToList();
                if (lines.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "Не выбрано ни одной позиции для перемещения.");
                    return;
                }

                var ids = lines.SelectMany(l => l.Ids.Take(l.Quantity)).ToList();
                if (ids.Count == 0)
                {
                    ErrorOccurred?.Invoke(this, "Не удалось сформировать список позиций для перемещения.");
                    return;
                }

                await _repo.TransferOutItemsByIdsAsync(fromMart, toMart, ids);

                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Ошибка при перемещении: {ex.Message}");
            }
        }
        private void RefreshTargetMarts()
        {
            var currentId = _session.CurrentMart?.Id;

            // запоминаем текущий выбор (если был)
            var prevId = SelectedTargetMart?.Id;

            TargetMarts.Clear();
            foreach (var m in _refData.Marts)
                if (m.Id != currentId)
                    TargetMarts.Add(m);

            // вернуть прежний выбор, если он ещё доступен; иначе — первый
            var newSel = TargetMarts.FirstOrDefault(m => m.Id == prevId) ?? TargetMarts.FirstOrDefault();
            if (!Equals(SelectedTargetMart, newSel))
                SelectedTargetMart = newSel;

            TransferCommand?.RaiseCanExecuteChanged();
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
                    // превью необязательное — молча пропускаем
                }
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
            bmp.Freeze(); // важно для UI-потока
            return bmp;
        }
    }
}

using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;
using DrJaw.ViewModels;

namespace DrJaw.ViewModels.User
{
    public sealed class UserPanelViewModel : ViewModels.ViewModelBase
    {
        private readonly IWindowService _windows;
        private readonly IReferenceDataService _refData;
        private readonly IMssqlRepository _repo;
        private readonly IUserSessionService _session;
        private readonly ICollectionView _itemsView;

        private MSSQLMart? _mart;
        private MSSQLMetal? _metal;
        private decimal _totalWeight;
        private int _totalCount;
        private decimal _totalPrice;
        private bool _isBusy;
        private CancellationTokenSource? _reloadCts;
        private CancellationTokenSource? _filterCts;
        private bool _openImageBusy;
        private DGMSSQLItem? _selectedItem;
        public bool IsFooterEnabled => (_session.CurrentMart?.Id) == (_mart?.Id);                   
        private void RefreshFooterLock()                                                            
        {
            OnPropertyChanged(nameof(IsFooterEnabled));                                             
            RaiseAllCanExec();                                                                      
        }                                                                                           
        private void RaiseAllCanExec()
        {
            (AddItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (TransferOutCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (LomOutCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (LomInCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ReturnCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (TransferInCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (InCartCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (CartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ClearSearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
            OpenImageCommand.RaiseCanExecuteChanged();
        }                                                                                          

        public UserPanelViewModel(IWindowService windows, IReferenceDataService refData, IMssqlRepository repo, IUserSessionService session)
        {
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
            _refData = refData ?? throw new ArgumentNullException(nameof(refData));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            _mart = _session.CurrentMart;
            _metal = null;

            UpdateCommand = new AsyncRelayCommand(async _ => await LoadItemsAsync(),
            _ => CanUpdate());

            AddItemCommand = new RelayCommand(_ => //если не надо блокировать кнопки убрать  && IsFooterEnabled, а у команд убрать , _ => IsFooterEnabled
            {
                var m = _windows.ShowAddItem(repo: _repo, metal: Metal, refData: _refData, session: _session);
                if (m != null)
                {
                    // если металл изменился — сеттер сам вызовет ScheduleReloadAsync()
                    if (Metal == null || m.Id != Metal.Id)
                        Metal = m;
                    else
                        ScheduleReloadAsync(); // металл тот же, но данные поменялись
                }
            }, _ => IsFooterEnabled);

            DeleteItemCommand = new RelayCommand(_ =>
            {
                var picked = PickItemsForAction();
                if (picked.Count == 0)
                {
                    _windows.ShowError("Удаление", "Выберите товар: галочками или текущую строку.");
                    return;
                }

                ShowAndReload(() =>
                {
                    // новый сигнатурный вызов, без out-параметра
                    var ok = _windows.ShowDeleteItem(_repo, picked);
                    return ok;
                });

            }, _ => IsFooterEnabled);

            TransferOutCommand = new RelayCommand(_ =>
            {
                var picked = PickItemsForAction();
                if (picked.Count == 0)
                {
                    _windows.ShowError("Перемещение", "Выберите товар: галочками или текущую строку.");
                    return;
                }

                ShowAndReload(() =>
                {
                    var ok = _windows.ShowTransferOut(_repo, picked);
                    return ok;
                });

            }, _ => IsFooterEnabled);
            LomOutCommand = new RelayCommand(_ => _windows.ShowLomOut(), _ => IsFooterEnabled);
            LomInCommand = new RelayCommand(_ => _windows.ShowLomIn(), _ => IsFooterEnabled);


            InCartCommand = new AsyncRelayCommand(async _ => await PutSelectedToCartAsync(), _ => IsFooterEnabled);
            ReturnCommand = new RelayCommand(
                _ => ShowAndReload(() => _windows.ShowReturn(_repo, _session)), // Func<bool?>-версия
                _ => IsFooterEnabled
            );
            TransferInCommand = new RelayCommand(
                _ => ShowAndReload(() => _windows.ShowTransferIn()),
                _ => IsFooterEnabled && IncomingTransferCount > 0);
            CartCommand = new RelayCommand(
                _ => ShowAndReload(() => _windows.ShowCart()),
                _ => IsFooterEnabled && ReadyToSoldCount > 0);
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty, _ => IsFooterEnabled);

            OpenImageCommand = new AsyncRelayCommand(async p => await OpenImageAsync(p as DGMSSQLItem),
                                         p => p is DGMSSQLItem);

            _itemsView = CollectionViewSource.GetDefaultView(MSSQLItems);
            _itemsView.Filter = ItemsFilter;
            if (_itemsView is INotifyCollectionChanged incc)
                incc.CollectionChanged += (_, __) => RecalcTotals();

            EnsureDefaultSelections();
            _ = UpdateBadgesAsync();
            _ = TryAutoLoadAsync();

            _refData.Changed += async (_, __) =>
            {
                EnsureDefaultSelections();
                await TryAutoLoadAsync();
            };
            _session.Changed += (_, __) => {if ((_session.CurrentMart?.Id) != (_mart?.Id)) RefreshFooterLock(); _ = UpdateBadgesAsync(); }; //если ненадо блокировать кнопки удалить

            _itemsView.Refresh();
            RecalcTotals();
            RefreshFooterLock();//если ненадо блокировать кнопки удалить
        }

        public ReadOnlyObservableCollection<MSSQLMart> Marts => _refData.Marts;
        public ReadOnlyObservableCollection<MSSQLMetal> Metals => _refData.Metals;

        public ObservableCollection<DGMSSQLItem> MSSQLItems { get; } = new();

        public MSSQLMart? Mart
        {
            get => _mart;
            set
            {
                if (Set(ref _mart, value))
                {
                    RaiseCanExec();
                    ScheduleReloadAsync();
                    RefreshFooterLock();//если ненадо блокировать кнопки удалить
                    _ = UpdateBadgesAsync();
                }
            }
        }
        public DGMSSQLItem? SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        public MSSQLMetal? Metal
        {
            get => _metal;
            set
            {
                if (Set(ref _metal, value))
                {
                    RaiseCanExec();
                    ScheduleReloadAsync();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                    ScheduleFilterRefresh();
            }
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set => Set(ref _totalWeight, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => Set(ref _totalCount, value);
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set => Set(ref _totalPrice, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (Set(ref _isBusy, value)) RaiseCanExec(); }
        }

        public AsyncRelayCommand UpdateCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand TransferOutCommand { get; }
        public ICommand LomOutCommand { get; }
        public ICommand LomInCommand { get; }
        public ICommand ReturnCommand { get; }
        public ICommand TransferInCommand { get; }
        public ICommand InCartCommand { get; }
        public ICommand CartCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public AsyncRelayCommand OpenImageCommand { get; }


        private void EnsureDefaultSelections()
        {
            if (Mart is null && Marts.Count > 0) Mart = Marts[0];
            if (Metal is null && Metals.Count > 0) Metal = Metals[0];
        }

        private bool CanUpdate() => !IsBusy && Mart is not null && Metal is not null;

        private void RaiseCanExec()
        {
            UpdateCommand.RaiseCanExecuteChanged();
            (InCartCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            // при желании добавь сюда и другие команды с _ => IsFooterEnabled
        }

        private async Task TryAutoLoadAsync()
        {
            if (IsBusy) return;
            if (Mart is null || Metal is null) return;
            if (MSSQLItems.Count > 0) return;
            await LoadItemsAsync();
        }

        private int _readyToSoldCount;
        public int ReadyToSoldCount
        {
            get => _readyToSoldCount;
            private set
            {
                if (Set(ref _readyToSoldCount, value))
                {
                    OnPropertyChanged(nameof(CartButtonText));
                    (CartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private int _incomingTransferCount;
        public int IncomingTransferCount
        {
            get => _incomingTransferCount;
            private set
            {
                if (Set(ref _incomingTransferCount, value))
                {
                    OnPropertyChanged(nameof(TransferInButtonText));
                    (TransferInCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private async Task UpdateBadgesAsync()
        {
            var martId = _mart?.Id ?? _session.CurrentMart?.Id;
            if (martId is null) { ReadyToSoldCount = 0; IncomingTransferCount = 0; return; }

            try
            {
                var rs = await _repo.GetReadyToSoldCountAsync(martId.Value);
                var ti = await _repo.GetIncomingTransferCountAsync(martId.Value);
                ReadyToSoldCount = rs;
                IncomingTransferCount = ti;
            }
            catch
            {
                // не роняем UI, просто обнулим
                ReadyToSoldCount = 0;
                IncomingTransferCount = 0;
            }
        }

        public string CartButtonText =>
            ReadyToSoldCount > 0 ? $"Корзина ({ReadyToSoldCount})" : "Корзина";
        public string TransferInButtonText =>
    IncomingTransferCount > 0 ? $"Принять перевод ({IncomingTransferCount})" : "Принять перевод";

        private async Task LoadItemsAsync()
        {
            if (Mart is null || Metal is null) return;

            try
            {
                IsBusy = true;

                var items = await _repo.GetItemsAsync(Mart, Metal);

                MSSQLItems.Clear();
                foreach (var it in items)
                    MSSQLItems.Add(it);

                _itemsView.Refresh();
                RecalcTotals();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ScheduleReloadAsync()
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();
            var token = _reloadCts.Token;

            if (Mart is null || Metal is null || IsBusy) return;

            try
            {
                await Task.Delay(200, token);
                if (!token.IsCancellationRequested)
                    await LoadItemsAsync();
            }
            catch (TaskCanceledException) { }
        }

        private async void ScheduleFilterRefresh()
        {
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;

            try
            {
                await Task.Delay(150, token);
                if (token.IsCancellationRequested) return;
                _itemsView.Refresh();
                RecalcTotals();
            }
            catch (TaskCanceledException) { }
        }

        private bool ItemsFilter(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is not DGMSSQLItem it) return false;

            var haystack = $"{it.Type} {it.Articul} {it.Size} {it.Stones} {it.Comment} {it.Manufacturer} {it.Price} {it.Weight}"
                .ToLowerInvariant();

            foreach (var token in SearchText.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (!haystack.Contains(token))
                    return false;

            return true;
        }
        private void RecalcTotals() 
        { 
            decimal w = 0m, p = 0m; 
            int c = 0; 
            foreach (var it in _itemsView.OfType<DGMSSQLItem>()) 
            {
                w += it.Weight * it.ItemCount; 
                c += it.ItemCount; 
                p += it.Price * it.ItemCount; 
            } 
            TotalWeight = w; 
            TotalCount = c; 
            TotalPrice = p; 
        }

        private async Task OpenImageAsync(DGMSSQLItem? item)
        {
            if (item == null) return;
            try
            {
                var id = item.Ids.FirstOrDefault();
                if (id == 0) { _windows.ShowError("Изображение", "Нет элементов в группе."); return; }
                var bytes = await _repo.GetItemImageAsync(id);
                if (bytes == null || bytes.Length == 0)
                {
                    _windows.ShowError("Изображение", "Для выбранного элемента изображение не найдено.");
                    return;
                }

                var bmp = ToBitmapImage(bytes);
                var title = string.IsNullOrWhiteSpace(item.Articul) ? "Изображение" : $"{item.Articul} ({item.Metal})";
                _windows.ShowImage(bmp, title);
            }
            catch (Exception ex)
            {
                _windows.ShowError("Ошибка загрузки", ex.Message);
            }
        }
        private static BitmapImage ToBitmapImage(byte[] bytes)
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
        private void ShowAndReload(Func<bool?> showDialog)
        {
            if (showDialog() == true)
                ScheduleReloadAsync();
            _ = UpdateBadgesAsync();
        }
        private void ShowAndReload(Action showDialog)
        {
            showDialog();
            ScheduleReloadAsync();
            _ = UpdateBadgesAsync(); // если считаешь бейджи
        }
        private IReadOnlyList<DGMSSQLItem> PickItemsForAction()
        {
            var picked = MSSQLItems.Where(x => x.IsSelected).ToList();
            if (picked.Count > 0) return picked;
            if (SelectedItem != null) return new[] { SelectedItem };
            return Array.Empty<DGMSSQLItem>();
        }
        private async Task PutSelectedToCartAsync()
        {
            // 1) сначала пробуем отмеченные галочками
            var groups = MSSQLItems.Where(x => x.IsSelected).ToList();

            // 2) если галочек нет — fallback на текущую выделенную строку
            if (groups.Count == 0 && SelectedItem != null)
                groups = new List<DGMSSQLItem> { SelectedItem };

            // 3) если всё ещё пусто — ругаемся
            if (groups.Count == 0)
            {
                _windows.ShowError("В корзину", "Отметьте позиции галочками или выделите строку.");
                return;
            }

            // по одному Id из каждой группы
            var ids = groups
                .Select(g => g.Ids.FirstOrDefault())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                _windows.ShowError("В корзину", "Не удалось получить идентификаторы выбранных позиций.");
                return;
            }

            try
            {
                IsBusy = true;
                await _repo.MarkReadyToSoldAsync(ids);
                await LoadItemsAsync(); // обновляем список
                _ = UpdateBadgesAsync();
            }
            catch (Exception ex)
            {
                _windows.ShowError("В корзину", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

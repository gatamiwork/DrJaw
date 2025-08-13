using DrJaw.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DrJaw.Views.Common
{
    public partial class Orders : Window
    {
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private CancellationTokenSource? _cts;

        private List<DGMSSQLOrders> _allOrders = new();
        private List<MSSQLOrderTotals> _periodTotals = new(); // КЕШ агрегаций за период

        private bool _isInitializing;
        private int _reloadRequested; // 0/1

        public Orders()
        {
            InitializeComponent();

            Loaded += Orders_Loaded;

            // период → перегружаем из БД (заказы + агрегаты)
            DateFrom.SelectedDateChanged += DateChanged_Reload;
            DateTo.SelectedDateChanged += DateChanged_Reload;

            // фильтры → только локальная фильтрация
            ComboBoxMart.SelectionChanged += FiltersChanged_Local;
            ComboBoxUser.SelectionChanged += FiltersChanged_Local;

            DataGridOrders.SelectionChanged += DataGridOrders_SelectionChanged;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // отписки
            Loaded -= Orders_Loaded;
            DateFrom.SelectedDateChanged -= DateChanged_Reload;
            DateTo.SelectedDateChanged -= DateChanged_Reload;
            ComboBoxMart.SelectionChanged -= FiltersChanged_Local;
            ComboBoxUser.SelectionChanged -= FiltersChanged_Local;
            DataGridOrders.SelectionChanged -= DataGridOrders_SelectionChanged;

            // отпускаем источники
            DataGridItem.ItemsSource = null;
            DataGridOrders.ItemsSource = null;
        }

        private async void Orders_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            try
            {
                // временно отписываемся, чтобы не стреляли DateChanged_Reload во время установки
                DateFrom.SelectedDateChanged -= DateChanged_Reload;
                DateTo.SelectedDateChanged -= DateChanged_Reload;
                ComboBoxMart.SelectionChanged -= FiltersChanged_Local;
                ComboBoxUser.SelectionChanged -= FiltersChanged_Local;

                DateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
                DateTo.SelectedDate = DateTime.Today;

                var martsWithAll = new List<MSSQLMart> { new() { Id = 0, Name = "Все магазины" } };
                martsWithAll.AddRange(Storage.Marts ?? new List<MSSQLMart>());
                ComboBoxMart.ItemsSource = martsWithAll;
                ComboBoxMart.SelectedIndex = 0;

                var usersWithAll = new List<MSSQLUser> { new() { Id = 0, Name = "Все пользователи" } };
                usersWithAll.AddRange(Storage.Users ?? new List<MSSQLUser>());
                ComboBoxUser.ItemsSource = usersWithAll;
                ComboBoxUser.SelectedIndex = 0;
            }
            finally
            {
                // возвращаем события
                DateFrom.SelectedDateChanged += DateChanged_Reload;
                DateTo.SelectedDateChanged += DateChanged_Reload;
                ComboBoxMart.SelectionChanged += FiltersChanged_Local;
                ComboBoxUser.SelectionChanged += FiltersChanged_Local;
                _isInitializing = false;
            }

            await ReloadForPeriodAsync(); // первая полноценная загрузка
        }

        private async void DateChanged_Reload(object? sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _isInitializing) return;
            await ReloadForPeriodAsync();
        }

        private async Task ReloadForPeriodAsync()
        {
            // если занято — попросим повтор после текущей
            if (!await _loadLock.WaitAsync(0))
            {
                Interlocked.Exchange(ref _reloadRequested, 1);
                return;
            }

            var newCts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref _cts, newCts);
            old?.Cancel();
            old?.Dispose();
            var ct = newCts.Token;

            try
            {
                IsEnabled = false;

                var from = (DateFrom.SelectedDate ?? DateTime.Today.AddMonths(-1)).Date;
                var to = (DateTo.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

                var ordersTask = Storage.Repo.LoadOrder(from, to);
                var totalsTask = Storage.Repo.LoadOrderTotals(from, to);
                await Task.WhenAll(ordersTask, totalsTask);

                if (ct.IsCancellationRequested) return;

                _allOrders = ordersTask.Result ?? new();
                _periodTotals = totalsTask.Result ?? new();

                ApplyFilters(); // локальная фильтрация + низ из кеша
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allOrders = new();
                _periodTotals = new();
                DataGridOrders.ItemsSource = null;
                DataGridItem.ItemsSource = null;
                ResetBottomTotals();
            }
            finally
            {
                IsEnabled = true;
                _loadLock.Release();

                // если пока мы грузились пришёл ещё один запрос — повторим ещё раз
                if (Interlocked.Exchange(ref _reloadRequested, 0) == 1)
                    _ = ReloadForPeriodAsync();
            }
        }

        private void FiltersChanged_Local(object? sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilters(); // только локально, БД НЕ трогаем
        }

        private static int GetSelectedId(ComboBox cb) => cb?.SelectedValue is int v ? v : 0;

        private void ApplyFilters()
        {
            int martId = GetSelectedId(ComboBoxMart);
            int userId = GetSelectedId(ComboBoxUser);

            var filtered = _allOrders.Where(o =>
                (martId == 0 || o.MartId == martId) &&
                (userId == 0 || o.UserId == userId)).ToList();

            DataGridOrders.ItemsSource = filtered;

            if (filtered.Count > 0)
                DataGridOrders.SelectedIndex = 0;
            else
            {
                DataGridItem.ItemsSource = null;
            }

            // НИЗ: считаем из КЕША _periodTotals по выбранным фильтрам
            UpdateBottomTotalsFromPeriodTotals(martId, userId);
        }

        private static int ExtractCartId(object row)
        {
            var t = row.GetType();
            var p = t.GetProperty("CartId") ?? t.GetProperty("Id");
            var v = p?.GetValue(row);
            if (v == null) return 0;
            try { return Convert.ToInt32(v); } catch { return 0; }
        }

        private async void DataGridOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is null)
            {
                // если заказа не выбрано — низ показывает агрегаты по текущим фильтрам (из КЕША)
                UpdateBottomTotalsFromPeriodTotals(GetSelectedId(ComboBoxMart), GetSelectedId(ComboBoxUser));
                DataGridItem.ItemsSource = null;
                return;
            }

            try
            {
                int cartId = ExtractCartId(DataGridOrders.SelectedItem);
                if (cartId == 0) return;

                var items = await Storage.Repo.LoadOrderItems(cartId);
                DataGridItem.ItemsSource = items;

                int martId = GetSelectedId(ComboBoxMart);
                int userId = GetSelectedId(ComboBoxUser);
                // Низ по выбранному заказу (как раньше)
                UpdateBottomTotalsFromPeriodTotals(martId, userId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки позиций заказа: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBottomTotalsFromPeriodTotals(int martId, int userId)
        {
            if (_periodTotals == null || _periodTotals.Count == 0)
            {
                ResetBottomTotals();
                return;
            }

            var filtered = _periodTotals.Where(t =>
                (martId == 0 || t.MartId == martId) &&
                (userId == 0 || t.UserId == userId));

            decimal gWeight = 0m, gPrice = 0m; int gCount = 0;
            decimal sWeight = 0m, sPrice = 0m; int sCount = 0;

            foreach (var t in filtered)
            {
                var metal = t.Metal?.Trim().ToLowerInvariant() ?? "";
                bool isGold = metal.Contains("зол") || metal.Contains("gold");
                bool isSilver = metal.Contains("сереб") || metal.Contains("silver");

                if (isGold)
                {
                    gCount += t.ItemCount;
                    gWeight += t.TotalWeight;
                    gPrice += t.TotalPrice;
                }
                else if (isSilver)
                {
                    sCount += t.ItemCount;
                    sWeight += t.TotalWeight;
                    sPrice += t.TotalPrice;
                }
            }

            labelTotalGWeight.Content = $"Общий вес золота: {gWeight:F2}";
            labelTotalGCount.Content = $"Общее количество золота: {gCount}";
            labelTotalGPrice.Content = $"Общая стоимость золота: {gPrice:F2}";

            labelTotalSWeight.Content = $"Общий вес серебра: {sWeight:F2}";
            labelTotalSCount.Content = $"Общее количество серебра: {sCount}";
            labelTotalSPrice.Content = $"Общая стоимость серебра: {sPrice:F2}";

            var ten = Math.Round((gPrice + sPrice) * 0.10m, 2, MidpointRounding.AwayFromZero);
            label10Percent.Content = $"10% стоимости проданных изделий: {ten:F2}";
        }

        private void ResetBottomTotals()
        {
            labelTotalGWeight.Content = "Общий вес золота: 0.00";
            labelTotalGCount.Content = "Общее количество золота: 0";
            labelTotalGPrice.Content = "Общая стоимость золота: 0.00";

            labelTotalSWeight.Content = "Общий вес серебра: 0.00";
            labelTotalSCount.Content = "Общее количество серебра: 0";
            labelTotalSPrice.Content = "Общая стоимость серебра: 0.00";

            label10Percent.Content = "10% стоимости проданных изделий: 0.00";
        }

        // helpers
        private static string GetString(Type t, object o, string name)
            => t.GetProperty(name)?.GetValue(o)?.ToString() ?? "";

        private static int GetInt(Type t, object o, string name)
        {
            var v = t.GetProperty(name)?.GetValue(o);
            if (v == null) return 0;
            try { return Convert.ToInt32(v); } catch { return 0; }
        }

        private static decimal GetDecimal(Type t, object o, string name)
        {
            var v = t.GetProperty(name)?.GetValue(o);
            if (v == null) return 0m;
            try { return Convert.ToDecimal(v); } catch { return 0m; }
        }
    }
}

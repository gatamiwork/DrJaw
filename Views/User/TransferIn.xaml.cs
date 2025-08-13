using DrJaw.Models;
using DrJaw.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DrJaw.Views.User
{
    public partial class TransferIn : Window
    {
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private CancellationTokenSource? _loadCts;
        public TransferIn()
        {
            InitializeComponent();
            Loaded += TransferIn_Loaded;
        }
        private async void TransferIn_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }
        private async Task LoadDataAsync()
        {
            // если грузимся — не стартуем вторую; просто отменим текущую и перезапустим после
            if (!await _loadLock.WaitAsync(0))
            {
                _loadCts?.Cancel();
                return;
            }
            var newCts = new CancellationTokenSource();
            var oldCts = Interlocked.Exchange(ref _loadCts, newCts);
            oldCts?.Cancel();
            oldCts?.Dispose();
            var ct = newCts.Token;

            try
            {
                IsEnabled = false;

                if (Storage.CurrentMart?.Id is not int martId)
                {
                    DataGridTransfer.ItemsSource = null;
                    TotalWeight.Content = "Общий вес: 0.00";
                    TotalCount.Content = "Количество: 0";
                    return;
                }

                // если есть перегрузка с CancellationToken — лучше использовать её
                var items = await Storage.Repo.LoadTransferItems(martId /*, ct*/);

                if (ct.IsCancellationRequested) return;

                DataGridTransfer.ItemsSource = items;
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
                _loadLock.Release();
            }
        }
        private void UpdateTotals()
        {
            var list = (DataGridTransfer.ItemsSource as IEnumerable<DGMSSQLTransferItem>)?.ToList()
                       ?? new List<DGMSSQLTransferItem>();

            decimal totalWeight = list.Sum(x => x.Weight);
            int totalCount = list.Count;

            TotalWeight.Content = $"Общий вес: {totalWeight:F2}";
            TotalCount.Content = $"Количество: {totalCount}";
        }
        // ✅ важно: RoutedEventArgs, не EventArgs
        private async void buttonCancelTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTransfer.SelectedItem is not DGMSSQLTransferItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var confirm = MessageBox.Show("Отменить входящий трансфер (вернуть отправителю)?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            SetButtonsEnabled(false);
            try
            {
                // ⚠️ стандартизируй репозиторий: сделай CancelTransferAsync(selectedItem.Id)
                // Временно оставляю твой вызов:
                await Storage.Repo.TransferItem(null, selectedItem.Id);

                EventBus.Publish("ItemsChanged");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отмене: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }
        private async void buttonTranster_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTransfer.SelectedItem is not DGMSSQLTransferItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Storage.CurrentMart?.Id is not int currentMartId)
            {
                MessageBox.Show("Магазин не выбран.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetButtonsEnabled(false);
            try
            {
                // ⚠️ лучше иметь явный метод AcceptTransferAsync(selectedItem.Id, currentMartId)
                await Storage.Repo.TransferItem(null, selectedItem.Id, currentMartId);

                EventBus.Publish("ItemsChanged");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при приёме товара: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }
        // Даблклик по строке = принять
        private async void DataGridTransfer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridTransfer.SelectedItem is DGMSSQLTransferItem _)
                buttonTranster_Click(sender, e);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            buttonTranster.IsEnabled = enabled;
            buttonCancelTransfer.IsEnabled = enabled;
        }

        // Если где-то меняется ItemsSource программно — можно пересчитывать итоги
        private void DataGridTransfer_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            UpdateTotals();
        }
    }
}

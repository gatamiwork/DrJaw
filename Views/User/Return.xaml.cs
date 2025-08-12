using DrJaw.Models;
using DrJaw.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DrJaw.Views.User
{
    public partial class Return : Window
    {
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private CancellationTokenSource? _loadCts;
        private bool _isInitializing;
        private int _reloadRequested; // 0/1
        public Return()
        {
            InitializeComponent();
            Loaded += Return_Loaded;
        }
        private async void Return_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            try
            {
                DatePickerReturn.SelectedDateChanged -= DatePickerReturn_SelectedDateChanged;
                if (DatePickerReturn.SelectedDate is null)
                    DatePickerReturn.SelectedDate = DateTime.Today;
            }
            finally
            {
                DatePickerReturn.SelectedDateChanged += DatePickerReturn_SelectedDateChanged;
                _isInitializing = false;
            }

            await LoadDataAsync();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;

            DataGridReturnItems.ItemsSource = null;
            DataGridReturnItems.SelectedItem = null;
        }
        private async void DatePickerReturn_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // если занято — попросим повтор после текущей
            if (!await _loadLock.WaitAsync(0))
            {
                Interlocked.Exchange(ref _reloadRequested, 1);
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
                    DataGridReturnItems.ItemsSource = null;
                    TextCount.Text = "Магазин не выбран";
                    ButtonReturn.IsEnabled = false;
                    return;
                }

                var dateStart = (DatePickerReturn.SelectedDate ?? DateTime.Today).Date;
                var dateEnd = dateStart.AddDays(1).AddTicks(-1);

                // если можешь — добавь перегрузку репозитория с CancellationToken
                var data = await Storage.Repo.LoadReturnCartItems(martId, dateStart, dateEnd /*, ct*/);

                if (ct.IsCancellationRequested) return; // актуальный запрос сменился — не трогаем UI

                DataGridReturnItems.ItemsSource = data;
                DataGridReturnItems.SelectedItem = null;
                TextCount.Text = $"Найдено: {data?.Count ?? 0}";
                ButtonReturn.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
                _loadLock.Release();

                // если пока мы грузились попросили повтор — сделаем ещё один заход
                if (Interlocked.Exchange(ref _reloadRequested, 0) == 1)
                    _ = LoadDataAsync();
            }
        }
        private void DataGridReturnItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonReturn.IsEnabled = DataGridReturnItems.SelectedItem is MSSQLReturnCartItem;
        }
        private async void ButtonReturn_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridReturnItems.SelectedItem is not MSSQLReturnCartItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Сделать возврат изделия?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            ButtonReturn.IsEnabled = false;
            try
            {
                await Storage.Repo.ReturnCartAndItemAsync(selectedItem.Id);

                // сообщаем и перезагружаем
                EventBus.Publish("ItemsChanged");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ButtonReturn.IsEnabled = true;
            }
        }
    }
}

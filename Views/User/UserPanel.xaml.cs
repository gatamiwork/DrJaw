using DrJaw.Models;
using DrJaw.Utils;
using DrJaw.Views.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DrJaw.Views.User
{
    public partial class UserPanel : UserControl, ICleanup, IRefreshable
    {
        private readonly ObservableCollection<DGMSSQLItem> _items = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private bool _isInitializing;

        // подписка на шину (если используешь «новый» EventBus с IDisposable – лучше хранить IDisposable)
        private readonly Action _itemsChangedHandler;

        public UserPanel()
        {
            InitializeComponent();
            DataGridItems.ItemsSource = _items;

            Loaded += async (_, __) =>
            {
                _isInitializing = true;
                try
                {
                    comboBoxMart.ItemsSource = Storage.Marts;
                    comboBoxMetal.ItemsSource = Storage.Metals;

                    comboBoxMart.SelectionChanged -= comboBoxMart_SelectionChanged;
                    comboBoxMetal.SelectionChanged -= comboBoxMetal_SelectionChanged;

                    if (Storage.CurrentMart != null)
                        comboBoxMart.SelectedValue = Storage.CurrentMart.Id;
                    else
                        comboBoxMart.SelectedIndex = -1;

                    if (Storage.CurrentMetal != null)
                        comboBoxMetal.SelectedValue = Storage.CurrentMetal.Id;
                    else
                        comboBoxMetal.SelectedIndex = -1;
                }
                finally
                {
                    comboBoxMart.SelectionChanged += comboBoxMart_SelectionChanged;
                    comboBoxMetal.SelectionChanged += comboBoxMetal_SelectionChanged;
                    _isInitializing = false;
                }

                await RefreshItemsAsync();
            };

            Unloaded += (_, __) => Cleanup();

            _itemsChangedHandler = () =>
            {
                // Безопасно маршаллим в UI-поток и не теряем исключения
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    try { await RefreshItemsAsync(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error); }
                });
            };
            EventBus.Subscribe("ItemsChanged", _itemsChangedHandler);
        }

        public void Refresh() => _ = RefreshItemsAsync();

        public void Cleanup()
        {
            // отписки
            EventBus.Unsubscribe("ItemsChanged", _itemsChangedHandler);

            // очистка данных и ссылок
            _items.Clear();
            DataGridItems.ItemsSource = null;
            comboBoxMart.ItemsSource = null;
            comboBoxMetal.ItemsSource = null;
        }

        private async Task RefreshItemsAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;
            try
            {
                IsEnabled = false;

                var loaded = await Storage.Repo.LoadItems(Storage.CurrentMart, Storage.CurrentMetal);
                var transferCount = await Storage.Repo.ItemTransferCount(Storage.CurrentMart);
                var cartCount = await Storage.Repo.ItemCartCount(Storage.CurrentMart);

                int totalCount = 0;
                decimal totalWeight = 0, totalPrice = 0;

                _items.Clear();
                foreach (var item in loaded)
                {
                    totalCount += item.ItemCount;
                    totalWeight += item.Weight * item.ItemCount;
                    totalPrice += item.Price * item.ItemCount;
                    _items.Add(item);
                }

                labelTotalCount.Content = $"Общее количество: {totalCount}";
                labelTotalWeight.Content = $"Общий вес: {totalWeight:F2}";
                labelTotalPrice.Content = $"Общая сумма: {totalPrice:F2}";

                UpdateActionButtons(transferCount, cartCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
                _refreshLock.Release();
            }
        }
        private void UpdateActionButtons(int transferCount, int cartCount)
        {
            // Отгрузка / Возврат(N) по трансферу
            if (transferCount > 0)
            {
                ButtonTransferIn.IsEnabled = true;
                ButtonTransferIn.Content = $"Отгрузка({transferCount})";
            }
            else
            {
                ButtonTransferIn.IsEnabled = false;
                ButtonTransferIn.Content = "Отгрузка";
            }

            // Продажа / Возврат(N) по корзине
            if (cartCount > 0)
            {
                ButtonCart.IsEnabled = true;
                ButtonCart.Content = $"Продажа({cartCount})";
            }
            else
            {
                ButtonCart.IsEnabled = false;
                ButtonCart.Content = "Продажа";
            }
        }


        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            await RefreshItemsAsync();
            RefreshButton.IsEnabled = true;
        }

        private async void comboBoxMart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (comboBoxMart.SelectedItem is MSSQLMart selectedMart)
            {
                Storage.CurrentMart = selectedMart;
                await RefreshItemsAsync();
            }
        }

        private async void comboBoxMetal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (comboBoxMetal.SelectedItem is MSSQLMetal selectedMetal)
            {
                Storage.CurrentMetal = selectedMetal;
                await RefreshItemsAsync();
            }
        }

        private void ButtonAddItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.AddItem { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }

        private async void ButtonDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridItems.SelectedItem is not DGMSSQLItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Удалить изделие?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (!await Storage.Repo.DeleteItem(selectedItem.mid))
                {
                    MessageBox.Show("Не удалось удалить товар.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                EventBus.Publish("ItemsChanged"); // триггерим обновление
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении товара: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonTransferOut_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridItems.SelectedItem is not DGMSSQLItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие для отправки.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var window = new DrJaw.Views.User.TransferOut(selectedItem) { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }

        private void LomOut_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.LomOut { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }

        private void LomIn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.LomIn { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }

        private async void buttonInCart(object sender, RoutedEventArgs e)
        {
            if (DataGridItems.SelectedItem is not DGMSSQLItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие для отправки.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!await Storage.Repo.SetReadyToSold(selectedItem.mid, true))
            {
                MessageBox.Show("Не удалось добавить в корзину.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            EventBus.Publish("ItemsChanged");
        }

        private void buttonCart(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.Cart { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            EventBus.Publish("ItemsChanged");
        }

        private void buttonTransferIn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.TransferIn { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            EventBus.Publish("ItemsChanged");
        }

        private void buttonReturn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.Return { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            EventBus.Publish("ItemsChanged");
        }
    }
}

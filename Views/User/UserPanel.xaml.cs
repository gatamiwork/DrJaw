using DrJaw.Models;
using DrJaw.Utils;
using DrJaw.Views.Common;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DrJaw.Views.User
{
    public partial class UserPanel : UserControl, ISwitchUserPanel
    {
        private ObservableCollection<MSSQLItem> _items = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public UserPanel()
        {
            InitializeComponent();
            Loaded += UserPanel_Loaded;
            EventBus.Subscribe("ItemsChanged", async () => await RefreshItemsAsync());
        }
        private async void UserPanel_Loaded(object sender, RoutedEventArgs e)
        {
            comboBoxMart.ItemsSource = Storage.Marts;
            comboBoxMetal.ItemsSource = Storage.Metals;

            if (Storage.CurrentMart != null)
                comboBoxMart.SelectedValue = Storage.CurrentMart.Id;
            else
                comboBoxMart.SelectedIndex = -1;

            if (Storage.CurrentMetal != null)
                comboBoxMetal.SelectedValue = Storage.CurrentMetal.Id;
            else
                comboBoxMetal.SelectedIndex = -1;

            DataGridItems.ItemsSource = _items;

            var loaded = await Storage.Repo.LoadItems(Storage.CurrentMart, Storage.CurrentMetal);
            _items.Clear();
            foreach (var item in loaded)
                _items.Add(item);
        }
        public void CleanupBeforeUnload()
        {
            // Очистка UI и данных
            Loaded -= UserPanel_Loaded;
            DataGridItems.ItemsSource = null;
            _items.Clear();
            comboBoxMart.ItemsSource = null;
            comboBoxMetal.ItemsSource = null;
        }
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            await RefreshItemsAsync();
            RefreshButton.IsEnabled = true;

        }
        private async void comboBoxMart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxMart.SelectedItem is MSSQLMart selectedMart)
            {
                Storage.CurrentMart = selectedMart;
                await RefreshItemsAsync();
            }
        }
        private async void comboBoxMetal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxMetal.SelectedItem is MSSQLMetal selectedMetal)
            {
                Storage.CurrentMetal = selectedMetal;
                await RefreshItemsAsync();
            }
        }
        private async Task RefreshItemsAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;

            try
            {
                int totalCount = 0;
                decimal totalWeight = 0;
                decimal totalPrice = 0;
                IsEnabled = false;

                var loaded = await Storage.Repo.LoadItems(Storage.CurrentMart, Storage.CurrentMetal);

                _items.Clear();
                foreach (var item in loaded)
                {
                    totalCount += item.ItemCount;
                    totalWeight += item.Weight * item.ItemCount;
                    totalPrice += item.Price * item.ItemCount;
                    _items.Add(item);
                }

                if (DataGridItems.ItemsSource == null)
                    DataGridItems.ItemsSource = _items;

                labelTotalCount.Content = $"Общее количество : {totalCount}";
                labelTotalWeight.Content = $"Общий вес: {totalWeight:F2}";
                labelTotalPrice.Content = $"Общая сумма: {totalPrice:F2}";
            }
            finally
            {
                IsEnabled = true;
                _refreshLock.Release();
            }
        }

        private void ButtonAddItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.AddItem();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        private async void ButtonDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridItems.SelectedItem is not MSSQLItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Удалить изделие?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (!await Storage.Repo.DeleteItem(selectedItem.mid))
                {
                    MessageBox.Show("Не удалось удалить товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DrJaw.Utils.EventBus.Publish("ItemsChanged"); // 🔁 обновим UserPanel, если надо
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении товара: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void buttonTransferOut_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridItems.SelectedItem is not MSSQLItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие для отправки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var window = new DrJaw.Views.User.TransferOut(selectedItem);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        private void LomOut_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.LomOut();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        private void LomIn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.LomIn();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }
        private async void buttonInCart(object sender, RoutedEventArgs e)
        {
            if (DataGridItems.SelectedItem is not MSSQLItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие для отправки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!await Storage.Repo.SetReadyToSold(selectedItem.mid, true))
            {
                MessageBox.Show("Не удалось добавить в корзину.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DrJaw.Utils.EventBus.Publish("ItemsChanged");
        }
        private void buttonCart(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.Cart();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            DrJaw.Utils.EventBus.Publish("ItemsChanged");
        }
        private void buttonTransferIn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.TransferIn();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            DrJaw.Utils.EventBus.Publish("ItemsChanged");
        }
        private void buttonReturn_Click(object sender, RoutedEventArgs e)
        {
            var window = new DrJaw.Views.User.Return();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            DrJaw.Utils.EventBus.Publish("ItemsChanged");
        }  
    }
}
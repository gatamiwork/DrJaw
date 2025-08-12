using DrJaw.Models;
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
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace DrJaw.Views.User
{
    /// <summary>
    /// Логика взаимодействия для Cart.xaml
    /// </summary>
    public partial class Cart : Window
    {
        public Cart()
        {
            InitializeComponent();
            Loaded += Cart_Loaded;
            CartDataGrid.CellEditEnding += CartDataGrid_CellEditEnding;
        }
        private void CartDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Подождать, пока значение реально обновится
            Dispatcher.BeginInvoke(new Action(UpdateTotalSum), System.Windows.Threading.DispatcherPriority.Background);
        }
        private int GetGlobalBonusPercent()
        {
            if (ComboBoxTotalBonus?.SelectedItem is ComboBoxItem it &&
                int.TryParse(it.Tag?.ToString(), out int p))
                return Math.Clamp(p, 0, 100);

            // фоллбэк: если Tag не задан, пробуем вытащить из текста "5%"
            var txt = (ComboBoxTotalBonus?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "0";
            return int.TryParse(txt.Replace("%", ""), out var p2) ? Math.Clamp(p2, 0, 100) : 0;
        }
        private void UpdateTotalSum()
        {
            if (!IsLoaded || LabelTotalPrice == null) return;

            if (CartDataGrid.ItemsSource is not IEnumerable<MSSQLReadyToSold> items)
            {
                LabelTotalPrice.Content = "Итого: 0.00 ₸";
                return;
            }

            int globalBonus = GetGlobalBonusPercent();

            decimal subtotal = 0m;
            foreach (var it in items)
                subtotal += it.Price * (1 - it.Bonus / 100m);

            decimal total = Math.Round(subtotal * (1 - globalBonus / 100m), 2, MidpointRounding.AwayFromZero);
            LabelTotalPrice.Content = $"Итого: {total:F2} ₸";
        }
        private void ComboBoxTotalBonus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateTotalSum();
        }
        private async void Cart_Loaded(object sender, RoutedEventArgs e)
        {
            var items = await Storage.Repo.LoadItemsInCart(Storage.CurrentMart);
            var paymentTypes = await Storage.Repo.LoadPaymentTypes();

            CartDataGrid.ItemsSource = items;
            ComboBoxPaymentTypes.ItemsSource = paymentTypes;
            ComboBoxPaymentTypes.SelectedIndex = 0;
            UpdateTotalSum();
        }
        private async void buttonRemoveCart(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Отменить заказ?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // Получаем список всех изделий из таблицы
            var items = CartDataGrid.ItemsSource as IEnumerable<MSSQLReadyToSold>;
            if (items == null)
            {
                MessageBox.Show("Корзина пуста.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ids = items.Select(x => x.Id).ToList();
            if (ids.Count == 0)
            {
                MessageBox.Show("Нет элементов для отмены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Устанавливаем ReadyToSold = false
            bool success = await Storage.Repo.SetReadyToSold(ids, false);
            if (success)
            {
                Close();
            }
            else
            {
                MessageBox.Show("Не удалось отменить заказ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void buttonRemoveItemInCart(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MSSQLReadyToSold item)
            {
                if (MessageBox.Show("Убрать изделие?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                if (await Storage.Repo.SetReadyToSold(item.Id, false))
                {
                    if (CartDataGrid.ItemsSource is IList<MSSQLReadyToSold> list)
                    {
                        list.Remove(item);
                        CartDataGrid.Items.Refresh();
                        UpdateTotalSum();
                        if (list.Count == 0) Close();
                    }
                }
                else
                {
                    MessageBox.Show("Не удалось убрать изделие.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            if (ComboBoxPaymentTypes.SelectedIndex < 0)
            {
                MessageBox.Show("Выберите метод оплаты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CartDataGrid.ItemsSource is not IEnumerable<MSSQLReadyToSold> itemsEnum)
            {
                MessageBox.Show("Корзина пуста.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var items = itemsEnum.ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("Корзина пуста.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int globalBonus = GetGlobalBonusPercent();

            decimal subtotal = 0m;
            foreach (var it in items)
                subtotal += it.Price * (1 - it.Bonus / 100m);

            decimal discountedTotal = Math.Round(subtotal * (1 - globalBonus / 100m), 2, MidpointRounding.AwayFromZero);

            int paymentTypeId = (ComboBoxPaymentTypes.SelectedItem as MSSQLPaymentType)?.Id ?? 0;
            if (paymentTypeId == 0)
            {
                MessageBox.Show("Выберите метод оплаты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int cartId = await Storage.Repo.CreateCart(
                    totalSum: discountedTotal,
                    martId: Storage.CurrentMart?.Id ?? 0,
                    userId: Storage.CurrentUser?.Id ?? 0,
                    paymentTypeId: paymentTypeId,
                    bonus: globalBonus
                );

                if (cartId == 0)
                {
                    MessageBox.Show("Ошибка при создании корзины.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (var it in items)
                    await Storage.Repo.CreateCartItem(cartId, it.Id, it.Bonus);

                DrJaw.Utils.EventBus.Publish("ItemsChanged");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка оформления: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

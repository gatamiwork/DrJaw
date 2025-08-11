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
        private void UpdateTotalSum()
        {
            if (CartDataGrid.ItemsSource is not IEnumerable<MSSQLReadyToSold> items)
                return;

            int globalBonus = 0;
            if (ComboBoxTotalBonus.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Tag?.ToString(), out int parsedBonus))
            {
                globalBonus = parsedBonus;
            }

            decimal total = items.Sum(item =>
            {
                decimal discounted = item.Price * (1 - item.Bonus / 100m);
                discounted *= (1 - globalBonus / 100m);
                return discounted;
            });

            LabelTotalPrice.Content = $"Итого: {total:F2} ₸";
        }
        private void ComboBoxTotalBonus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
                var result = MessageBox.Show("Убрать изделие?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (await Storage.Repo.SetReadyToSold(item.Id, false))
                    {
                        // Удаляем из ItemsSource
                        if (CartDataGrid.ItemsSource is IList<MSSQLReadyToSold> list)
                        {
                            list.Remove(item);
                            CartDataGrid.Items.Refresh();

                            if (list.Count == 0)
                                Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось убрать изделие.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
            // Получаем общий бонус из ComboBoxTotalBonus (например, "5%")
            string bonusStr = (ComboBoxTotalBonus.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Replace("%", "") ?? "0";
            if (!decimal.TryParse(bonusStr, out decimal totalBonus))
                totalBonus = 0;

            // Считаем сумму по всем элементам в таблице
            decimal totalPrice = 0;
            foreach (var item in CartDataGrid.ItemsSource.Cast<MSSQLReadyToSold>())
            {
                totalPrice += item.TotalPrice;
            }

            decimal discountedTotal = totalPrice * (1 - totalBonus / 100m);

            int paymentTypeId = (ComboBoxPaymentTypes.SelectedItem as MSSQLPaymentType)?.Id ?? 0;

            int cartId = await Storage.Repo.CreateCart(
                    totalSum: discountedTotal,
                    martId: Storage.CurrentMart?.Id ?? 0,
                    userId: Storage.CurrentUser?.Id ?? 0,
                    paymentTypeId: paymentTypeId,
                    bonus: (int)totalBonus
                );

            if (cartId == 0)
            {
                MessageBox.Show("Ошибка при создании корзины.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var item in CartDataGrid.ItemsSource.Cast<MSSQLReadyToSold>())
            {
                await Storage.Repo.CreateCartItem(cartId, item.Id, item.Bonus);
            }

            this.Close();
        }
    }
}

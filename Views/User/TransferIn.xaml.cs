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

namespace DrJaw.Views.User
{
    /// <summary>
    /// Логика взаимодействия для TransferIn.xaml
    /// </summary>
    public partial class TransferIn : Window
    {
        public TransferIn()
        {
            InitializeComponent();
            Loaded += TransferIn_Loaded;
        }
        private void CalculateTransferTotals()
        {
            decimal totalWeight = 0;
            int totalCount = 0;

            foreach (var item in DataGridTransfer.Items)
            {
                if (item is MSSQLTransferItem transferItem)
                {
                    totalWeight += transferItem.Weight;
                    totalCount++;
                }
            }

            TotalWeight.Content = $"Общий вес: {totalWeight:F2}";
            TotalCount.Content = $"Количество: {totalCount}";
        }

        private async void TransferIn_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }
        private async Task LoadData()
        {
            var items = await Storage.Repo.LoadTransferItems(Storage.CurrentMart.Id);

            DataGridTransfer.ItemsSource = items;
            CalculateTransferTotals();
        }
        private async void buttonCancelTransfer_Click(object sender, EventArgs e)
        {
            if (DataGridTransfer.SelectedItem is not MSSQLTransferItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Вернуть изделие?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await Storage.Repo.TransferItem(null, selectedItem.Id);
                await LoadData();
            }
        }
        private async void buttonTranster_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTransfer.SelectedItem is not MSSQLTransferItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var id = selectedItem.Id;
                await Storage.Repo.TransferItem(null, id, Storage.CurrentMart?.Id);
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

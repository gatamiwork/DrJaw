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
    /// Логика взаимодействия для TransferOut.xaml
    /// </summary>
    public partial class TransferOut : Window
    {
        private readonly MSSQLItem _itemToTransfer;

        public TransferOut(MSSQLItem item)
        {
            InitializeComponent();
            _itemToTransfer = item;
            Loaded += TransferOut_Loaded;
        }
        private void TransferOut_Loaded(object sender, RoutedEventArgs e)
        {
            var martsWithoutCurrent = Storage.Marts
                .Where(m => m.Id != Storage.CurrentMart?.Id)
                .ToList();
            comboboxTransfer.ItemsSource = martsWithoutCurrent;
            comboboxTransfer.SelectedIndex = 0;
        }
        private async void Action(object sender, RoutedEventArgs e)
        {
            if (comboboxTransfer.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите магазин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                int martId = Convert.ToInt32(comboboxTransfer.SelectedValue);

                bool transfer = await Storage.Repo.TransferItem(martId, _itemToTransfer.mid);
                if (transfer)
                {
                    DrJaw.Utils.EventBus.Publish("ItemsChanged");
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

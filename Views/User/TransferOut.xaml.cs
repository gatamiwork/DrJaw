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
        private readonly DGMSSQLItem _itemToTransfer;

        public TransferOut(DGMSSQLItem item)
        {
            InitializeComponent();
            _itemToTransfer = item ?? throw new ArgumentNullException(nameof(item));
            Loaded += TransferOut_Loaded;
        }
        private void TransferOut_Loaded(object sender, RoutedEventArgs e)
        {
            // Информация о товаре
            TextItemInfo.Text =
                $"Артикул: {_itemToTransfer.Articul}\n" +
                $"Тип: {_itemToTransfer.Type}, Металл: {_itemToTransfer.Metal}\n" +
                $"Вес: {_itemToTransfer.Weight:F2}, Цена: {_itemToTransfer.Price:F2}";

            // Безопасно получаем список магазинов ≠ текущему
            var currentMartId = Storage.CurrentMart?.Id;
            var marts = (Storage.Marts ?? Enumerable.Empty<MSSQLMart>())
                        .Where(m => currentMartId == null || m.Id != currentMartId.Value)
                        .ToList();

            comboboxTransfer.ItemsSource = marts;

            if (marts.Count == 0)
            {
                MessageBox.Show("Нет доступных магазинов для отправки.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = false;
                Close();
                return;
            }
            comboboxTransfer.SelectedIndex = 0;
        }
        private async void Action(object sender, RoutedEventArgs e)
        {
            if (comboboxTransfer.SelectedValue is not int martId)
            {
                MessageBox.Show("Пожалуйста, выберите магазин-получатель.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ButtonTransfer.IsEnabled = false;

            try
            {
                // Желательно иметь асинхронную версию в репо: TransferItemAsync(...)
                bool transferOk = await Storage.Repo.TransferItem(martId, _itemToTransfer.mid);
                if (transferOk)
                {
                    DrJaw.Utils.EventBus.Publish("ItemsChanged");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось выполнить отправку.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перемещении товара: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Разблокируем если окно не закрыли
                if (IsLoaded) ButtonTransfer.IsEnabled = true;
            }
        }
    }
}

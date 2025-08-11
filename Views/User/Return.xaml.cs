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
    /// Логика взаимодействия для Return.xaml
    /// </summary>
    public partial class Return : Window
    {
        public Return()
        {
            InitializeComponent();
            Loaded += Return_Loaded;
        }
        private async Task LoadData()
        {
            try
            {
                var dateStart = DatePickerReturn.SelectedDate ?? DateTime.Today;
                var dateEnd = dateStart.AddDays(1).AddTicks(-1); // конец текущего дня

                var data = await Storage.Repo.LoadReturnCartItems(Storage.CurrentMart.Id, dateStart, dateEnd);
                DataGridReturnItems.ItemsSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }
        private async void Return_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }
        private async void DatePickerReturn_SelectedDateChanged(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridReturnItems.SelectedItem is not MSSQLReturnCartItem selectedItem)
            {
                MessageBox.Show("Пожалуйста, выберите изделие.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Сделать возврат изделия?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int id = selectedItem.Id;

                    await Storage.Repo.ReturnItem(id);
                    await Storage.Repo.ReturnCartItem(id);
                    await LoadData(); // метод, который перезагружает таблицу
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка обновления данных: " + ex.Message);
                }
            }
        }
    }
}

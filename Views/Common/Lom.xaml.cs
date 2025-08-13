using DrJaw.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

namespace DrJaw.Views.Common
{
    /// <summary>
    /// Логика взаимодействия для Lom.xaml
    /// </summary>
    public partial class Lom : Window
    {
        private bool isFormLoading = false;
        private List<DGMSSQLLomItem> lomItems = new();
        private List<MSSQLLomTotals> lomTotals = new();

        public Lom()
        {
            InitializeComponent();
            Loaded += Lom_Loaded;
        }
        private async Task LoadData()
        {
            var dateStart = DateFrom.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var dateEnd = (DateTo.SelectedDate ?? DateTime.Today).AddDays(1).AddTicks(-1);

            try
            {
                lomItems = await Storage.Repo.LoadLom(dateStart, dateEnd);
                lomTotals = await Storage.Repo.LoadLomTotals(dateStart, dateEnd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных лома: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lomItems = new();
                lomTotals = new();
            }

            UpdateDataGrid();
            UpdateLomTotals();
        }
        private void UpdateDataGrid()
        {
            if (lomItems == null || lomItems.Count == 0)
            {
                DataGridLom.ItemsSource = null;
                return;
            }

            var filtered = lomItems.AsEnumerable();

            // Фильтрация по магазину
            var martValue = ComboBoxMart.SelectedValue;
            if (martValue is int martId && martId != 0)
            {
                var martName = Storage.Marts.FirstOrDefault(m => m.Id == martId)?.Name;
                if (!string.IsNullOrEmpty(martName))
                    filtered = filtered.Where(item => item.Mart == martName);
            }

            // Фильтрация по пользователю
            var userValue = ComboBoxUser.SelectedValue;
            if (userValue is int userId && userId != 0)
            {
                var userName = Storage.Users.FirstOrDefault(u => u.Id == userId)?.Name;
                if (!string.IsNullOrEmpty(userName))
                    filtered = filtered.Where(item => item.UserName == userName);
            }

            // Преобразуем в список, чтобы избежать повторных вычислений
            var filteredList = filtered.ToList();

            // Обогащаем данными Type и Price
            foreach (var item in filteredList)
            {
                item.Type = item.CartId != null
                    ? "Заказ"
                    : item.Receiving == true
                        ? "Покупка"
                        : "Отгрузка";

                item.Price = item.Weight * (item.PricePerGram ?? 0);
            }

            // Привязываем к DataGrid
            DataGridLom.ItemsSource = filteredList;
        }
        private void UpdateLomTotals()
        {
            int selectedMartId = ComboBoxMart.SelectedValue is int id ? id : 0;

            // локальный хелпер на "нули"
            void ResetTotals()
            {
                labelTotalWeightStart.Content = "Вес: 0.00";
                labelTotalPriceStart.Content = "Цена: 0.00";
                labelCurrentWeight.Content = "Вес: 0.00";
                labelCurrentPrice.Content = "Цена: 0.00";
                labelTotalWeightEnd.Content = "Вес: 0.00";
                labelTotalPriceEnd.Content = "Цена: 0.00";
            }

            if (lomTotals == null || lomTotals.Count == 0)
            {
                ResetTotals();
                return;
            }

            if (selectedMartId == 0)
            {
                decimal startWeight = lomTotals.Sum(x => x.StartWeight);
                decimal startPrice = lomTotals.Sum(x => x.StartPrice);
                decimal currWeight = lomTotals.Sum(x => x.CurrentWeight);
                decimal currPrice = lomTotals.Sum(x => x.CurrentPrice);
                decimal endWeight = lomTotals.Sum(x => x.EndWeight);
                decimal endPrice = lomTotals.Sum(x => x.EndPrice);

                labelTotalWeightStart.Content = $"Вес: {startWeight:F2}";
                labelTotalPriceStart.Content = $"Цена: {startPrice:F2}";
                labelCurrentWeight.Content = $"Вес: {currWeight:F2}";
                labelCurrentPrice.Content = $"Цена: {currPrice:F2}";
                labelTotalWeightEnd.Content = $"Вес: {endWeight:F2}";
                labelTotalPriceEnd.Content = $"Цена: {endPrice:F2}";
            }
            else
            {
                var row = lomTotals.FirstOrDefault(t => t.MartId == selectedMartId);
                if (row == null)
                {
                    ResetTotals();
                    return;
                }

                labelTotalWeightStart.Content = $"Вес: {row.StartWeight:F2}";
                labelTotalPriceStart.Content = $"Цена: {row.StartPrice:F2}";
                labelCurrentWeight.Content = $"Вес: {row.CurrentWeight:F2}";
                labelCurrentPrice.Content = $"Цена: {row.CurrentPrice:F2}";
                labelTotalWeightEnd.Content = $"Вес: {row.EndWeight:F2}";
                labelTotalPriceEnd.Content = $"Цена: {row.EndPrice:F2}";
            }
        }
        private async void Lom_Loaded(object sender, RoutedEventArgs e)
        {
            DateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            DateTo.SelectedDate = DateTime.Today;

            var martsWithAll = new List<MSSQLMart>
    {
        new MSSQLMart { Id = 0, Name = "Все магазины" }
    };
            martsWithAll.AddRange(Storage.Marts);
            ComboBoxMart.ItemsSource = martsWithAll;
            ComboBoxMart.SelectedIndex = 0;

            var usersWithAll = new List<MSSQLUser>
        {
            new MSSQLUser { Id = 0, Name = "Все пользователи" }
        };
            usersWithAll.AddRange(Storage.Users);
            Trace.WriteLine(Storage.Users.Count().ToString());
            ComboBoxUser.ItemsSource = usersWithAll;
            ComboBoxUser.SelectedIndex = 0;
            await LoadData(); 
        }
        private async void Date_SelectedDateChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) // чтобы не срабатывало при загрузке формы
            {
                await LoadData(); // или твой метод обновления
            }
        }
        private void ComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) // чтобы не срабатывало при загрузке формы
            {
                UpdateDataGrid();
                UpdateLomTotals();
            }
        }
    }
}

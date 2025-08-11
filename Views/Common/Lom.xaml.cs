using DrJaw.Models;
using System;
using System.Collections.Generic;
using System.Data;
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
        private List<MSSQLLomItem> lomItems = new();
        private DataTable lomTotals;
        public Lom()
        {
            InitializeComponent();
            Loaded += Lom_Loaded;
        }
        private async Task LoadData()
        {
            var dateStart = DateFrom.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var dateEnd = (DateTo.SelectedDate ?? DateTime.Today).AddDays(1).AddTicks(-1);

            lomItems = await Storage.Repo.LoadLom(dateStart, dateEnd);

            if (lomItems.Count == 0)
            {
                DataGridLom.ItemsSource = null;
                UpdateLomTotals();
                return;
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

            if (lomTotals == null || lomTotals.Rows.Count == 0)
                return;

            if (selectedMartId == 0)
            {
                // Агрегируем по всем строкам
                decimal startWeight = 0, startPrice = 0;
                decimal currentWeight = 0, currentPrice = 0;
                decimal endWeight = 0, endPrice = 0;

                foreach (DataRow r in lomTotals.Rows)
                {
                    startWeight += r.Field<decimal?>("StartWeight") ?? 0;
                    startPrice += r.Field<decimal?>("StartPrice") ?? 0;
                    currentWeight += r.Field<decimal?>("CurrentWeight") ?? 0;
                    currentPrice += r.Field<decimal?>("CurrentPrice") ?? 0;
                    endWeight += r.Field<decimal?>("EndWeight") ?? 0;
                    endPrice += r.Field<decimal?>("EndPrice") ?? 0;
                }

                labelTotalWeightStart.Content = $"Вес: {startWeight:F2}";
                labelTotalPriceStart.Content = $"Цена: {startPrice:F2}";
                labelCurrentWeight.Content = $"Вес: {currentWeight:F2}";
                labelCurrentPrice.Content = $"Цена: {currentPrice:F2}";
                labelTotalWeightEnd.Content = $"Вес: {endWeight:F2}";
                labelTotalPriceEnd.Content = $"Цена: {endPrice:F2}";
            }
            else
            {
                // Фильтруем по магазину
                var row = lomTotals.AsEnumerable()
                    .FirstOrDefault(r => r.Field<int?>("MartId") == selectedMartId);

                if (row != null)
                {
                    labelTotalWeightStart.Content = $"Вес: {row.Field<decimal?>("StartWeight") ?? 0:F2}";
                    labelTotalPriceStart.Content = $"Цена: {row.Field<decimal?>("StartPrice") ?? 0:F2}";
                    labelCurrentWeight.Content = $"Вес: {row.Field<decimal?>("CurrentWeight") ?? 0:F2}";
                    labelCurrentPrice.Content = $"Цена: {row.Field<decimal?>("CurrentPrice") ?? 0:F2}";
                    labelTotalWeightEnd.Content = $"Вес: {row.Field<decimal?>("EndWeight") ?? 0:F2}";
                    labelTotalPriceEnd.Content = $"Цена: {row.Field<decimal?>("EndPrice") ?? 0:F2}";
                }
                else
                {
                    // Нет данных по выбранному магазину
                    labelTotalWeightStart.Content = "Вес: 0.00";
                    labelTotalPriceStart.Content = "Цена: 0.00";
                    labelCurrentWeight.Content = "Вес: 0.00";
                    labelCurrentPrice.Content = "Цена: 0.00";
                    labelTotalWeightEnd.Content = "Вес: 0.00";
                    labelTotalPriceEnd.Content = "Цена: 0.00";
                }
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
            ComboBoxUser.ItemsSource = usersWithAll;
            ComboBoxUser.SelectedIndex = 0;
            await LoadData(); 
        }
        private async void DateFrom_SelectedDateChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) // чтобы не срабатывало при загрузке формы
            {
                await LoadData(); // или твой метод обновления
            }
        }
        private void ComboBoxMart_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) // чтобы не срабатывало при загрузке формы
            {
                UpdateDataGrid();
                UpdateLomTotals();
            }
        }
    }
}

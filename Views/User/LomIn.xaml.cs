using DrJaw.Models;
using DrJaw.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public partial class LomIn : Window
    {
        public LomIn()
        {
            InitializeComponent();
            // Маски ввода
            InputValidators.AttachNumericValidation(textBoxWeight);
            InputValidators.AttachNumericValidation(textBoxPricePerGramm);
            // Автопересчёт
            textBoxWeight.TextChanged += TotalChanged;
            textBoxPricePerGramm.TextChanged += TotalChanged;

            Loaded += (_, __) => textBoxWeight.Focus();
        }
        private static bool TryParseDecimal(string? s, out decimal v)
        {
            v = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().Replace(" ", "");

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return true;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return true;

            var curSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var altSep = curSep == "," ? "." : ",";
            var swapped = s.Replace(altSep, curSep);
            return decimal.TryParse(swapped, NumberStyles.Number, CultureInfo.CurrentCulture, out v);
        }
        private void TotalChanged(object? sender, TextChangedEventArgs e)
        {
            if (TryParseDecimal(textBoxWeight.Text, out var w) &&
                TryParseDecimal(textBoxPricePerGramm.Text, out var p) &&
                w > 0 && p > 0)
            {
                var total = decimal.Round(w * p, 2, MidpointRounding.AwayFromZero);
                textBoxTotalPrice.Text = total.ToString("F2", CultureInfo.CurrentCulture);
            }
            else
            {
                textBoxTotalPrice.Text = "0";
            }
        }
        private async void Action(object sender, RoutedEventArgs e)
        {
            // Проверка окружения
            if (Storage.CurrentUser?.Id is not int userId)
            {
                MessageBox.Show("Пользователь не определён.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Storage.CurrentMart?.Id is not int martId)
            {
                MessageBox.Show("Магазин не выбран.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Парсинг значений
            if (!TryParseDecimal(textBoxWeight.Text, out var weight) || weight <= 0)
            {
                MessageBox.Show("Введите корректный вес (больше 0).", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!TryParseDecimal(textBoxPricePerGramm.Text, out var pricePerGram) || pricePerGram <= 0)
            {
                MessageBox.Show("Введите корректную цену за грамм (больше 0).", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            buttonAction.IsEnabled = false;

            try
            {
                int lomId = await Storage.Repo.CreateLom(Storage.CurrentUser?.Id, Storage.CurrentMart?.Id, true, weight, pricePerGram);
                if (lomId > 0)
                {
                    EventBus.Publish("ItemsChanged"); // если нужно обновление панели
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Не удалось добавить лом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (IsLoaded) buttonAction.IsEnabled = true;
            }
        }
    }
}

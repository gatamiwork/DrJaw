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
using static MaterialDesignThemes.Wpf.Theme;

namespace DrJaw.Views.User
{
    public partial class LomOut : Window
    {
        public LomOut()
        {
            InitializeComponent();
            InputValidators.AttachNumericValidation(textBoxLom);
            Loaded += (_, __) => textBoxLom.Focus();
        }
        private static bool TryParseDecimal(string? s, out decimal v)
        {
            v = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().Replace(" ", "");

            // текущая культура
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return true;
            // инвариантная
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return true;

            // альтернативный разделитель
            var curSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var altSep = curSep == "," ? "." : ",";
            var swapped = s.Replace(altSep, curSep);
            return decimal.TryParse(swapped, NumberStyles.Number, CultureInfo.CurrentCulture, out v);
        }

        private async void Action(object sender, RoutedEventArgs e)
        {
            // быстрые проверки окружения
            if (Storage.CurrentUser?.Id is not int userId)
            {
                MessageBox.Show("Пользователь не определён.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Storage.CurrentMart?.Id is not int martId)
            {
                MessageBox.Show("Магазин не выбран.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // парсим вес без Replace('.', ',')
            if (!TryParseDecimal(textBoxLom.Text, out var weight) || weight <= 0)
            {
                MessageBox.Show("Введите корректный вес (больше 0).", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            buttonAction.IsEnabled = false;
            try
            {
                int lomId = await Storage.Repo.CreateLom(Storage.CurrentUser?.Id, Storage.CurrentMart?.Id, false, weight);
                if (lomId > 0)
                {
                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось создать запись лома.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                if (IsLoaded) buttonAction.IsEnabled = true;
            }
        }
    }
}

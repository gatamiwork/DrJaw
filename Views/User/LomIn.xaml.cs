using DrJaw.Models;
using DrJaw.Utils;
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
    /// Логика взаимодействия для LomIn.xaml
    /// </summary>
    public partial class LomIn : Window
    {
        public LomIn()
        {
            InitializeComponent();
            InputValidators.AttachNumericValidation(textBoxWeight);
            textBoxWeight.TextChanged += TotalCount;
            InputValidators.AttachNumericValidation(textBoxPricePerGramm);
            textBoxPricePerGramm.TextChanged += TotalCount;
        }
        private void TotalCount(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(textBoxWeight.Text, out decimal weight) &&
                decimal.TryParse(textBoxPricePerGramm.Text, out decimal price))
            {
                decimal total = weight * price;
                textBoxTotalPrice.Text = total.ToString();
            }
            else
            {
                textBoxTotalPrice.Text = "0";
            }
        }
        private async void Action(object sender, RoutedEventArgs e)
        {
            buttonAction.IsEnabled = false;

            if (!decimal.TryParse(textBoxWeight.Text, out decimal weight) || weight <= 0)
            {
                MessageBox.Show("Введите корректный вес (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                buttonAction.IsEnabled = true;
                return;
            }

            if (!decimal.TryParse(textBoxPricePerGramm.Text, out decimal pricePerGram) || pricePerGram <= 0)
            {
                MessageBox.Show("Введите корректную цену за грамм (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                buttonAction.IsEnabled = true;
                return;
            }

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
                    buttonAction.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                buttonAction.IsEnabled = true;
            }
        }
    }
}

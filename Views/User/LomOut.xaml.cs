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
using static MaterialDesignThemes.Wpf.Theme;

namespace DrJaw.Views.User
{
    /// <summary>
    /// Логика взаимодействия для LomOut.xaml
    /// </summary>
    public partial class LomOut : Window
    {
        public LomOut()
        {
            InitializeComponent();
            InputValidators.AttachNumericValidation(textBoxLom);
        }

        private async void Action(object sender, RoutedEventArgs e)
        {
            buttonAction.IsEnabled = false;

            if (!decimal.TryParse(textBoxLom.Text, out decimal weight))
            {
                MessageBox.Show("Некорректный вес.");
                buttonAction.IsEnabled = true;
                return;
            }

            try
            {
                int lomId = await Storage.Repo.CreateLom(Storage.CurrentUser?.Id, Storage.CurrentMart?.Id, false, weight);
                if (lomId > 0) this.Close();
                else
                {
                    MessageBox.Show("Не удалось создать запись лома.");
                    buttonAction.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                buttonAction.IsEnabled = true;
            }
        }
    }
}

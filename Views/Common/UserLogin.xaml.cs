using DrJaw.Models;
using System.Windows;

namespace DrJaw.Views.Common
{
    public partial class UserLogin : Window
    {
        public UserLogin()
        {
            InitializeComponent();
            Loaded += UserLogin_Loaded;
        }
        private async void UserLogin_Loaded(object sender, RoutedEventArgs e)
        {
            Storage.Users = await Storage.Repo.LoadUsers();
            Storage.Marts = await Storage.Repo.LoadMarts();
            Storage.Metals = await Storage.Repo.LoadMetals();
            Storage.CurrentMetal = Storage.Metals?.FirstOrDefault();

            comboboxMSSQLUser.ItemsSource = Storage.Users;
            comboboxMSSQLMart.ItemsSource = Storage.Marts;

            if (Storage.CurrentUser != null)
                comboboxMSSQLUser.SelectedValue = Storage.CurrentUser.Id;
            else
                comboboxMSSQLUser.SelectedIndex = -1;

            if (Storage.CurrentMart != null)
                comboboxMSSQLMart.SelectedValue = Storage.CurrentMart.Id;
            else
                comboboxMSSQLMart.SelectedIndex = -1;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (comboboxMSSQLUser.SelectedIndex == -1) 
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }
            if (comboboxMSSQLMart.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите магазин");
                return;
            }

            Storage.CurrentUser = comboboxMSSQLUser.SelectedItem as MSSQLUser;
            Storage.CurrentMart = comboboxMSSQLMart.SelectedItem as MSSQLMart;

            this.DialogResult = true;
            this.Close();
        }
    }
}

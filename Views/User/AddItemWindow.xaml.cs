using System.Windows;
using DrJaw.ViewModels.User;

namespace DrJaw.Views.User
{
    public partial class AddItemWindow : Window
    {
        public AddItemWindow()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                if (DataContext is AddItemViewModel vm)
                {
                    vm.CloseRequested += (_, ok) =>
                    {
                        DialogResult = ok;
                        Close();
                    };

                    vm.ErrorOccurred += (_, msg) =>
                    {
                        // Можешь заменить на свой ErrorWindow
                        MessageBox.Show(this, msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    };
                }
            };
        }
    }
}

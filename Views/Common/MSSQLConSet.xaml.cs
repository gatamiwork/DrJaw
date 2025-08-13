using System.Windows;
using DrJaw.Models;
using DrJaw.Utils;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class MSSQLConSet : Window
    {
        private MSSQLConSetViewModel _vm;

        public MSSQLConSet()
        {
            InitializeComponent();

            var settings = AppSettingsManager.Load();
            _vm = new MSSQLConSetViewModel(settings);
            _vm.RequestClose += Vm_RequestClose;
            DataContext = _vm;
            PasswordBox.Password = _vm.Password ?? string.Empty;
        }
        private void Vm_RequestClose(bool ok)
        {
            DialogResult = ok;
            Close();
        }
        // Пробрасываем пароль из PasswordBox в VM
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _vm.Password = PasswordBox.Password;
        }
    }
}

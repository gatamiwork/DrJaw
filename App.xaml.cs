using System.Diagnostics;
using System.Windows;
using DrJaw.Views;
using DrJaw.Views.Common;

namespace DrJaw
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1) Настройки MSSQL
            var mssqlWindow = new MSSQLConSet();
            var res = mssqlWindow.ShowDialog();
            if (res != true)
            {
                Trace.WriteLine("MSSQL settings dialog canceled.");
                Shutdown();
                return;
            }

            // 2) Логин
            var loginWindow = new UserLogin();
            var loginResult = loginWindow.ShowDialog();
            if (loginResult != true)
            {
                Trace.WriteLine("Login canceled.");
                Shutdown();
                return;
            }

            // 3) Главное окно
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}

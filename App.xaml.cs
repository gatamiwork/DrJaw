using System.Windows;
using System.Data.SqlClient;
using DrJaw.Services;
using DrJaw.Services.Config;   // ← добавь
using DrJaw.Services.MSSQL;   // ← добавь
using DrJaw.Services.Data;
using DrJaw.Views;
using DrJaw.Views.Common;

namespace DrJaw
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            string? connStr = TryBuildConnStringFromConfig(out bool okFromConfig);
            if (!okFromConfig)
            {
                var dbDialog = new MSSQLConnectWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = false
                };
                var dbOk = dbDialog.ShowDialog() == true &&
                           !string.IsNullOrWhiteSpace(dbDialog.BuiltConnectionString);
                if (!dbOk) { Shutdown(); return; }
                connStr = dbDialog.BuiltConnectionString!;
            }

            var mgr = new MssqlManager(connStr!);
            var repo = new MssqlRepository(mgr);
            var refData = new ReferenceDataService(repo);
            var session = new UserSessionService();

            await refData.EnsureLoadedAsync();

            var loginDialog = new UserLoginWindow(refData, session, repo)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false
            };
            var loginOk = loginDialog.ShowDialog() == true && loginDialog.SelectedUser != null;
            if (!loginOk) { Shutdown(); return; }

            var windows = new WindowService(session, repo, refData);
            var main = new MainWindow(windows, session, refData, repo);
            MainWindow = main;

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }

        private static string? TryBuildConnStringFromConfig(out bool ok)
        {
            ok = false;
            var cfg = ConfigService.Load();
            if (string.IsNullOrWhiteSpace(cfg.Mssql.Server) ||
                string.IsNullOrWhiteSpace(cfg.Mssql.Database) ||
                string.IsNullOrWhiteSpace(cfg.Mssql.Username))
                return null;

            var pwd = Secret.Unprotect(cfg.Mssql.PasswordEnc);
            try
            {
                var sb = new SqlConnectionStringBuilder
                {
                    DataSource = cfg.Mssql.Server,
                    InitialCatalog = cfg.Mssql.Database,
                    UserID = cfg.Mssql.Username,
                    Password = pwd,
                    Encrypt = true,
                    TrustServerCertificate = true
                };
                // тестим синхронно
                using var cn = new SqlConnection(sb.ConnectionString);
                cn.Open();
                ok = true;
                return sb.ConnectionString;
            }
            catch
            {
                ok = false;
                return null;
            }
        }
    }
}

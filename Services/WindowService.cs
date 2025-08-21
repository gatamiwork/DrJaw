using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrJaw.Models;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;
using DrJaw.ViewModels.Common;
using DrJaw.Views.Common;
using DrJaw.Views.User;

namespace DrJaw.Services
{
    public sealed class WindowService : IWindowService
    {
        private readonly IUserSessionService _session;
        private readonly IMssqlRepository _repo;
        private readonly IReferenceDataService _refData;

        public WindowService(IUserSessionService session, IMssqlRepository repo, IReferenceDataService refData)
        {
            _session = session;
            _repo = repo;
            _refData = refData;
        }

        public void ShowLom() => ShowModal(new LomWindow(_repo,_session));
        public void ShowOrders() => ShowModal(new OrdersWindow(_repo, _session));
        public void ShowSettings() => ShowModal(new SettingsWindow());
        public void ShowWoo() => ShowModal(new WooWindow());
        public MSSQLMetal? ShowAddItem(IMssqlRepository repo, MSSQLMetal? metal,
                                       IReferenceDataService refData, IUserSessionService session)
        {
            var vm = new AddItemViewModel(repo, metal, refData, session);
            var win = new Views.User.AddItemWindow { DataContext = vm };
            win.Owner = System.Windows.Application.Current.MainWindow;

            var ok = win.ShowDialog() == true;
            return ok ? vm.SelectedMetal : null;
        }
        public bool? ShowTransferOut(IMssqlRepository repo, IEnumerable<DGMSSQLItem> items)
        {
            var win = new TransferOutWindow(repo, _session, _refData, items);
            win.Owner = Application.Current.MainWindow;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return win.ShowDialog();
        }
        public void ShowLomOut()
        {
            var w = new LomOutWindow(_repo, _session);
            w.Owner = Application.Current.MainWindow;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowDialog();
        }
        public void ShowLomIn()
        {
            var w = new LomInWindow(_repo, _session);
            w.Owner = Application.Current.MainWindow;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowDialog();
        }

        public bool? ShowReturn(IMssqlRepository repo, IUserSessionService session)
        {
            var win = new ReturnWindow(repo, session) { Owner = Application.Current.MainWindow };
            return win.ShowDialog();
        }
        public bool? ShowDeleteItem(IMssqlRepository repo, IEnumerable<DGMSSQLItem> items)
        {
            var win = new DeleteItemWindow(repo, _session, items);
            win.Owner = System.Windows.Application.Current.MainWindow;
            return win.ShowDialog();
        }
        public bool? ShowTransferIn()
        {
            var w = new TransferInWindow(_repo, _session);
            w.Owner = Application.Current.MainWindow;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return w.ShowDialog();
        }
        public void ShowCart()
        {
            var w = new CartWindow(_repo, _session, this);
            w.Owner = Application.Current.MainWindow;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowDialog();
        }


        public UserLoginResult? ShowUserLoginDialog()
        {
            var dlg = new UserLoginWindow(_refData, _session, _repo)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dlg.DataContext is UserLoginViewModel vm)
                vm.SetInitialSelection(_session.CurrentUser, _session.CurrentMart);

            var ok = dlg.ShowDialog() == true && dlg.SelectedUser != null;
            if (!ok) return null;

            return new UserLoginResult
            {
                User = dlg.SelectedUser!,
                Mart = dlg.SelectedMart,
                AdminPassword = dlg.AdminPasswordText
            };
        }

        private static bool? ShowModal(Window w)
        {
            w.Owner = Application.Current.MainWindow;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return w.ShowDialog();
        }

        public void ShowError(string title, string message)
        {
            var w = new ErrorWindow(title, message)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            w.ShowDialog();
        }
        public void ShowImage(BitmapSource image, string? title = null)
        {
            var w = new ImageViewerWindow(image, title)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            w.ShowDialog();
        }

        public Task ShowMessageAsync(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
    }
}

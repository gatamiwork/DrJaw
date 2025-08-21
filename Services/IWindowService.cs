using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrJaw.Models;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.User;
using DrJaw.Views.User;

namespace DrJaw.Services
{
    public interface IWindowService
    {
        void ShowLom();
        void ShowOrders();
        void ShowSettings();
        void ShowWoo();

        // Диалоги User-панели:
        MSSQLMetal? ShowAddItem(
            IMssqlRepository repo,
            MSSQLMetal? metal,
            IReferenceDataService refData,
            IUserSessionService session);
        bool? ShowTransferOut(IMssqlRepository repo, IEnumerable<DGMSSQLItem> items);
        void ShowLomOut();
        void ShowLomIn();
        bool? ShowDeleteItem(IMssqlRepository repo, IEnumerable<DGMSSQLItem> items);
        bool? ShowReturn(IMssqlRepository repo, IUserSessionService session);
        bool? ShowTransferIn();
        void ShowCart();
        void ShowError(string title, string message);
        void ShowImage(BitmapSource image, string? title = null);

        UserLoginResult? ShowUserLoginDialog();
    }
}

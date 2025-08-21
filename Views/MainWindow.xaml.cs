using System.Windows;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels;

namespace DrJaw.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(IWindowService windows, IUserSessionService session, IReferenceDataService refData, IMssqlRepository repo)
        {
            InitializeComponent();
            DataContext = new MainViewModel(windows, session, refData, repo);
        }
    }
}

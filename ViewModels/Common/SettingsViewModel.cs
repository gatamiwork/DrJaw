namespace DrJaw.ViewModels.Common
{
    public sealed class SettingsViewModel : ViewModels.ViewModelBase
    {
        private string _header = "Settings";
        public string Header { get => _header; set => Set(ref _header, value); }
    }
}

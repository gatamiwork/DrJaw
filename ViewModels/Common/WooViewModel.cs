namespace DrJaw.ViewModels.Common
{
    public sealed class WooViewModel : ViewModels.ViewModelBase
    {
        private string _header = "WooCommerce";
        public string Header { get => _header; set => Set(ref _header, value); }
    }
}

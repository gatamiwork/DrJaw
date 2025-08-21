namespace DrJaw.ViewModels.Common
{
    public sealed class ErrorViewModel : ViewModels.ViewModelBase
    {
        private string _title = "Ошибка";
        public string Title { get => _title; set => Set(ref _title, value); }

        private string _message = "";
        public string Message { get => _message; set => Set(ref _message, value); }
    }
}

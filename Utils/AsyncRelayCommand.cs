using System;
using System.Threading.Tasks;

namespace DrJaw.Utils
{
    public class AsyncRelayCommand : RelayCommandBase
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;
        private bool _running;

        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public override bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

        public override async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _running = true; RaiseCanExecuteChanged();
            try { await _executeAsync(); }
            finally { _running = false; RaiseCanExecuteChanged(); }
        }
    }
}

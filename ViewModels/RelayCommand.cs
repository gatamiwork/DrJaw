using System;
using System.Windows.Input;

namespace DrJaw.ViewModels
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _exec;
        private readonly Func<object?, bool>? _can;

        public RelayCommand(Action<object?> exec, Func<object?, bool>? can = null)
            => (_exec, _can) = (exec, can);

        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _exec(p);

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _exec;
        private readonly Predicate<object?>? _can;
        private readonly Action<Exception>? _onException; // опциональный хук
        private bool _busy;

        public AsyncRelayCommand(Func<object?, Task> exec, Predicate<object?>? can = null, Action<Exception>? onException = null)
        { _exec = exec ?? throw new ArgumentNullException(nameof(exec)); _can = can; _onException = onException; }

        public bool CanExecute(object? p) => !_busy && (_can?.Invoke(p) ?? true);

        public async void Execute(object? p)
        {
            _busy = true; RaiseCanExecuteChanged();
            try { await _exec(p); }
            catch (Exception ex) { _onException?.Invoke(ex); /* не даём упасть UI */ }
            finally { _busy = false; RaiseCanExecuteChanged(); }
        }

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

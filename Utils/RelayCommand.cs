using System;
using System.Windows.Input;

namespace DrJaw.Utils
{
    public abstract class RelayCommandBase : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public abstract bool CanExecute(object? parameter);
        public abstract void Execute(object? parameter);
    }

    public class RelayCommand : RelayCommandBase
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public override bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public override void Execute(object? parameter) => _execute();
    }
}

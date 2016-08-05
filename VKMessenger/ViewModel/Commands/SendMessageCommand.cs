using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VKMessenger.ViewModel.Commands
{
    public class SendMessageCommand : ICommand
    {
        private Action _targetExecuteAction;
        private Func<bool> _targetCanExecuteMethod;

        public SendMessageCommand(Action executeAction)
        {
            _targetExecuteAction = executeAction;
        }

        public SendMessageCommand(Action executeAction, Func<bool> canExecuteMethod)
        {
            _targetExecuteAction = executeAction;
            _targetCanExecuteMethod = canExecuteMethod;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (_targetCanExecuteMethod != null)
            {
                return _targetCanExecuteMethod();
            }
            
            return _targetExecuteAction != null;
        }

        public void Execute(object parameter)
        {
            _targetExecuteAction?.Invoke();
        }
    }
}

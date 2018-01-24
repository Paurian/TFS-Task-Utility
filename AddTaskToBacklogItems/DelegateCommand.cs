using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AddTaskToBacklogItems
{
    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _executeWithParameter;
        private readonly Action _executeNoParameters;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action executeNoParameters) : this (executeNoParameters, null)
        {
        }

        public DelegateCommand(Action executeNoParameters, Predicate<object> canExecute)
        {
            _executeNoParameters = executeNoParameters;
            _canExecute = canExecute;
        }

        public DelegateCommand(Action<object> executeWithParameter) : this(executeWithParameter, null)
        {
        }

        public DelegateCommand(Action<object> executeWithParameter, Predicate<object> canExecute)
        {
            _executeWithParameter = executeWithParameter;
            _canExecute = canExecute;
        }

        public virtual bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public virtual void Execute(object parameter)
        {
            if (_executeWithParameter != null)
            {
                _executeWithParameter(parameter);
            }
            else if (_executeNoParameters != null)
            {
                _executeNoParameters();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
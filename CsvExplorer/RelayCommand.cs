using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CsvExplorer
{
    public class RelayCommand : ICommand
    {
        private Action<object> Command { get; }
        private Predicate<object> CanExecutePredicate { get; }

        public RelayCommand(Action<object> command, Predicate<object> canExecute = null)
        {
            Command = command;
            CanExecutePredicate = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return CanExecutePredicate?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            Command(parameter);
        }
    }
}

using System;
using System.Windows.Input;

namespace CommentClassifier
{
    class DelegatingCommand: ICommand
    {
        private readonly Action<object> _a;

        public DelegatingCommand(Action<object> a)
        {
            _a = a;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        #pragma warning disable 67
        public event EventHandler CanExecuteChanged;
        #pragma warning restore 67

        public void Execute(object parameter)
        {
            _a(parameter);
        }
    }
}

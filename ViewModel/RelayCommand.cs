using System;
using System.Windows.Input;

namespace ShaderBaker.ViewModel
{

public class RelayCommand : ICommand
{
    public event EventHandler CanExecuteChanged;

    public delegate bool NoArgPredicate();
    public NoArgPredicate canExecute;

    public Action execute;

    public RelayCommand(Action execute)
        : this(execute, () => true)
    { }

    public RelayCommand(Action execute, NoArgPredicate canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public void RaiseCanExecuteChanged()
    {
        var handlers = CanExecuteChanged;
        if (handlers != null)
        {
            handlers(this, EventArgs.Empty);
        }
    }
    
    public bool CanExecute(object parameter)
    {
        return canExecute();
    }

    public void Execute(object parameter)
    {
        execute();
    }
}

public class RelayCommand<T> : ICommand
{
    public event EventHandler CanExecuteChanged;

    public Predicate<T> canExecute;

    public Action<T> execute;

    public RelayCommand(Action<T> execute)
        : this(execute, parameter => true)
    { }

    public RelayCommand(Action<T> execute, Predicate<T> canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public void RaiseCanExecuteChanged()
    {
        var handlers = CanExecuteChanged;
        if (handlers != null)
        {
            handlers(this, EventArgs.Empty);
        }
    }
    
    public bool CanExecute(object parameter)
    {
        return canExecute((T) parameter);
    }

    public void Execute(object parameter)
    {
        execute((T) parameter);
    }
}

}

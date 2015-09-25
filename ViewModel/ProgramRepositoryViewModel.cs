using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ShaderBaker.ViewModel
{

public class ProgramRepositoryViewModel
{
    public ObservableCollection<ProgramViewModel> Programs
    {
        get;
        private set;
    }

    public ICommand AddProgramCommand
    {
        get;
        private set;
    }

    public ProgramRepositoryViewModel()
    {
        Programs = new ObservableCollection<ProgramViewModel>();
        AddProgramCommand = new AddProgramCommandImpl(this);
    }

    private void addProgram()
    {
        Programs.Add(new ProgramViewModel());
    }

    private class AddProgramCommandImpl : ICommand
    {
        private readonly ProgramRepositoryViewModel programRepo;

        public event EventHandler CanExecuteChanged;

        public AddProgramCommandImpl(ProgramRepositoryViewModel programRepo)
        {
            this.programRepo = programRepo;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            programRepo.addProgram();
        }
    }
}

}

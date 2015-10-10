using ShaderBaker.GlRenderer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ShaderBaker.ViewModel
{

class ObjectRepositoryViewModel
{
    private readonly IDictionary<Shader, ShaderViewModel> shaderViewModelsByShader;
    
    public ObservableCollection<ShaderViewModel> Shaders
    {
        get;
        private set;
    }

    public ObservableCollection<ProgramViewModel> Programs
    {
        get;
        private set;
    }
    
    public ICommand AddVertexShaderCommand
    {
        get;
        private set;
    }
    
    public ICommand AddGeometryShaderCommand
    {
        get;
        private set;
    }
    
    public ICommand AddFragmentShaderCommand
    {
        get;
        private set;
    }

    public ICommand AddProgramCommand
    {
        get;
        private set;
    }

    private ShaderViewModel selectedShader;
    public ShaderViewModel SelectedShader
    {
        get
        {
            return selectedShader;
        }
        set
        {
            selectedShader = value;
            renameShaderCommand.RaiseCanExecuteChanged();
        }
    }

    private RenameShaderCommandImpl renameShaderCommand;
    public ICommand RenameShaderCommand
    {
        get
        {
            return renameShaderCommand;
        }
    }

    public ObjectRepositoryViewModel()
    {
        Programs = new ObservableCollection<ProgramViewModel>();
        shaderViewModelsByShader = new Dictionary<Shader, ShaderViewModel>();
        Shaders = new ObservableCollection<ShaderViewModel>();
        AddProgramCommand = new AddProgramCommandImpl(this);
        AddVertexShaderCommand = new AddShaderCommand(this, ProgramStage.Vertex);
        AddGeometryShaderCommand = new AddShaderCommand(this, ProgramStage.Geometry);
        AddFragmentShaderCommand = new AddShaderCommand(this, ProgramStage.Fragment);
        renameShaderCommand = new RenameShaderCommandImpl(this);
    }

    public ShaderViewModel GetViewModelForShader(Shader shader)
    {
        return shaderViewModelsByShader[shader];
    }

    private void addShader(Shader shader)
    {
        var shaderViewModel = new ShaderViewModel(shader);
        shaderViewModelsByShader.Add(shader, shaderViewModel);
        Shaders.Add(shaderViewModel);
    }
    
    private void addProgram()
    {
        Programs.Add(new ProgramViewModel());
    }

    private class AddShaderCommand : ICommand
    {
        private readonly ObjectRepositoryViewModel repo;

        private readonly ProgramStage stage;

        public event EventHandler CanExecuteChanged;

        public AddShaderCommand(ObjectRepositoryViewModel repo, ProgramStage stage)
        {
            this.repo = repo;
            this.stage = stage;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            repo.addShader(new Shader(stage));
        }
    }

    private class AddProgramCommandImpl : ICommand
    {
        private readonly ObjectRepositoryViewModel repo;

        public event EventHandler CanExecuteChanged;

        public AddProgramCommandImpl(ObjectRepositoryViewModel repo)
        {
            this.repo = repo;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            repo.addProgram();
        }
    }

    private class RenameShaderCommandImpl : ICommand
    {
        private readonly ObjectRepositoryViewModel repo;

        public event EventHandler CanExecuteChanged;

        public RenameShaderCommandImpl(ObjectRepositoryViewModel repo)
        {
            this.repo = repo;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged(repo, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return repo.SelectedShader != null;
        }

        public void Execute(object parameter)
        {
            repo.SelectedShader.Renaming = true;
        }
    }
}

}

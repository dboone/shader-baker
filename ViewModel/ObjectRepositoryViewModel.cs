using ShaderBaker.GlRenderer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ShaderBaker.ViewModel
{

class ObjectRepositoryViewModel : ViewModelBase
{
    private readonly IDictionary<Shader, ShaderViewModel> shaderViewModelsByShader;
    
    public ObservableCollection<ShaderViewModel> Shaders
    {
        get;
        private set;
    }

    public ObservableCollection<ShaderViewModel> OpenShaders
    {
        get;
        private set;
    }

    private int activeOpenShaderIndex;
    public int ActiveOpenShaderIndex
    {
        get { return activeOpenShaderIndex; }
        set
        {
            activeOpenShaderIndex = value;
            OnPropertyChanged("ActiveOpenShaderIndex");
        }
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
            attachSelectedShaderCommand.RaiseCanExecuteChanged();
        }
    }

    private ProgramViewModel selectedProgram;
    public ProgramViewModel SelectedProgram
    {
        get
        {
            return selectedProgram;
        }
        set
        {
            selectedProgram = value;
            attachSelectedShaderCommand.RaiseCanExecuteChanged();
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

    private AttachSelectedShaderCommandImpl attachSelectedShaderCommand;
    public ICommand AttachSelectedShaderCommand
    {
        get
        {
            return attachSelectedShaderCommand;
        }
    }

    public ObjectRepositoryViewModel()
    {
        Programs = new ObservableCollection<ProgramViewModel>();
        shaderViewModelsByShader = new Dictionary<Shader, ShaderViewModel>();
        Shaders = new ObservableCollection<ShaderViewModel>();
        OpenShaders = new ObservableCollection<ShaderViewModel>();
        activeOpenShaderIndex = -1;
        AddProgramCommand = new AddProgramCommandImpl(this);
        AddVertexShaderCommand = new AddShaderCommand(this, ProgramStage.Vertex);
        AddGeometryShaderCommand = new AddShaderCommand(this, ProgramStage.Geometry);
        AddFragmentShaderCommand = new AddShaderCommand(this, ProgramStage.Fragment);
        renameShaderCommand = new RenameShaderCommandImpl(this);
        attachSelectedShaderCommand = new AttachSelectedShaderCommandImpl(this);
    }

    private bool isShaderSelected()
    {
        return SelectedShader != null;
    }

    private bool isProgramSelected()
    {
        return SelectedProgram != null;
    }

    public ShaderViewModel GetViewModelForShader(Shader shader)
    {
        return shaderViewModelsByShader[shader];
    }

    public void OnProgramTreeSelectionChanged(object selectedItem)
    {
        SelectedProgram = selectedItem as ProgramViewModel;
    }

    public void OpenSelectedShader()
    {
        if (selectedShader == null)
        {
            return;
        }

        var openShaderIndex = OpenShaders.IndexOf(selectedShader);
        if (openShaderIndex == -1)
        {
            OpenShaders.Add(selectedShader);
            ActiveOpenShaderIndex = OpenShaders.Count - 1;   
        } else
        {
            ActiveOpenShaderIndex = openShaderIndex;
        }
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

    private void attachSelectedShaderToSelectedProgram()
    {
        attachShaderToProgram(SelectedProgram, SelectedShader);
    }

    private static void attachShaderToProgram(ProgramViewModel program, ShaderViewModel shader)
    {
        var currentAttachedShader = program.GetShaderForStage(shader.Stage);
        if (currentAttachedShader.hasValue())
        {
            program.DetachShader(currentAttachedShader.get());
        }
        program.AttachShader(shader);
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
            return repo.isShaderSelected();
        }

        public void Execute(object parameter)
        {
            if (!repo.SelectedShader.Renaming)
            {
                repo.SelectedShader.Renaming = true;
            }
        }
    }

    private class AttachSelectedShaderCommandImpl : ICommand
    {
        private readonly ObjectRepositoryViewModel repo;

        public event EventHandler CanExecuteChanged;

        public AttachSelectedShaderCommandImpl(ObjectRepositoryViewModel repo)
        {
            this.repo = repo;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged(repo, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return repo.isShaderSelected() && repo.isProgramSelected();
        }

        public void Execute(object parameter)
        {
            repo.attachSelectedShaderToSelectedProgram();
        }
    }
}

}

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
            attachSelectedShaderToSelectedProgrammCommand.RaiseCanExecuteChanged();
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
            renameSelectedProgramCommand.RaiseCanExecuteChanged();
            attachSelectedShaderToSelectedProgrammCommand.RaiseCanExecuteChanged();
        }
    }

    private RelayCommand renameShaderCommand;
    public ICommand RenameShaderCommand
    {
        get
        {
            return renameShaderCommand;
        }
    }

    private RelayCommand renameSelectedProgramCommand;
    public ICommand RenameSelectedProgramCommand
    {
        get
        {
            return renameSelectedProgramCommand;
        }
    }

    private RelayCommand attachSelectedShaderToSelectedProgrammCommand;
    public ICommand AttachSelectedShaderToSelectedProgramCommand
    {
        get
        {
            return attachSelectedShaderToSelectedProgrammCommand;
        }
    }

    public ObjectRepositoryViewModel()
    {
        Programs = new ObservableCollection<ProgramViewModel>();
        shaderViewModelsByShader = new Dictionary<Shader, ShaderViewModel>();
        Shaders = new ObservableCollection<ShaderViewModel>();
        OpenShaders = new ObservableCollection<ShaderViewModel>();
        activeOpenShaderIndex = -1;

        AddProgramCommand = new RelayCommand(addProgram);

        AddVertexShaderCommand = new RelayCommand(() => addNewShader(ProgramStage.Vertex));
        AddGeometryShaderCommand = new RelayCommand(() => addNewShader(ProgramStage.Geometry));
        AddFragmentShaderCommand = new RelayCommand(() => addNewShader(ProgramStage.Fragment));

        renameShaderCommand = new RelayCommand(
            () =>
            {
                if (!SelectedShader.Renaming)
                {
                    SelectedShader.Renaming = true;
                }
            },
            isShaderSelected);
        
        renameSelectedProgramCommand = new RelayCommand(
            () =>
            {
                SelectedProgram.Renaming = true;
            },
            isProgramSelected);

        attachSelectedShaderToSelectedProgrammCommand = new RelayCommand(
            () => attachSelectedShaderToSelectedProgram(),
            () => isShaderSelected() && isProgramSelected());
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

    private void addNewShader(ProgramStage stage)
    {
        addShader(new Shader(stage));
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
}

}

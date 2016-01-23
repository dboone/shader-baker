using ShaderBaker.GlRenderer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace ShaderBaker.ViewModel
{

class ObjectRepositoryViewModel : ViewModelBase
{
    public GlContextManager GlContextManager { get; }

    private readonly IDictionary<Shader, ShaderViewModel> shaderViewModelsByShader;
    
    public ObservableCollection<ShaderViewModel> Shaders { get; }

    public ObservableCollection<ShaderViewModel> OpenShaders { get; }

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

    public ObservableCollection<ProgramViewModel> Programs { get; }
    
    public ICommand AddVertexShaderCommand { get; }
    
    public ICommand AddGeometryShaderCommand {  get; }
    
    public ICommand AddFragmentShaderCommand { get; }

    public ICommand AddProgramCommand { get; }

    private ShaderViewModel selectedShader;
    public ShaderViewModel SelectedShader
    {
        get { return selectedShader; }
        set
        {
            selectedShader = value;
            renameShaderCommand.RaiseCanExecuteChanged();
            attachSelectedShaderToSelectedProgramCommand.RaiseCanExecuteChanged();
        }
    }

    private ProgramViewModel selectedProgram;
    public ProgramViewModel SelectedProgram
    {
        get { return selectedProgram; }
        set
        {
            selectedProgram = value;
            attachSelectedShaderToSelectedProgramCommand.RaiseCanExecuteChanged();
            activateSelectedProgramCommand.RaiseCanExecuteChanged();
        }
    }

    private ProgramViewModel activeProgram;
    public ProgramViewModel ActiveProgram
    {
        get { return activeProgram; }
        set
        {
            activeProgram = value;
            OnPropertyChanged("ActiveProgram");
        }
    }

    private readonly RelayCommand renameShaderCommand;
    public ICommand RenameShaderCommand => renameShaderCommand;

    private readonly RelayCommand attachSelectedShaderToSelectedProgramCommand;
    public ICommand AttachSelectedShaderToSelectedProgramCommand
        => attachSelectedShaderToSelectedProgramCommand;

    private readonly RelayCommand activateSelectedProgramCommand;
    public ICommand ActivateSelectedProgramCommand => activateSelectedProgramCommand;

    public ObjectRepositoryViewModel()
    {
        GlContextManager = new GlContextManager();
        Programs = new ObservableCollection<ProgramViewModel>();
        shaderViewModelsByShader = new Dictionary<Shader, ShaderViewModel>();
        Shaders = new ObservableCollection<ShaderViewModel>();
        OpenShaders = new ObservableCollection<ShaderViewModel>();
        activeOpenShaderIndex = -1;

        AddProgramCommand = new RelayCommand(addProgram);

        AddVertexShaderCommand = new RelayCommand(() => addShader(ProgramStage.Vertex));
        AddGeometryShaderCommand = new RelayCommand(() => addShader(ProgramStage.Geometry));
        AddFragmentShaderCommand = new RelayCommand(() => addShader(ProgramStage.Fragment));

        renameShaderCommand = new RelayCommand(
            () =>
            {
                if (!SelectedShader.Renaming)
                {
                    SelectedShader.Renaming = true;
                }
            },
            isShaderSelected);

        attachSelectedShaderToSelectedProgramCommand = new RelayCommand(
            attachSelectedShaderToSelectedProgram,
            () => isShaderSelected() && isProgramSelected());

        activateSelectedProgramCommand = new RelayCommand(
            () => ActiveProgram = SelectedProgram,
            isProgramSelected);

        var timer = new DispatcherTimer{Interval = TimeSpan.FromMilliseconds(250)};
        timer.Tick += (sender, e) => GlContextManager.ShaderCompiler.PublishValidationResults();
        timer.Start();
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

    private void addShader(ProgramStage stage)
    {
        var shader = new Shader(stage);

        GlContextManager.ShaderCompiler.AddShader(shader);
        var shaderViewModel = new ShaderViewModel(shader);
        shaderViewModelsByShader.Add(shader, shaderViewModel);
        Shaders.Add(shaderViewModel);
    }
    
    private void addProgram()
    {
        var program = new Program();

        GlContextManager.ShaderCompiler.AddProgram(program);
        Programs.Add(new ProgramViewModel(program));
    }

    private void attachSelectedShaderToSelectedProgram()
    {
        attachShaderToProgram(SelectedProgram, SelectedShader);
    }

    private static void attachShaderToProgram(ProgramViewModel program, ShaderViewModel shader)
    {
        var currentAttachedShader = program.GetShaderForStage(shader.Stage);
        if (currentAttachedShader.IsSome)
        {
            program.DetachShader(currentAttachedShader.Value);
        }
        program.AttachShader(shader);
    }

    public void CloseShaderTab(ShaderViewModel shader)
    {
        OpenShaders.Remove(shader);
    }
}

}

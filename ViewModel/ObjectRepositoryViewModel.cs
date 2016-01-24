using ShaderBaker.GlRenderer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.IO;
using ShaderBaker.Serial;
using System.Linq;
using System.Threading;

namespace ShaderBaker.ViewModel
{

class ObjectRepositoryViewModel : ViewModelBase
{
    private const string ProjectFileName = "test.sbp";

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
            GlContextManager.SetActiveProgram(activeProgram.Program);
            OnPropertyChanged("ActiveProgram");
        }
    }

    private readonly RelayCommand openProjectCommand;
    public ICommand OpenProjectCommand => openProjectCommand;

    private readonly RelayCommand saveProjectCommand;
    public ICommand SaveProjectCommand => saveProjectCommand;

    private readonly RelayCommand renameShaderCommand;
    public ICommand RenameShaderCommand => renameShaderCommand;

    private readonly RelayCommand attachSelectedShaderToSelectedProgramCommand;
    public ICommand AttachSelectedShaderToSelectedProgramCommand
        => attachSelectedShaderToSelectedProgramCommand;

    private readonly RelayCommand activateSelectedProgramCommand;
    public ICommand ActivateSelectedProgramCommand => activateSelectedProgramCommand;

    private ImageSource programPreviewImage;
    public ImageSource ProgramPreviewImage
    {
        get { return programPreviewImage; }
        set
        {
            programPreviewImage = value;
            OnPropertyChanged("ProgramPreviewImage");
        }
    }

    public ObjectRepositoryViewModel()
    {
        GlContextManager = new GlContextManager();
        GlContextManager.ImageRendered += ProgramPreviewImageRendered;

        Programs = new ObservableCollection<ProgramViewModel>();
        shaderViewModelsByShader = new Dictionary<Shader, ShaderViewModel>();
        Shaders = new ObservableCollection<ShaderViewModel>();
        OpenShaders = new ObservableCollection<ShaderViewModel>();
        activeOpenShaderIndex = -1;

        openProjectCommand = new RelayCommand(() => OpenProjectFromFile(ProjectFileName));
        saveProjectCommand = new RelayCommand(() => SaveProjectToFile(ProjectFileName));

        AddProgramCommand = new RelayCommand(() => AddProgram(new Program()));

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

    private void addNewShader(ProgramStage stage)
    {
        AddShader(new Shader(stage));
    }

    public void AddShader(Shader shader)
    {
        GlContextManager.ShaderCompiler.AddShader(shader);
        var shaderViewModel = new ShaderViewModel(shader);
        shaderViewModelsByShader.Add(shader, shaderViewModel);
        Shaders.Add(shaderViewModel);
    }

    public void AddNewProgram()
    {
        addProgramViewModel(new ProgramViewModel());
    }

    public void AddProgram(Program program)
    {
        addProgramViewModel(new ProgramViewModel(program, shaderViewModelsByShader));
    }

    private void addProgramViewModel(ProgramViewModel programViewModel)
    {
        GlContextManager.ShaderCompiler.AddProgram(programViewModel.Program);
        Programs.Add(programViewModel);
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

    private void ProgramPreviewImageRendered(ImageSource image)
    {
        Application.Current.Dispatcher.InvokeAsync(() => ProgramPreviewImage = image);
    }

    public void OnResize(Size newSize)
    {
        GlContextManager.ResizePreviewImage((int) newSize.Width, (int) newSize.Height);
    }

    public void OpenProjectFromFile(string projectFileName)
    {
        try
        {
//TODO do the file reading on a different thread from the UI thread
            using (var inputStream = new FileStream(projectFileName, FileMode.Open, FileAccess.Read))
            {
                Shader[] shaders;
                Program[] programs;
                ProjectSerializer.ReadProject(inputStream, out shaders, out programs);

                foreach (var shader in shaders)
                {
                    AddShader(shader);
                }
                foreach (var program in programs)
                {
                    AddProgram(program);
                }
            }
        } catch (FileNotFoundException ex)
        {
            Console.WriteLine(" *** Could not load Shader Baker project file: " + ex.FileName);
        }
    }

    public void SaveProjectToFile(string projectFileName)
    {
//TODO do the file writing on a different thread from the UI thread
        using (var outputStream = new FileStream(projectFileName, FileMode.OpenOrCreate, FileAccess.Write))
        {
            var shaders = Shaders.Select(viewModel => viewModel.Shader).ToArray();
            var programs = Programs.Select(viewModel => viewModel.Program).ToArray();
            ProjectSerializer.WriteProject(outputStream, shaders, programs);
        }
    }
}

}

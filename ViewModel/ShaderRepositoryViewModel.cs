using ShaderBaker.GlRenderer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System;

namespace ShaderBaker.ViewModel
{

public class ShaderRepositoryViewModel
{
    private readonly IDictionary<Shader, ShaderViewModel> shaderViewModelsByShader;
    
    public ObservableCollection<ShaderViewModel> Shaders
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

    public ShaderRepositoryViewModel()
    {
        shaderViewModelsByShader = new Dictionary<Shader, ShaderViewModel>();
        Shaders = new ObservableCollection<ShaderViewModel>();
        AddVertexShaderCommand = new AddShaderCommand(this, ProgramStage.Vertex);
        AddGeometryShaderCommand = new AddShaderCommand(this, ProgramStage.Geometry);
        AddFragmentShaderCommand = new AddShaderCommand(this, ProgramStage.Fragment);
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

    private class AddShaderCommand : ICommand
    {
        private readonly ShaderRepositoryViewModel shaderRepo;

        private readonly ProgramStage stage;

        public event EventHandler CanExecuteChanged;

        public AddShaderCommand(
            ShaderRepositoryViewModel shaderRepo, ProgramStage stage)
        {
            this.shaderRepo = shaderRepo;
            this.stage = stage;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            shaderRepo.addShader(new Shader(stage));
        }
    }
}

}

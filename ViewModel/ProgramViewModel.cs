using ShaderBaker.GlRenderer;
using ShaderBaker.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShaderBaker.ViewModel
{

public class ProgramViewModel : ViewModelBase
{
    private readonly Program program;
    
    private readonly IDictionary<ProgramStage, ShaderViewModel> shadersByStage;
    
    public ObservableCollection<ShaderViewModel> AttachedShaders
    {
        get;
        private set;
    }
    
    public Validity LinkageValidity
    {
        get { return program.LinkageValidity; }
    }

    public Option<string> LinkError
    {
        get { return program.LinkError; }
    }

    public ProgramViewModel()
    {
        program = new Program();
        shadersByStage = new Dictionary<ProgramStage, ShaderViewModel>();
        AttachedShaders = new ObservableCollection<ShaderViewModel>();
    }

    public void AttachShader(ShaderViewModel shaderViewModel)
    {
        Debug.Assert(
            !shadersByStage.ContainsKey(shaderViewModel.Stage),
            "A shader for the " + shaderViewModel.Stage.ToString()
                + " stage is already attached to this program view model");

        shaderViewModel.AttachToProgram(program);
        shadersByStage.Add(shaderViewModel.Stage, shaderViewModel);
    }

    public void DetachShader(ShaderViewModel shaderViewModel)
    {
        shaderViewModel.DetachFromProgram(program);
        bool removed = shadersByStage.Remove(shaderViewModel.Stage);
        Debug.Assert(
            removed,
            "No shader is attached to the " + shaderViewModel.Stage
                + " stage of this program view model");
    }

    private void onLinkageValidityChanged(
        Program sender, Validity oldValidity, Validity newValidity)
    {
        OnPropertyChanged("LinkageValidity");
        OnPropertyChanged("LinkError");
    }
}

}

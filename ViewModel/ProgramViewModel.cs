using ShaderBaker.GlRenderer;
using ShaderBaker.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShaderBaker.ViewModel
{

public class ProgramViewModel : ViewModelBase
{
    public Program Program { get; }

    public string ProgramName
    {
        get { return Program.Name; }
        set
        {
            Program.Name = value;
            OnPropertyChanged("ProgramName");
        }
    }
    
    private readonly IDictionary<ProgramStage, ShaderViewModel> shadersByStage;
    
    public ObservableCollection<ShaderViewModel> AttachedShaders { get; }
    
    public Validity LinkageValidity => Program.LinkageValidity;
    
    public string LinkError =>
        Program.LinkError.IsSome
            ? Program.LinkError.Value.TrimEnd()
            : "";

    public ProgramViewModel(Program program)
    {
        this.Program = program;
        shadersByStage = new Dictionary<ProgramStage, ShaderViewModel>();
        AttachedShaders = new ObservableCollection<ShaderViewModel>();
        program.LinkageValidityChanged += onLinkageValidityChanged;
    }

    public Option<ShaderViewModel> GetShaderForStage(ProgramStage stage)
    {
        ShaderViewModel shader;
        if (shadersByStage.TryGetValue(stage, out shader))
        {
            return Option<ShaderViewModel>.Some(shader);
        } else
        {
            return Option<ShaderViewModel>.None();
        }
    }

    public void AttachShader(ShaderViewModel shaderViewModel)
    {
        Debug.Assert(
            !shadersByStage.ContainsKey(shaderViewModel.Stage),
            "A shader for the " + shaderViewModel.Stage.ToString()
                + " stage is already attached to this program view model");

        shaderViewModel.AttachToProgram(Program);
        shadersByStage.Add(shaderViewModel.Stage, shaderViewModel);
        AttachedShaders.Add(shaderViewModel);
    }

    public void DetachShader(ShaderViewModel shaderViewModel)
    {
        shaderViewModel.DetachFromProgram(Program);
        bool removed = shadersByStage.Remove(shaderViewModel.Stage);
        Debug.Assert(
            removed,
            "No shader is attached to the " + shaderViewModel.Stage
                + " stage of this program view model");

        AttachedShaders.Remove(shaderViewModel);
    }

    private void onLinkageValidityChanged(
        Program sender, Validity oldValidity, Validity newValidity)
    {
        OnPropertyChanged("LinkageValidity");
        OnPropertyChanged("LinkError");
    }
}

}

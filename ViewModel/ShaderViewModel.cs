using ShaderBaker.GlRenderer;
using ShaderBaker.Utilities;

namespace ShaderBaker.ViewModel
{

public class ShaderViewModel : ViewModelBase
{
    public readonly Shader shader;

    public ProgramStage Stage
    {
        get { return shader.Stage; }
    }

    public string Source
    {
        get { return shader.Source; }
        set
        {
            shader.Source = value;
            OnPropertyChanged("Source");
        }
    }

    public Validity SourceValidity
    {
        get { return shader.SourceValidity; }
    }

    public Option<string> CompilationError
    {
        get { return shader.CompilationError; }
    }

    public ShaderViewModel(Shader shader)
    {
        this.shader = shader;
        shader.SourceValidityChanged += onSourceValidityChanged;
    }

    public void AttachToProgram(Program program)
    {
        program.AttachShader(shader);
    }

    public void DetachFromProgram(Program program)
    {
        program.DetachShader(shader);
    }

    private void onSourceValidityChanged(
        Shader sender, Validity oldValidity, Validity newValidity)
    {
        OnPropertyChanged("SourceValidity");
        OnPropertyChanged("CompilationError");
    }
}

}

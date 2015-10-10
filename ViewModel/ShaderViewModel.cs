using ShaderBaker.GlRenderer;
using ShaderBaker.Utilities;

namespace ShaderBaker.ViewModel
{

public class ShaderViewModel : ViewModelBase
{
    public readonly Shader shader;

    public string ShaderName
    {
        get
        {
            return shader.Name;
        }
        set
        {
            shader.Name = value;
            OnPropertyChanged("ShaderName");
        }
    }

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

    private bool renaming;
    public bool Renaming
    {
        get
        {
            return renaming;
        }
        set
        {
            this.renaming = value;
            OnPropertyChanged("Renaming");
        }
    }

    public ShaderViewModel(Shader shader)
    {
        this.shader = shader;
        renaming = false;
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

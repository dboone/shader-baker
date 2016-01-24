using ShaderBaker.GlRenderer;

namespace ShaderBaker.ViewModel
{

public class ShaderViewModel : ViewModelBase
{
    public Shader Shader { get; }

    public string ShaderName
    {
        get
        {
            return Shader.Name;
        }
        set
        {
            Shader.Name = value;
            OnPropertyChanged("ShaderName");
        }
    }

    public ProgramStage Stage => Shader.Stage;

    public string Source
    {
        get { return Shader.Source; }
        set
        {
            Shader.Source = value;
            OnPropertyChanged("Source");
        }
    }

    public Validity SourceValidity => Shader.SourceValidity;

    public string CompilationError =>
        Shader.CompilationError.IsSome
            ? Shader.CompilationError.Value.TrimEnd()
            : "";

    private bool renaming;
    public bool Renaming
    {
        get
        {
            return renaming;
        }
        set
        {
            renaming = value;
            OnPropertyChanged("Renaming");
        }
    }

    public ShaderViewModel(Shader shader)
    {
        Shader = shader;
        renaming = false;
        shader.SourceValidityChanged += onSourceValidityChanged;
    }

    public void DetachFromProgram(Program program)
    {
        program.DetachShader(Shader);
    }

    private void onSourceValidityChanged(
        Shader sender, Validity oldValidity, Validity newValidity)
    {
        OnPropertyChanged("SourceValidity");
        OnPropertyChanged("CompilationError");
    }
}

}

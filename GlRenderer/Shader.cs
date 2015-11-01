using ShaderBaker.Utilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShaderBaker.GlRenderer
{

public sealed class Shader
{
    /// <summary>
    /// The name of the shader
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The shader's stage in the program pipeline
    /// </summary>
    public ProgramStage Stage { get; }
    
    private string source;

    /// <summary>
    /// The source code for this shader
    /// </summary>
    public string Source
    {
        get { return source; }
        set
        {
            source = value;
            raiseSourceChanged();
            ResetSourceValidity();
        }
    }

    /// <summary>
    /// The validity of the shader source code. This represents its compilation status.
    /// </summary>
    public Validity SourceValidity
    {
        get;
        private set;
    }

    /// <summary>
    /// The compilation error. This will only have a value if SourceValidity is Invalid.
    /// </summary>
    public Option<string> CompilationError
    {
        get;
        private set;
    }

    public delegate void SourceValidityChangedHandler(
        Shader sender, Validity oldValidity, Validity newValidity);
    public event SourceValidityChangedHandler SourceValidityChanged;

    public delegate void SourceChangedHandler(Shader sender);
    public event SourceChangedHandler SourceChanged;

    public Shader(ProgramStage stage)
    {
        Name = stage + "Shader";
        Stage = stage;
        Source = "";
    }

    private void raiseSourceChanged()
    {
        var handlers = SourceChanged;
        handlers?.Invoke(this);
    }

    [Conditional("DEBUG")]
    private void assertValidityUnknown()
    {
        if (SourceValidity != Validity.Unknown)
        {
            Debug.Fail("The source validity of this Shader has already been set");
        }
    }

    private void setSourceValidity(Validity newValidity)
    {
        var oldValidity = SourceValidity;
        if (newValidity == oldValidity)
        {
            return;
        }

        SourceValidity = newValidity;
        var handlers = SourceValidityChanged;
        handlers?.Invoke(this, oldValidity, newValidity);
    }
    
    public void ResetSourceValidity()
    {
        CompilationError = Option<string>.empty();
        setSourceValidity(Validity.Unknown);
    }

    public void ValidateSource()
    {
        assertValidityUnknown();

        CompilationError = Option<string>.empty();
        setSourceValidity(Validity.Valid);
    }

    public void InvalidateSource(string compilationError)
    {
        assertValidityUnknown();

        CompilationError = Option<string>.of(compilationError);
        setSourceValidity(Validity.Invalid);
    }
}

}

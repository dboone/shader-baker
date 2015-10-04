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
    public string Name
    {
        get;
        set;
    }

    /// <summary>
    /// The shader's stage in the program pipeline
    /// </summary>
    public readonly ProgramStage Stage;

    /// <summary>
    /// The programs to which this Shader is attached
    /// </summary>
    public readonly ICollection<Program> ParentPrograms;

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

    public Shader(ProgramStage stage)
    {
        Name = stage.ToString() + "Shader";
        Stage = stage;
        ParentPrograms = new List<Program>();
        Source = "";
    }

    private void assertValidityUnknown()
    {
        Debug.Assert(
            SourceValidity == Validity.Unknown,
            "The source validity of this Shader has already been set");
    }

    private void setSourceValidity(Validity newValidity)
    {
        Validity oldValidity = newValidity;
        if (oldValidity != SourceValidity)
        {
            SourceValidityChanged(this, oldValidity, SourceValidity);
        }
    }
        
    public void ResetSourceValidity()
    {
        SourceValidity = Validity.Unknown;
        CompilationError = Option<string>.empty();
        foreach (Program parentProgram in ParentPrograms)
        {
            parentProgram.ResetLinkageValidity();
        }
    }

    public void ValidateSource()
    {
        assertValidityUnknown();

        SourceValidity = Validity.Valid;
    }

    public void InvalidateSource(string compilationError)
    {
        assertValidityUnknown();

        CompilationError = Option<string>.of(compilationError);
        SourceValidity = Validity.Invalid;
    }
}

}

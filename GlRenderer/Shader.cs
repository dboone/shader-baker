using ShaderBaker.Utilities;
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
            resetSourceValidity();
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

    /// <summary>
    /// A field holding the number of modifications made to this shader.
    /// </summary>
    /// <remarks>
    /// This field is useful for coordinating multiple threads operating on this shader. Its value
    /// can be sent as part of a work request to another thread, and when the thread is finished
    /// with the work, its value can be checked before publishing any results. If they are different,
    /// its results should be discarded.
    /// 
    /// It is safe to let this value overflow and wrap around to zero. The probability of this value
    /// overflowing and matching the exact same value as when another thread saved it is negligible.
    /// </remarks>
    public uint ModCount { get; private set; }

    public delegate void SourceChangedHandler(Shader sender);
    public event SourceChangedHandler SourceChanged;

    public delegate void SourceValidityChangedHandler(
        Shader sender, Validity oldValidity, Validity newValidity);
    public event SourceValidityChangedHandler SourceValidityChanged;

    public Shader(ProgramStage stage)
    {
        Name = stage + "Shader";
        Stage = stage;
        source = "";
        SourceValidity = Validity.Unknown;
        CompilationError = Option<string>.None();
        ModCount = 0;
    }
    
    private void raiseSourceChanged()
    {
        ++ModCount;
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
    
    private void resetSourceValidity()
    {
        CompilationError = Option<string>.None();
        setSourceValidity(Validity.Unknown);
    }

    public void ValidateSource()
    {
        assertValidityUnknown();

        CompilationError = Option<string>.None();
        setSourceValidity(Validity.Valid);
    }

    public void InvalidateSource(string compilationError)
    {
        assertValidityUnknown();

        CompilationError = Option<string>.Some(compilationError);
        setSourceValidity(Validity.Invalid);
    }
}

}

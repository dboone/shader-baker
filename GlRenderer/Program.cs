using ShaderBaker.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShaderBaker.GlRenderer
{

public sealed class Program
{
    public string Name { get; set; }

    private IDictionary<ProgramStage, Shader> shadersByStage;

    /// <summary>
    /// The shaders attached to this Program, keyed by their ProgramPipelineStage
    /// </summary>
    public IReadOnlyDictionary<ProgramStage, Shader> ShadersByStage { get; }
    
    /// <summary>
    /// The validity of the program. This represents its link status.
    /// </summary>
    public Validity LinkageValidity
    {
        get;
        private set;
    }
    
    /// <summary>
    /// The link error. This will only have a value if LinkageValidity is Invalid.
    /// </summary>
    public Option<string> LinkError
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
    
    public delegate void ShaderAttachedHandler(Program sender, Shader shader);
    public event ShaderAttachedHandler ShaderAttached;
    
    public delegate void ShaderDetachedHandler(Program sender, Shader shader);
    public event ShaderDetachedHandler ShaderDetached;
    
    public delegate void InputsChangedHandler(Program program);
    public event InputsChangedHandler InputsChanged;
    
    public delegate void LinkageValidityChangedHandler(
        Program sender, Validity oldValidity, Validity newValidity);
    public event LinkageValidityChangedHandler LinkageValidityChanged;

    public Program()
    {
        Name = "Program";
        shadersByStage = new Dictionary<ProgramStage, Shader>();
        ShadersByStage = new ReadOnlyDictionary<ProgramStage, Shader>(shadersByStage);
        LinkageValidity = Validity.Unknown;
        LinkError = Option<string>.None();
        ModCount = 0;
    }

    private void raiseShaderAttached(Shader shader)
    {
        var handlers = ShaderAttached;
        handlers?.Invoke(this, shader);
    }

    private void raiseShaderDetached(Shader shader)
    {
        var handlers = ShaderDetached;
        handlers?.Invoke(this, shader);
    }

    private void raiseInputsChanged()
    {
        var handlers = InputsChanged;
        handlers?.Invoke(this);
    }
    
    private void onAttachedShaderSourceChanged(Shader shader)
    {
        programInputsChanged();
    }

    public void AttachShader(Shader shader)
    {
        Debug.Assert(
            !ShadersByStage.ContainsKey(shader.Stage),
            "A shader for the " + shader.Stage.ToString()
                + " stage is already attached to this program");
                
        shader.SourceChanged += onAttachedShaderSourceChanged;
        shadersByStage.Add(shader.Stage, shader);
        raiseShaderAttached(shader);
        programInputsChanged();
    }

    public void DetachShader(Shader shader)
    {
        bool removed = shadersByStage.Remove(
            new KeyValuePair<ProgramStage, Shader>(shader.Stage, shader));
        Debug.Assert(
            removed,
            "No shader is attached to the "
                + shader.Stage.ToString() + " stage of this program");
                
        shader.SourceChanged -= onAttachedShaderSourceChanged;
        raiseShaderDetached(shader);
        programInputsChanged();
    }

    private void assertValidityUnknown()
    {
        Debug.Assert(
            LinkageValidity == Validity.Unknown,
            "The linkage validity of this Program has already been set");
    }

    private void setLinkageValidity(Validity newValidity)
    {
        var oldValidity = LinkageValidity;
        if (newValidity == oldValidity)
        {
            return;
        }

        LinkageValidity = newValidity;
        var handlers = LinkageValidityChanged;
        handlers?.Invoke(this, oldValidity, newValidity);
    }

    private void programInputsChanged()
    {
        ++ModCount;
        LinkError = Option<string>.None();
        raiseInputsChanged();
        setLinkageValidity(Validity.Unknown);
    }

    public void ValidateProgramLinkage()
    {
        assertValidityUnknown();
        
        LinkError = Option<string>.None();
        setLinkageValidity(Validity.Valid);
    }

    public void InvalidateProgramLinkage(string linkError)
    {
        assertValidityUnknown();

        LinkError = Option<string>.Some(linkError);
        setLinkageValidity(Validity.Invalid);
    }
}

}

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
    
    public delegate void ShaderAttachedHandler(Program sender, Shader shader);
    public event ShaderAttachedHandler ShaderAttached;
    
    public delegate void ShaderDetachedHandler(Program sender, Shader shader);
    public event ShaderDetachedHandler ShaderDetached;
    
    public delegate void LinkageValidityChangedHandler(
        Program sender, Validity oldValidity, Validity newValidity);
    public event LinkageValidityChangedHandler LinkageValidityChanged;

    public Program()
    {
        Name = "Program";
        shadersByStage = new Dictionary<ProgramStage, Shader>();
        ShadersByStage = new ReadOnlyDictionary<ProgramStage, Shader>(shadersByStage);
        ResetLinkageValidity();
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
    
    private void onAttachedShaderSourceValidityChanged(
        Shader sender, Validity oldValidity, Validity newValidity)
    {
        ResetLinkageValidity();
    }

    public void AttachShader(Shader shader)
    {
        Debug.Assert(
            !ShadersByStage.ContainsKey(shader.Stage),
            "A shader for the " + shader.Stage.ToString()
                + " stage is already attached to this program");
                
        shadersByStage.Add(shader.Stage, shader);
            shader.SourceValidityChanged += onAttachedShaderSourceValidityChanged;
        raiseShaderAttached(shader);
        ResetLinkageValidity();
    }

    public void DetachShader(Shader shader)
    {
        bool removed = shadersByStage.Remove(
            new KeyValuePair<ProgramStage, Shader>(shader.Stage, shader));
        Debug.Assert(
            removed,
            "No shader is attached to the "
                + shader.Stage.ToString() + " stage of this program");
                
        shader.SourceValidityChanged -= onAttachedShaderSourceValidityChanged;
        raiseShaderDetached(shader);
        ResetLinkageValidity();
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

    public void ResetLinkageValidity()
    {
        LinkError = Option<string>.empty();
        setLinkageValidity(Validity.Unknown);
    }

    public void ValidateProgramLinkage()
    {
        assertValidityUnknown();
        
        LinkError = Option<string>.empty();
        setLinkageValidity(Validity.Valid);
    }

    public void InvalidateProgramLinkage(string linkError)
    {
        assertValidityUnknown();

        LinkError = Option<string>.of(linkError);
        setLinkageValidity(Validity.Invalid);
    }
}

}

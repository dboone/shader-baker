using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShaderBaker.GlRenderer
{

public sealed class ShaderCompiler : IGlCache
{
    private readonly ISet<Shader> shaders
        = new HashSet<Shader>(new IdentityEqualityComparer<Shader>());

    private readonly IDictionary<Shader, uint> glShaderCache
        = new Dictionary<Shader, uint>(new IdentityEqualityComparer<Shader>());

    private IDictionary<Shader, CompileInputs> shadersToCompile
        = new Dictionary<Shader, CompileInputs>(new IdentityEqualityComparer<Shader>());
    
    private IDictionary<Shader, ValidationResult> validationResults
        = new Dictionary<Shader, ValidationResult>(new IdentityEqualityComparer<Shader>());

    public void AddShader(Shader shader)
    {
        Debug.Assert(shader != null, "shader cannot be null");

        var added = shaders.Add(shader);
        Debug.Assert(added, "Shader has already been added to this validator");
        
        recompileShader(shader);
        shader.SourceChanged += recompileShader;
    }

    private void recompileShader(Shader shader)
    {
        lock (this)
        {
            shadersToCompile[shader] = new CompileInputs(shader.Source);
        }
    }

    public void RemoveShader(Shader shader)
    {
        Debug.Assert(shader != null, "shader cannot be null");

        var removed = shaders.Remove(shader);
        Debug.Assert(removed, "Shader was never added to this validator");
        
        shader.SourceChanged -= recompileShader;
    }

    public void ValidateShaders(OpenGL gl)
    {
        IDictionary<Shader, CompileInputs> shadersToCompile;
        lock (this)
        {
            shadersToCompile = this.shadersToCompile;
            this.shadersToCompile = new Dictionary<Shader, CompileInputs>(shadersToCompile.Count);
        }
        
        // The set of shaders to compile is traversed in two passes for superior OpenGL interoperation.
        // Since getting the shader info log requires waiting for the compile to finish, all compilation
        // commands are submitted before any shader logs are queried. This increases the chance that
        // a shader has finished compiling by the time its log is read.

        foreach (var pair in shadersToCompile)
        {
            var shader = pair.Key;
            var shaderSource = pair.Value.SourceToValidate;

            uint glShaderHandle;
            if (!glShaderCache.TryGetValue(shader, out glShaderHandle))
            {
                glShaderHandle = gl.CreateShader(shader.Stage.GlEnumValue());
                glShaderCache.Add(shader, glShaderHandle);
            }
            
            gl.ShaderSource(glShaderHandle, shaderSource);
            gl.CompileShader(glShaderHandle);
            // after a GLSL shader is compiled, OpenGL does not need to keep the source
            // any more, so set it to an empty string to free up memory in the driver
            gl.ShaderSource(glShaderHandle, string.Empty);
        }
        
        foreach (var pair in shadersToCompile)
        {
            var shader = pair.Key;
            var shaderSource = pair.Value.SourceToValidate;

            uint glShaderHandle = glShaderCache[shader];
            var compileError = ShaderUtilities.GetShaderInfoLog(gl, glShaderHandle);
            lock (this)
            {
                validationResults[shader]
                    = new ValidationResult(shaderSource, compileError);
            }
        }
    }

    public void PublishValidationResults()
    {
        IDictionary<Shader, ValidationResult> validationResults;
        lock (this)
        {
            validationResults = this.validationResults;
            this.validationResults = new Dictionary<Shader, ValidationResult>(validationResults.Count);
        }

        foreach (var pair in validationResults)
        {
            var shader = pair.Key;
            var result = pair.Value;
            if (result.ValidatedSource == shader.Source)
            {
                var error = result.ValidationError;
                if (error.hasValue())
                {
                    shader.InvalidateSource(error.get());
                } else
                {
                    shader.ValidateSource();
                }
            }
        }
    }

    public void ClearCache(OpenGL gl)
    {
        foreach (var glShaderHandle in glShaderCache.Values)
        {
            gl.DeleteShader(glShaderHandle);
        }
        glShaderCache.Clear();
    }

    private struct CompileInputs
    {
        public readonly string SourceToValidate;
        
        public CompileInputs(string sourceToValidate)
        {
            SourceToValidate = sourceToValidate;
        }
    }

    private struct ValidationResult
    {
        public readonly string ValidatedSource;

        public readonly Option<string> ValidationError;

        public ValidationResult(
            string validatedSource,
            Option<string> validationError)
        {
            ValidatedSource = validatedSource;
            ValidationError = validationError;
        }
    }
}

}

using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShaderBaker.GlRenderer
{

public sealed class ShaderCompiler
{
    private readonly ISet<Shader> shaders
        = new HashSet<Shader>(new IdentityEqualityComparer<Shader>());
    
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
        
        foreach (var pair in shadersToCompile)
        {
            var shader = pair.Key;
            var shaderSource = pair.Value.SourceToValidate;
            var glShaderHandle = gl.CreateShader(shader.Stage.GlEnumValue());
            gl.ShaderSource(glShaderHandle, shaderSource);
            gl.CompileShader(glShaderHandle);
            var compileError = ShaderUtilities.GetShaderInfoLog(gl, glShaderHandle);
            lock (this)
            {
                validationResults[shader]
                    = new ValidationResult(shaderSource, compileError);
            }

            gl.DeleteShader(glShaderHandle);
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

using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShaderBaker.GlRenderer
{

/// <summary>
/// A class for compiling GLSL shaders through an OpenGL context.
/// </summary>
/// <remarks>
/// This class has some a special threading model. It is designed to work with
/// two threads, an Application Thread which manages the application state, and
/// an OpenGL Thread which communicates with OpenGL. The OpenGL Thread must have
/// a current OpenGL context. The methods AddShader, RemoveShader, and
/// PublishValidationResults should only be called from the Application Thread.
/// The methods ValidateShaders and ClearCache should only be called from the
/// OpenGL thread.
/// 
/// This threading model is to simplify the management of OpenGL objects, such
/// that they are used with the correct context. It also provides a simple model
/// for exchanging information between OpenGL and the application.
/// </remarks>
public sealed class ShaderCompiler : IGlCache
{
    private readonly ISet<Shader> shaders
        = new HashSet<Shader>(new IdentityEqualityComparer<Shader>());

    private readonly IDictionary<Shader, uint> glShaderCache
        = new Dictionary<Shader, uint>(new IdentityEqualityComparer<Shader>());

// The shadersToCompile and validationResults fields follow a double-buffering pattern
// to ensure thread-safety. When an element is added to one of these collections, a
// mutex is acquired, the element added, and then the mutex is released. When the collection
// is operated on, the same mutex is acquired; a reference to the collection is copied to a
// local variable; a newly-allocated, empty collection is assigned to the field; and the mutex
// is released. This pattern minimizes thread communication and the time a mutex is held.
//
// These collections also store immutable values, which can be safely exchanged across threads.

    private IDictionary<Shader, ValidationInputs> shadersToValidate
        = new Dictionary<Shader, ValidationInputs>(new IdentityEqualityComparer<Shader>());
    
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
            shadersToValidate[shader] = new ValidationInputs(shader.Source);
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
        IDictionary<Shader, ValidationInputs> localShadersToValidate;
        lock (this)
        {
            localShadersToValidate = shadersToValidate;
            shadersToValidate = new Dictionary<Shader, ValidationInputs>(localShadersToValidate.Count);
        }
        
        // The set of shaders to compile is traversed in two passes for superior OpenGL interoperation.
        // Since getting the shader info log requires waiting for the compile to finish, all compilation
        // commands are submitted before any shader logs are queried. This increases the chance that
        // a shader has finished compiling by the time its log is read.

        foreach (var pair in localShadersToValidate)
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
        
        foreach (var pair in localShadersToValidate)
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
        IDictionary<Shader, ValidationResult> localValidationResults;
        lock (this)
        {
            localValidationResults = validationResults;
            validationResults = new Dictionary<Shader, ValidationResult>(localValidationResults.Count);
        }

        foreach (var pair in localValidationResults)
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

    private struct ValidationInputs
    {
        public readonly string SourceToValidate;
        
        public ValidationInputs(string sourceToValidate)
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

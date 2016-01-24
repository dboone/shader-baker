using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
public sealed class ShaderCompiler
{
    private static ISet<T> IdentitySet<T>() where T : class
    {
        return new HashSet<T>(new IdentityEqualityComparer<T>());
    }

    private static IDictionary<K, V> IdentityMap<K, V>() where K : class
    {
        return new Dictionary<K, V>(new IdentityEqualityComparer<K>());
    }

    private static IDictionary<K, V> IdentityMap<K, V>(int capacity) where K : class
    {
        return new Dictionary<K, V>(capacity, new IdentityEqualityComparer<K>());
    }
    
// The shadersToCompile and validationResults fields follow a double-buffering pattern
// to ensure thread-safety. When an element is added to one of these collections, a
// mutex is acquired, the element added, and then the mutex is released. When the collection
// is operated on, the same mutex is acquired; a reference to the collection is copied to a
// local variable; a newly-allocated, empty collection is assigned to the field; and the mutex
// is released. This pattern minimizes thread communication and the time a mutex is held.
//
// These collections also store immutable values, which can be safely exchanged across threads.
        
    private readonly ISet<Shader> shaders = IdentitySet<Shader>();

    private IDictionary<Shader, ShaderValidationInputs> shadersToValidate
        = IdentityMap<Shader, ShaderValidationInputs>();
    
    private IDictionary<Shader, ShaderValidationResult> shaderValidationResults
        = IdentityMap<Shader, ShaderValidationResult>();
        
    private readonly ISet<Program> programs = IdentitySet<Program>();

    private IDictionary<Program, ProgramValidationInputs> programsToValidate
        = IdentityMap<Program, ProgramValidationInputs>();
    
    private IDictionary<Program, ProgramValidationResult> programValidationResults
        = IdentityMap<Program, ProgramValidationResult>();

    private void submitShaderForValidation(Shader shader)
    {
        lock (this)
        {
            shadersToValidate[shader] = new ShaderValidationInputs(shader);
        }
    }
    
    public void AddShader(Shader shader)
    {
        Debug.Assert(shader != null, "shader cannot be null");

        var added = shaders.Add(shader);
        Debug.Assert(added, "Shader has already been added to this validator");
        
        submitShaderForValidation(shader);
        shader.SourceChanged += submitShaderForValidation;
    }

    public void RemoveShader(Shader shader)
    {
        Debug.Assert(shader != null, "shader cannot be null");

        var removed = shaders.Remove(shader);
        Debug.Assert(removed, "Shader was never added to this validator");
        
        shader.SourceChanged -= submitShaderForValidation;
    }

    private void onProgramInputsChanged(Program program)
    {
        submitProgramForValidation(program);
    }

    private void submitProgramForValidation(Program program)
    {
        lock (this)
        {
            programsToValidate[program] = new ProgramValidationInputs(program);
        }
    }

    public void AddProgram(Program program)
    {
        Debug.Assert(program != null, "Cannot add a null program");

        var added = programs.Add(program);
        Debug.Assert(added, "Program has already been added to this validator");

        submitProgramForValidation(program);
        program.InputsChanged += onProgramInputsChanged;
    }

    public void RemoveProgram(Program program)
    {
        Debug.Assert(program != null, "Cannot remove a null program");

        var removed = programs.Remove(program);
        Debug.Assert(removed, "Program was never added to this validator");
        
        program.InputsChanged -= onProgramInputsChanged;
    }

    public void ValidateShaders(OpenGL gl)
    {
        IDictionary<Shader, ShaderValidationInputs> localShadersToValidate;
        IDictionary<Program, ProgramValidationInputs> localProgramsToValidate;
        lock (this)
        {
            localShadersToValidate = shadersToValidate;
            localProgramsToValidate = programsToValidate;
            shadersToValidate = IdentityMap<Shader, ShaderValidationInputs>(localShadersToValidate.Count);
            programsToValidate = IdentityMap<Program, ProgramValidationInputs>(localProgramsToValidate.Count);
        }
        
        foreach (var pair in localShadersToValidate)
        {
            var shaderInputs = pair.Value;

            var shaderHandle = gl.CreateShader(shaderInputs.Stage.GlEnumValue());
            
            gl.ShaderSource(shaderHandle, shaderInputs.SourceToValidate);
            gl.CompileShader(shaderHandle);
            var compileError = ShaderUtilities.GetShaderInfoLog(gl, shaderHandle);
            gl.DeleteShader(shaderHandle);
            
            var shader = pair.Key;
            var result = new ShaderValidationResult(
                shaderInputs.Shader, shaderInputs.ModCount, compileError);
            lock (this)
            {
                shaderValidationResults[shader] = result;
            }
        }

        foreach (var pair in localProgramsToValidate)
        {
            var programInputs = pair.Value;
            
            var programHandle = gl.CreateProgram();

            var shaderHandles = new List<uint>(programInputs.AttachedShaders.Count);
            var failedShaders = new List<ProgramStage>(programInputs.AttachedShaders.Count);
            foreach (var shaderInputs in programInputs.AttachedShaders)
            {
                var shaderHandle = gl.CreateShader(shaderInputs.Stage.GlEnumValue());
                shaderHandles.Add(shaderHandle);

                gl.ShaderSource(shaderHandle, shaderInputs.SourceToValidate);
                gl.CompileShader(shaderHandle);
                var compileError = ShaderUtilities.GetShaderInfoLog(gl, shaderHandle);
                if (compileError.IsSome)
                {
                    failedShaders.Add(shaderInputs.Stage);
                }
                
                gl.AttachShader(programHandle, shaderHandle);
            }

            Option<string> linkError;
            if (failedShaders.Count == 0)
            {
                gl.LinkProgram(programHandle);
                linkError = ProgramUtilities.GetLinkStatus(gl, programHandle);
            } else
            {
                var errorMessage = failedShaders
                    .Select(stage => "Attached " + stage + " shader has an error")
                    .Aggregate(new StringBuilder(), (current, next) => current.Append("\n").Append(next))
                    .ToString();
                linkError = Option<string>.Some(errorMessage);
            }

            gl.DeleteProgram(programHandle);

            foreach (var shaderHandle in shaderHandles)
            {
                // no need to call glDetachShader - the program has already been deleted
                gl.DeleteShader(shaderHandle);
            }
            
            var program = pair.Key;
            var result = new ProgramValidationResult(
                programInputs.Program, programInputs.ModCount, linkError);
            lock (this)
            {
                programValidationResults[program] = result;
            }
        }
    }

    public void PublishValidationResults()
    {
        IDictionary<Shader, ShaderValidationResult> localShaderValidationResults;
        IDictionary<Program, ProgramValidationResult> localProgramValidationResults;
        lock (this)
        {
            localShaderValidationResults = shaderValidationResults;
            localProgramValidationResults = programValidationResults;
            shaderValidationResults = new Dictionary<Shader, ShaderValidationResult>(localShaderValidationResults.Count);
            programValidationResults = new Dictionary<Program, ProgramValidationResult>(localProgramValidationResults.Count);
        }

        foreach (var result in localShaderValidationResults.Values)
        {
            if (result.ModCount == result.Shader.ModCount)
            {
                var error = result.ValidationError;
                if (error.IsSome)
                {
                    result.Shader.InvalidateSource(error.Value);
                } else
                {
                    result.Shader.ValidateSource();
                }
            }
        }

        foreach (var result in localProgramValidationResults.Values)
        {
            if (result.ModCount == result.Program.ModCount)
            {
                var error = result.ValidationError;
                if (error.IsSome)
                {
                    result.Program.InvalidateProgramLinkage(error.Value);
                } else
                {
                    result.Program.ValidateProgramLinkage();
                }
            }
        }
    }

    private struct ShaderValidationInputs
    {
        public readonly Shader Shader;

        public readonly ProgramStage Stage;

        public readonly string SourceToValidate;

        public readonly uint ModCount;

        public ShaderValidationInputs(Shader shader)
        {
            Shader = shader;
            Stage = shader.Stage;
            SourceToValidate = shader.Source;
            ModCount = shader.ModCount;
        }
    }

    private struct ShaderValidationResult
    {
        public readonly Shader Shader;

        public readonly uint ModCount;

        public readonly Option<string> ValidationError;

        public ShaderValidationResult(
            Shader shader, uint modCount, Option<string> validationError)
        {
            Shader = shader;
            ModCount = modCount;
            ValidationError = validationError;
        }
    }

    private struct ProgramValidationInputs
    {
        public readonly Program Program;

        public readonly IList<ShaderValidationInputs> AttachedShaders;

        public readonly uint ModCount;

        public ProgramValidationInputs(Program program)
        {
            Program = program;
            AttachedShaders = program.ShadersByStage.Values
                .Select(shader => new ShaderValidationInputs(shader))
                .ToList();
            ModCount = program.ModCount;
        }
    }

    private struct ProgramValidationResult
    {
        public readonly Program Program;

        public readonly uint ModCount;

        public readonly Option<string> ValidationError;

        public ProgramValidationResult(
            Program program, uint modCount, Option<string> validationError)
        {
            Program = program;
            ModCount = modCount;
            ValidationError = validationError;
        }
    }
}

}

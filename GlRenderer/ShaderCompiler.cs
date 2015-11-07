using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

    private readonly IDictionary<Shader, ShaderData> shaderDataCache
        = new Dictionary<Shader, ShaderData>(new IdentityEqualityComparer<Shader>());

// The shadersToCompile and validationResults fields follow a double-buffering pattern
// to ensure thread-safety. When an element is added to one of these collections, a
// mutex is acquired, the element added, and then the mutex is released. When the collection
// is operated on, the same mutex is acquired; a reference to the collection is copied to a
// local variable; a newly-allocated, empty collection is assigned to the field; and the mutex
// is released. This pattern minimizes thread communication and the time a mutex is held.
//
// These collections also store immutable values, which can be safely exchanged across threads.

    private IDictionary<Shader, ShaderValidationInputs> shadersToValidate
        = new Dictionary<Shader, ShaderValidationInputs>(new IdentityEqualityComparer<Shader>());
    
    private IDictionary<Shader, ShaderValidationResult> shaderValidationResults
        = new Dictionary<Shader, ShaderValidationResult>(new IdentityEqualityComparer<Shader>());
        
    private readonly ISet<Program> programs
        = new HashSet<Program>(new IdentityEqualityComparer<Program>());

    private readonly IDictionary<Program, uint> glProgramCache
        = new Dictionary<Program, uint>(new IdentityEqualityComparer<Program>());

    private IDictionary<Program, ProgramValidationInputs> programsToValidate
        = new Dictionary<Program, ProgramValidationInputs>(new IdentityEqualityComparer<Program>());
    
    private IDictionary<Program, ProgramValidationResult> programValidationResults
        = new Dictionary<Program, ProgramValidationResult>(new IdentityEqualityComparer<Program>());

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

    [Conditional("DEBUG")]
    private void checkShaderAdded(Shader shader)
    {
        if (!shaders.Contains(shader))
        {
            Debug.Fail("A shader must be added to the validator before it can be attached to the program");
        }
    }

    private void onShaderAttachedToProgram(Program program, Shader shader)
    {
        submitProgramForValidation(program);
    }

    private void onShaderDetachedFromProgram(Program program, Shader shader)
    {
        submitProgramForValidation(program);
    }

    private void onProgramLinkageValidityChanged(
        Program program, Validity oldValidity, Validity newValidity)
    {
        if (newValidity == Validity.Unknown)
        {
            submitProgramForValidation(program);
        }
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
        Debug.Assert(program.ShadersByStage.Count == 0, "Cannot add a program that already has shaders attached");

        var added = programs.Add(program);
        Debug.Assert(added, "Program has already been added to this validator");

        submitProgramForValidation(program);
        program.ShaderAttached += onShaderAttachedToProgram;
        program.ShaderDetached += onShaderDetachedFromProgram;
        program.LinkageValidityChanged += onProgramLinkageValidityChanged;
    }

    public void RemoveProgram(Program program)
    {
        Debug.Assert(program != null, "Cannot remove a null program");

        var removed = programs.Remove(program);
        Debug.Assert(removed, "Program was never added to this validator");
        
        program.ShaderAttached -= onShaderAttachedToProgram;
        program.ShaderDetached -= onShaderDetachedFromProgram;
        program.LinkageValidityChanged -= onProgramLinkageValidityChanged;
    }

    private ShaderData getDataForShader(OpenGL gl, Shader shader)
    {
        ShaderData shaderData;
        if (!shaderDataCache.TryGetValue(shader, out shaderData))
        {
            shaderData = new ShaderData();
            shaderDataCache.Add(shader, shaderData);
        }

        if (!shaderData.HandleValid)
        {
            shaderData.ShaderHandle = gl.CreateShader(shader.Stage.GlEnumValue());
            shaderData.HandleValid = true;
        }

        return shaderData;
    }

    public void ValidateShaders(OpenGL gl)
    {
        IDictionary<Shader, ShaderValidationInputs> localShadersToValidate;
        IDictionary<Program, ProgramValidationInputs> localProgramsToValidate;
        lock (this)
        {
            localShadersToValidate = shadersToValidate;
            localProgramsToValidate = programsToValidate;
            shadersToValidate = new Dictionary<Shader, ShaderValidationInputs>(localShadersToValidate.Count);
            programsToValidate = new Dictionary<Program, ProgramValidationInputs>(localProgramsToValidate.Count);
        }
        
        // The set of shaders to compile is traversed in two passes for superior OpenGL interoperation.
        // Since getting the shader info log requires waiting for the compile to finish, all compilation
        // commands are submitted before any shader logs are queried. This increases the chance that
        // a shader has finished compiling by the time its log is read.
        
        foreach (var pair in localShadersToValidate)
        {
            var shader = pair.Key;
            var shaderSource = pair.Value.SourceToValidate;

            var shaderData = getDataForShader(gl, shader);
            shaderData.Source = shaderSource;
            shaderData.Validity = Validity.Unknown;
        }
        
        foreach (var inputs in localProgramsToValidate.Values)
        {
            foreach (var shader in inputs.AttachedShaders)
            {
                getDataForShader(gl, shader);
            }
        }

        foreach (var shaderData in shaderDataCache.Values)
        {
            if (shaderData.Validity == Validity.Unknown)
            {
                gl.ShaderSource(shaderData.ShaderHandle, shaderData.Source);
                gl.CompileShader(shaderData.ShaderHandle);
                // after a GLSL shader is compiled, OpenGL does not need to keep the source
                // any more, so set it to an empty string to free up memory
                gl.ShaderSource(shaderData.ShaderHandle, string.Empty);
            }
        }
        
        foreach (var pair in shaderDataCache)
        {
            var shader = pair.Key;
            var shaderData = pair.Value;

            if (shaderData.Validity == Validity.Unknown)
            {
                var compileError = ShaderUtilities.GetShaderInfoLog(gl, shaderData.ShaderHandle);
                shaderData.Validity = compileError.hasValue() ? Validity.Invalid : Validity.Valid;
                
                if (localShadersToValidate.ContainsKey(shader))
                {
                    lock (this)
                    {
                        shaderValidationResults[shader]
                            = new ShaderValidationResult(shaderData.Source, compileError);
                    }
                }
            }
        }

        var invalidPrograms = new Dictionary<Program, string>();

        foreach (var pair in localProgramsToValidate)
        {
            var program = pair.Key;
            var attachedShaders = pair.Value.AttachedShaders;
            
            uint glProgramHandle;
            if (!glProgramCache.TryGetValue(program, out glProgramHandle))
            {
                glProgramHandle = gl.CreateProgram();
                glProgramCache.Add(program, glProgramHandle);
            }

            var attachedShadersData = attachedShaders
                .Select(shader => Tuple.Create(shader, shaderDataCache[shader]))
                .ToArray();

            bool anyShaderValidityUnknown = attachedShadersData
                .Where(data => data.Item2.Validity == Validity.Unknown)
                .Any();

            Debug.Assert(!anyShaderValidityUnknown, "One or more shaders attached to this program were not compiled");

            var invalidShaders = attachedShadersData
                .Where(data => data.Item2.Validity == Validity.Invalid)
                .Select(data => "Attached " + data.Item1.Stage + " shader has an error")
                .ToArray();

            if (invalidShaders.Length > 0)
            {
                invalidPrograms.Add(program, string.Join("\n", invalidShaders));
            } else
            {
                foreach (var shaderData in attachedShadersData)
                {
                    gl.AttachShader(glProgramHandle, shaderData.Item2.ShaderHandle);
                }

                gl.LinkProgram(glProgramHandle);

                foreach (var shaderData in attachedShadersData)
                {
                    gl.DetachShader(glProgramHandle, shaderData.Item2.ShaderHandle);
                }
            }
        }
        
        foreach (var pair in localProgramsToValidate)
        {
            var program = pair.Key;
            Option<string> linkError;
            string attachedProgramErrors;
            if (invalidPrograms.TryGetValue(program, out attachedProgramErrors))
            {
                linkError = Option<string>.of(attachedProgramErrors);
            } else
            {

                uint glProgramHandle = glProgramCache[program];
                linkError = ProgramUtilities.GetLinkStatus(gl, glProgramHandle);
            }

            var attachedShaders = pair.Value.AttachedShaders;
            lock (this)
            {
                programValidationResults[program]
                    = new ProgramValidationResult(attachedShaders, linkError);
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

        foreach (var pair in localShaderValidationResults)
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

        foreach (var pair in localProgramValidationResults)
        {
            var program = pair.Key;
            var result = pair.Value;
            
            var error = result.ValidationError;
            if (error.hasValue())
            {
                program.InvalidateProgramLinkage(error.get());
            } else
            {
                program.ValidateProgramLinkage();
            }
        }
    }

    public void ClearCache(OpenGL gl)
    {
        foreach (var glProgramHandle in glProgramCache.Values)
        {
            gl.DeleteProgram(glProgramHandle);
        }
        glProgramCache.Clear();

        foreach (var shaderData in shaderDataCache.Values)
        {
            gl.DeleteShader(shaderData.ShaderHandle);
            shaderData.HandleValid = false;
        }
    }

    private struct ProgramValidationInputs
    {
        public readonly IList<Shader> AttachedShaders;

        public ProgramValidationInputs(Program program)
        {
            AttachedShaders = new List<Shader>(program.ShadersByStage.Values);
        }
    }

    private struct ProgramValidationResult
    {
        public readonly IList<Shader> ValidatedShaders;

        public readonly Option<string> ValidationError;

        public ProgramValidationResult(
            IList<Shader> validatedShaders, Option<string> validationError)
        {
            ValidatedShaders = validatedShaders;
            ValidationError = validationError;
        }
    }

    private struct ShaderValidationInputs
    {
        public readonly string SourceToValidate;
        
        public ShaderValidationInputs(Shader shader)
        {
            SourceToValidate = shader.Source;
        }
    }

    private struct ShaderValidationResult
    {
        public readonly string ValidatedSource;

        public readonly Option<string> ValidationError;

        public ShaderValidationResult(
            string validatedSource, Option<string> validationError)
        {
            ValidatedSource = validatedSource;
            ValidationError = validationError;
        }
    }

    private class ShaderData
    {
        public bool HandleValid;

        public uint ShaderHandle;

        public string Source;

        public Validity Validity;

        public ShaderData()
        {
            HandleValid = false;
            Source = "";
            Validity = Validity.Unknown;
        }
    }
}

}

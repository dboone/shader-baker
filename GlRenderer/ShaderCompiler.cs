﻿using ShaderBaker.GlUtilities;
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
        Debug.Assert(program.ShadersByStage.Count == 0, "Cannot add a program that already has shaders attached");

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
            var inputs = pair.Value;

            var shaderData = getDataForShader(gl, shader);
            shaderData.Source = inputs.SourceToValidate;
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
                shaderData.Validity = compileError.IsSome ? Validity.Invalid : Validity.Valid;
                
                ShaderValidationInputs inputs;
                if (localShadersToValidate.TryGetValue(shader, out inputs))
                {
                    var result = new ShaderValidationResult(inputs.ModCount, compileError);
                    lock (this)
                    {
                        shaderValidationResults[shader] = result;
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

        foreach (var pair in localShadersToValidate)
        {
            var shader = pair.Key;
            var inputs = pair.Value;

            var shaderData = shaderDataCache[shader];

        }
        
        foreach (var pair in localProgramsToValidate)
        {
            var program = pair.Key;
            var inputs = pair.Value;

            Option<string> linkError;
            string attachedProgramErrors;
            if (invalidPrograms.TryGetValue(program, out attachedProgramErrors))
            {
                linkError = Option<string>.Some(attachedProgramErrors);
            } else
            {

                uint glProgramHandle = glProgramCache[program];
                linkError = ProgramUtilities.GetLinkStatus(gl, glProgramHandle);
            }
            
            var result = new ProgramValidationResult(inputs.ModCount, linkError);
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

        foreach (var pair in localShaderValidationResults)
        {
            var shader = pair.Key;
            var result = pair.Value;

            if (result.ModCount == shader.ModCount)
            {
                var error = result.ValidationError;
                if (error.IsSome)
                {
                    shader.InvalidateSource(error.Value);
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
            
            if (result.ModCount == program.ModCount)
            {
                var error = result.ValidationError;
                if (error.IsSome)
                {
                    program.InvalidateProgramLinkage(error.Value);
                } else
                {
                    program.ValidateProgramLinkage();
                }
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

        public readonly uint ModCount;

        public ProgramValidationInputs(Program program)
        {
            AttachedShaders = new List<Shader>(program.ShadersByStage.Values);
            ModCount = program.ModCount;
        }
    }

    private struct ProgramValidationResult
    {
        public readonly uint ModCount;

        public readonly Option<string> ValidationError;

        public ProgramValidationResult(uint modCount, Option<string> validationError)
        {
            ModCount = modCount;
            ValidationError = validationError;
        }
    }

    private struct ShaderValidationInputs
    {
        public readonly string SourceToValidate;

        public readonly uint ModCount;
        
        public ShaderValidationInputs(Shader shader)
        {
            SourceToValidate = shader.Source;
            ModCount = shader.ModCount;
        }
    }

    private struct ShaderValidationResult
    {
        public readonly uint ModCount;

        public readonly Option<string> ValidationError;

        public ShaderValidationResult(uint modCount, Option<string> validationError)
        {
            ModCount = modCount;
            ValidationError = validationError;
        }
    }

    private class ShaderData
    {
        public bool HandleValid = false;

        public uint ShaderHandle;

        public string Source = "";

        public Validity Validity = Validity.Unknown;
    }
}

}

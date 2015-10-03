using ShaderBaker.GlRenderer;
using ShaderBaker.GlRenderer.Task;
using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;

namespace GlRenderer.ShaderBaker
{

public class ShaderCompiler
{
    private readonly GlContextManager glContextManager;
    private readonly SingletonGlTask compileVertexShaderTask;
    private readonly SingletonGlTask compileFragmentShaderTask;

    private uint renderProgramHandle;
    private uint compileProgramHandle;

    private uint vertexShaderHandle;
    private uint fragmentShaderHandle;
    
    private volatile string _vertexShaderSource;
    public string VertexShaderSource
    {
        get { return _vertexShaderSource; }
        set
        {
            _vertexShaderSource = value;
            compileVertexShaderTask.Submit(
                new CompileVertexShaderTask(
                    new CompileShaderHelper(this, vertexShaderHandle, value)));
        }
    }
    
    private volatile string _fragmentShaderSource;
    public string FragmentShaderSource
    {
        get { return _fragmentShaderSource; }
        set
        {
            _fragmentShaderSource = value;
            compileFragmentShaderTask.Submit(
                new CompileFragmentShaderTask(
                    new CompileShaderHelper(this, fragmentShaderHandle, value)));
        }
    }
        
    public delegate void VertexShaderCompiledEventHandler(ShaderCompiler sender, uint shaderHandle, Option<string> compileStatus);
    public event VertexShaderCompiledEventHandler VertexShaderCompiled;

    public delegate void FragmentShaderCompiledEventHandler(ShaderCompiler sender, uint shaderHandle, Option<string> compileStatus);
    public event FragmentShaderCompiledEventHandler FragmentShaderCompiled;

    public delegate void ProgramLinkedEventHandler(ShaderCompiler sender, Option<string> linkStatus);
    public event ProgramLinkedEventHandler ProgramLinked;

    public ShaderCompiler(OpenGL gl, GlContextManager glContextManager)
    {
        this.glContextManager = glContextManager;
        this.compileVertexShaderTask = glContextManager.CreateSingletonGlTask();
        this.compileFragmentShaderTask = glContextManager.CreateSingletonGlTask();

        compileProgramHandle = gl.CreateProgram();
        renderProgramHandle = gl.CreateProgram();
        
        vertexShaderHandle = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
        fragmentShaderHandle = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);

        gl.AttachShader(compileProgramHandle, vertexShaderHandle);
        gl.AttachShader(compileProgramHandle, fragmentShaderHandle);

        gl.AttachShader(renderProgramHandle, vertexShaderHandle);
        gl.AttachShader(renderProgramHandle, fragmentShaderHandle);

        VertexShaderSource = 
              "#version 330\n"
            + "\n"
            + "void main()\n"
            + "{\n"
            + "    gl_Position = vec4(0.0, 0.0, 0.0, 1.0);\n"
            + "}\n";

        FragmentShaderSource =
             "#version 330\n"
            + "\n"
            + "out vec4 color;"
            + "\n"
            + "void main()\n"
            + "{\n"
            + "    color = vec4(1.0, 1.0, 1.0, 1.0);\n"
            + "}\n";
    }

    private Option<string> recompileVertexShader(OpenGL gl)
    {
        return recompileShader(gl, vertexShaderHandle, _vertexShaderSource);
    }

    private Option<string> recompileFragmentShader(OpenGL gl)
    {
        return recompileShader(gl, fragmentShaderHandle, _fragmentShaderSource);
    }

    private static Option<string> recompileShader(OpenGL gl, uint shaderHandle, string shaderSource)
    {
        gl.ShaderSource(shaderHandle, shaderSource);
        gl.CompileShader(shaderHandle);
        return ShaderUtilities.GetShaderInfoLog(gl, shaderHandle);
    }

    private static Option<string> relinkProgram(OpenGL gl, uint programHandle)
    {
        gl.LinkProgram(programHandle);

        Option<string> linkStatus = ProgramUtilities.GetLinkStatus(gl, programHandle);
        if (linkStatus.hasValue())
        {
            return linkStatus;
        }

        Option<string> validateStatus = ProgramUtilities.Validate(gl, programHandle);
        if (validateStatus.hasValue())
        {
            return validateStatus;
        }

        return Option<string>.empty();
    }

    private void swapRenderProgram()
    {
        uint tmp = compileProgramHandle;
        compileProgramHandle = renderProgramHandle;
        renderProgramHandle = tmp;
    }

    public void UseRenderProgram(OpenGL gl)
    {
        gl.UseProgram(renderProgramHandle);
    }

    public void DisposeGlObjects(OpenGL gl)
    {
        gl.DeleteProgram(renderProgramHandle);
        gl.DeleteProgram(compileProgramHandle);
        gl.DeleteProgram(vertexShaderHandle);
        gl.DeleteProgram(fragmentShaderHandle);
        
        renderProgramHandle = 0;
        compileProgramHandle = 0;
        vertexShaderHandle = 0;
        fragmentShaderHandle = 0;
    }

    private class CompileShaderHelper
    {
        public readonly ShaderCompiler shaderCompiler;

        public readonly uint shaderHandle;

        public readonly string shaderSource;

        public CompileShaderHelper(
            ShaderCompiler shaderCompiler, uint shaderHandle, string shaderSource)
        {
            this.shaderCompiler = shaderCompiler;
            this.shaderHandle = shaderHandle;
            this.shaderSource = shaderSource;
        }

        public Option<string> Execute(OpenGL gl)
        {
            Option<string> compileStatus = recompileShader(gl, shaderHandle, shaderSource);
            if (!compileStatus.hasValue())
            {
                shaderCompiler.glContextManager.SubmitGlTask(new LinkProgramTask(shaderCompiler));
            }
            return compileStatus;
        }
    }

    private class CompileVertexShaderTask : IGlTask
    {
        private readonly CompileShaderHelper helper;

        public CompileVertexShaderTask(CompileShaderHelper helper)
        {
            this.helper = helper;
        }

        public void Execute(OpenGL gl)
        {
            Option<string> compileStatus = helper.Execute(gl);
            helper.shaderCompiler.VertexShaderCompiled(
                helper.shaderCompiler,
                helper.shaderHandle,
                compileStatus);
        }
    }

    private class CompileFragmentShaderTask : IGlTask
    {
        private readonly CompileShaderHelper helper;

        public CompileFragmentShaderTask(CompileShaderHelper helper)
        {
            this.helper = helper;
        }

        public void Execute(OpenGL gl)
        {
            Option<string> compileStatus = helper.Execute(gl);
            helper.shaderCompiler.FragmentShaderCompiled(
                helper.shaderCompiler,
                helper.shaderHandle,
                compileStatus);
        }
    }

    private class LinkProgramTask : IGlTask
    {
        private readonly ShaderCompiler shaderCompiler;

        public LinkProgramTask(ShaderCompiler shaderCompiler)
        {
            this.shaderCompiler = shaderCompiler;
        }

        public void Execute(OpenGL gl)
        {
            Option<string> linkStatus = relinkProgram(gl, shaderCompiler.compileProgramHandle);
            if (!linkStatus.hasValue())
            {
                shaderCompiler.swapRenderProgram();
            }
            shaderCompiler.ProgramLinked(shaderCompiler, linkStatus);
        }
    }
}

}

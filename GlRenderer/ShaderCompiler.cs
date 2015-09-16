using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;

namespace GlRenderer.ShaderBaker
{

public class ShaderCompiler
{
// putting volatile on the fields in this class doesn't
// make it 100% thread safe, but it is better than nothing

    private volatile string _vertexShaderSource;
    public string VertexShaderSource
    {
        get { return _vertexShaderSource; }
        set
        {
            _vertexShaderSource = value;
            updateVertexShader = true;
        }
    }

    private volatile bool updateVertexShader;
    
    private volatile string _fragmentShaderSource;
    public string FragmentShaderSource
    {
        get { return _fragmentShaderSource; }
        set
        {
            _fragmentShaderSource = value;
            updateFragmentShader = true;
        }
    }

    private volatile bool updateFragmentShader;

    private uint renderProgramHandle;
    private uint compileProgramHandle;

    private uint vertexShaderHandle;
    private uint fragmentShaderHandle;

    public ShaderCompiler(OpenGL gl)
    {
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

    private Option<string> recompileShader(OpenGL gl, uint shaderHandle, string shaderSource)
    {
        gl.ShaderSource(shaderHandle, shaderSource);
        gl.CompileShader(shaderHandle);
        return Shader.GetShaderInfoLog(gl, shaderHandle);
    }

    private Option<string> relinkProgram(OpenGL gl, uint programHandle)
    {
        gl.LinkProgram(programHandle);

        Option<string> linkStatus = Program.GetLinkStatus(gl, programHandle);
        if (linkStatus.hasValue())
        {
            return linkStatus;
        }

        Option<string> validateStatus = Program.Validate(gl, programHandle);
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

    public Option<string> RelinkProgramIfStale(OpenGL gl)
    {
        bool updateProgram = false;
        if (updateVertexShader)
        {
            updateVertexShader = false;
            Option<string> compileStatus = recompileVertexShader(gl);
            if (compileStatus.hasValue())
            {
                return compileStatus;
            }
            updateProgram  = true;
        }

        if (updateFragmentShader)
        {
            updateFragmentShader = false;
            Option<string> compileStatus = recompileFragmentShader(gl);
            if (compileStatus.hasValue())
            {
                return compileStatus;
            }
            updateProgram  = true;
        }

        if (updateProgram)
        {
            Option<string> linkStatus = relinkProgram(gl, compileProgramHandle);
            
            if (!linkStatus.hasValue())
            {
                swapRenderProgram();
            }

            return linkStatus;
        } else
        {
            return Option<string>.empty();
        }
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
}

}

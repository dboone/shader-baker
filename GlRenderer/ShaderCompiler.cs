using ShaderBaker.GlUtilities;
using ShaderBaker.Utilities;
using SharpGL;

namespace GlRenderer.ShaderBaker
{

public class ShaderCompiler
{
    private uint renderProgramHandle;
    private uint compileProgramHandle;

    private uint vertexShaderHandle;
    private uint fragmentShaderHandle;

    private bool renderProgramStale;

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
    }
    
    public Option<string> RecompileVertexShader(OpenGL gl, string shaderSource)
    {
        return recompileShader(gl, vertexShaderHandle, shaderSource);
    }

    public Option<string> RecompileFragmentShader(OpenGL gl, string shaderSource)
    {
        return recompileShader(gl, fragmentShaderHandle, shaderSource);
    }

    private Option<string> recompileShader(OpenGL gl, uint shaderHandle, string shaderSource)
    {
        gl.ShaderSource(shaderHandle, shaderSource);
        gl.CompileShader(shaderHandle);
        Option<string> compileStatus = Shader.GetShaderInfoLog(gl, shaderHandle);
        if (!compileStatus.hasValue())
        {
            renderProgramStale = true;
        }
        return compileStatus;
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
        if (renderProgramStale)
        {
            Option<string> linkStatus = relinkProgram(gl, compileProgramHandle);
            
            if (!linkStatus.hasValue())
            {
                swapRenderProgram();
            }

            renderProgramStale = false;
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

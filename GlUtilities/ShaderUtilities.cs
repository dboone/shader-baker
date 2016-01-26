using ShaderBaker.Utilities;
using SharpGL;
using System;
using System.Text;

namespace ShaderBaker.GlUtilities
{

public static class ShaderUtilities
{
    public static Option<string> GetShaderInfoLog(OpenGL gl, uint shaderHandle)
    {   
        int[] status = new int[1];
        gl.GetShader(shaderHandle, OpenGL.GL_COMPILE_STATUS, status);
        if (status[0] == OpenGL.GL_FALSE)
        {
            int[] logLength = new int[1];
            gl.GetShader(shaderHandle, OpenGL.GL_INFO_LOG_LENGTH, logLength);

            StringBuilder log = new StringBuilder(logLength[0]);
            gl.GetShaderInfoLog(shaderHandle, logLength[0], IntPtr.Zero, log);
            return Option<string>.Some(log.ToString());
        } else
        {
            return Option<string>.None();
        }
    }
}

}

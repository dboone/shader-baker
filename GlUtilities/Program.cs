using ShaderBaker.Utilities;
using SharpGL;
using System;
using System.Text;

namespace ShaderBaker.GlUtilities
{

public static class Program
{
    public static Option<string> GetLinkStatus(OpenGL gl, uint programHandle)
    {
        int[] status = new int[1];
        gl.GetProgram(programHandle, OpenGL.GL_LINK_STATUS, status);
        if (status[0] == OpenGL.GL_FALSE)
        {
            string log = getProgramInfoLog(gl, programHandle);
            return Option<string>.of(log);
        } else
        {
            return Option<string>.empty();
        }
    }

    public static Option<string> Validate(OpenGL gl, uint programHandle)
    {
        gl.ValidateProgram(programHandle);

        int[] status = new int[1];
        gl.GetProgram(programHandle, OpenGL.GL_VALIDATE_STATUS, status);
        if (status[0] == OpenGL.GL_FALSE)
        {
            string log = getProgramInfoLog(gl, programHandle);
            return Option<string>.of(log);
        } else
        {
            return Option<string>.empty();
        }
    }
        
    private static string getProgramInfoLog(OpenGL gl, uint programHandle)
    {
        int[] logLength = new int[1];
        gl.GetProgram(programHandle, OpenGL.GL_INFO_LOG_LENGTH, logLength);

        StringBuilder log = new StringBuilder(logLength[0]);
        gl.GetProgramInfoLog(programHandle, logLength[0], IntPtr.Zero, log);

        return log.ToString();
    }
}

}

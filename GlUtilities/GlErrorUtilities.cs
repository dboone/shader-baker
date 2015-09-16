using SharpGL;
using System.Collections.Generic;

namespace ShaderBaker.GlUtilities
{

public static class GlErrorUtilities
{
    public static void ClearGlErrors(OpenGL gl)
    {
        while (gl.GetError() != OpenGL.GL_NO_ERROR);
    }

    public static uint NextGlError(OpenGL gl)
    {
        return gl.GetError();
    }

    public static ICollection<uint> GetGlErrors(OpenGL gl)
    {
        ICollection<uint> errors = new List<uint>();
        while (true)
        {
            uint error = gl.GetError();
            if (error == OpenGL.GL_NO_ERROR)
            {
                return errors;
            }
            errors.Add(error);
        }
    }
}

}

using System;
using SharpGL;

namespace ShaderBaker.GlRenderer
{

public enum ProgramStage
{
    Vertex,
    Geometry,
    Fragment
}

public static class ProgramPipelineStageMethods
{
    public static uint GlEnumValue(this ProgramStage stage)
    {
        switch (stage)
        {
        case ProgramStage.Vertex:
            return OpenGL.GL_VERTEX_SHADER;
        case ProgramStage.Geometry:
            return OpenGL.GL_GEOMETRY_SHADER;
        case ProgramStage.Fragment:
            return OpenGL.GL_FRAGMENT_SHADER;
        default:
            throw new NotImplementedException(
                "Invalid shader stage: " + stage);
        }
    }
}

}

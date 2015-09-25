using SharpGL;

namespace ShaderBaker.GlRenderer
{

public enum ProgramStage : uint
{
    Vertex = OpenGL.GL_VERTEX_SHADER,
    Geometry = OpenGL.GL_GEOMETRY_SHADER,
    Fragment = OpenGL.GL_FRAGMENT_SHADER
}

public static class ProgramPipelineStageMethods
{
    public static uint GlEnumValue(this ProgramStage stage)
    {
        return (uint) stage;
    }
}

}

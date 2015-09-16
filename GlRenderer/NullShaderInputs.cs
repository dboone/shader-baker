using SharpGL;

namespace ShaderBaker.GlRenderer
{

public class NullShaderInputs
{
    private uint vaoHandle;

    public NullShaderInputs(OpenGL gl)
    {
        uint[] vao = new uint[1];
        gl.GenVertexArrays(vao.Length, vao);
        vaoHandle = vao[0];
    }

    public void Prepare(OpenGL gl)
    {
         gl.BindVertexArray(vaoHandle);
    }

    public void DisposeOpenGlObjects(OpenGL gl)
    {
        gl.DeleteVertexArrays(1, new uint[] { vaoHandle });
        vaoHandle = 0;
    }
}

}

using SharpGL;

namespace ShaderBaker.GlRenderer
{

interface IGlCache
{
    /// <summary>
    /// Delete any OpenGL obejcts in this class, and remove any references to them.
    /// </summary>
    /// <remarks>
    /// This must be called from the same thread in which OpenGL objects in the
    /// cache are created.
    /// </remarks>
    /// <param name="gl">An OpenGL context used to delete objects</param>
    void ClearCache(OpenGL gl);
}

}

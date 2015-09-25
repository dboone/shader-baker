using SharpGL;

namespace ShaderBaker.GlRenderer.Task
{

/// <summary>
/// A task to be submitted to <see cref="GlTaskQueue"/> for
/// exeuction on an OpenGL thread.
/// </summary>
/// <remarks>
/// Since these tasks are likely to be shared across multiple
/// threads, it is recommended that these tasks be made immutable
/// to guarantee their thread-safety.
/// </remarks>
public interface IGlTask
{
    void Execute(OpenGL gl);
}

}

using SharpGL;
using SharpGL.Version;
using System;
using System.Threading;

namespace ShaderBaker.GlRenderer
{

/// <summary>
/// Manages an OpenGL context on a dedicated thread. This
/// guarantees that all OpenGL calls are executed on the
/// same thread.
/// </summary>
public sealed class GlContextManager
{
    private readonly CancellationTokenSource cancelTokenSource;
    private readonly Thread glThread;

    public ShaderCompiler ShaderCompiler { get; }

    public GlContextManager()
    {
        cancelTokenSource = new CancellationTokenSource();
        glThread = new Thread(runGlThread);
        ShaderCompiler = new ShaderCompiler();
    }

    public void Start()
    {
        glThread.Start(new Tuple<CancellationToken>(cancelTokenSource.Token));
    }

    private void runGlThread(object args)
    {
        var inputs = (Tuple<CancellationToken>) args;

        var cancelToken = inputs.Item1;
        var gl = new OpenGL();

        if (!createRenderContext(gl, 1, 1))
        {
            Console.WriteLine("*** Unable to create OpenGL Render Context");
        }

        while (!cancelToken.IsCancellationRequested)
        {
            ShaderCompiler.ValidateShaders(gl);
        }
    }
        
    public void Stop()
    {
        cancelTokenSource.Cancel();
        cancelTokenSource.Dispose();
    }

    private static bool createRenderContext(OpenGL gl, int width, int height)
    {
        return gl.Create(
            OpenGLVersion.OpenGL3_3,
            RenderContextType.FBO,
            width,
            height,
            32,
            null);
    }
}

}

using SharpGL;
using SharpGL.RenderContextProviders;
using SharpGL.Version;
using SharpGL.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShaderBaker.GlRenderer
{

/// <summary>
/// Manages an OpenGL context on a dedicated thread. This helps guarantee
/// that all OpenGL calls are executed on the same thread.
/// </summary>
public sealed class GlContextManager
{
    private static readonly Program NullProgram = new Program();

    private readonly CancellationTokenSource cancelTokenSource;
    private readonly Thread glThread;

    public ShaderCompiler ShaderCompiler { get; }

    private Program activeProgram;
    private ActiveProgramValues activeProgramValues;

    private int renderWidth, renderHeight;
    
    public delegate void ImageRenderedHandler(ImageSource image);
    public event ImageRenderedHandler ImageRendered;

    public GlContextManager()
    {
        cancelTokenSource = new CancellationTokenSource();
        glThread = new Thread(runGlThread);
        ShaderCompiler = new ShaderCompiler();
        activeProgram = NullProgram;
        renderWidth = 1;
        renderHeight = 1;
    }

    public void ClearActiveProgram()
    {
        SetActiveProgram(NullProgram);
    }

    public void SetActiveProgram(Program program)
    {
        activeProgram.LinkageValidityChanged -= activeProgramValidityChanged;
        activeProgram = program;
        program.LinkageValidityChanged += activeProgramValidityChanged;
        resetActiveProgramValues();
    }

    public void ResizePreviewImage(int width, int height)
    {
        lock (this)
        {
            renderWidth = Math.Max(1, width);
            renderHeight = Math.Max(1, height);
        }
    }

    private void activeProgramValidityChanged(Program sender, Validity oldValidity, Validity newValidity)
    {
       resetActiveProgramValues();
    }

    private void resetActiveProgramValues()
    {
        lock (this)
        {
            activeProgramValues = new ActiveProgramValues(activeProgram);
        }
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
        
        int localRenderWidth = 1;
        int localRenderHeight = 1;
        if (!createRenderContext(gl, localRenderWidth, localRenderHeight))
        {
//TODO better error handling here
            Console.WriteLine("*** Unable to create OpenGL Render Context");
            return;
        }
        
        uint activeVaoHandle;
        uint activeGlProgramHandle;
        initGLObjects(gl, out activeVaoHandle, out activeGlProgramHandle);

        ActiveProgramValues localActiveProgramValues = null;
        while (!cancelToken.IsCancellationRequested)
        {
            bool resizeRenderContext;
            lock (this)
            {
                if (!ReferenceEquals(localActiveProgramValues, activeProgramValues))
                {
                    localActiveProgramValues = activeProgramValues;
                    if (localActiveProgramValues.Valid)
                    {
                        linkProgram(gl, localActiveProgramValues, activeGlProgramHandle);
                    } else
                    {
                        // Leave the old program running. This prevents the user from seeing
                        // a black screen while they are in the process of modifying a shader.
                    }
                }

                resizeRenderContext = localRenderWidth != renderWidth || localRenderHeight != renderHeight;
                localRenderWidth = renderWidth;
                localRenderHeight = renderHeight;
            }

            if (resizeRenderContext)
            {
                localActiveProgramValues = null;
                deleteGlObjects(gl, activeVaoHandle, activeGlProgramHandle);
                if (!createRenderContext(gl, localRenderWidth, localRenderHeight))
                {
//TODO better error handling here
                    Console.WriteLine("*** Unable to resize OpenGL Render Context");
                    return;
                }
                initGLObjects(gl, out activeVaoHandle, out activeGlProgramHandle);
            }

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);
            gl.BindVertexArray(activeVaoHandle);
            gl.UseProgram(activeGlProgramHandle);
            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);

            // do some other stuff while the image is rendering, to give it a chance to finish
            ShaderCompiler.ValidateShaders(gl);
            
            var provider = gl.RenderContextProvider as FBORenderContextProvider;
            Debug.Assert(provider != null, "Render context provider is not an FBO renderer");
//TODO this call to blit will probably block. Find a better way to copy the image to CPU memory.
            gl.Blit(IntPtr.Zero);
            var hBitmap = provider.InternalDIBSection.HBitmap;

            if (hBitmap != IntPtr.Zero)
            {
                var bitmap = GetFormattedBitmapSource(hBitmap);
                // the bitmap needs to be frozen in order to share it between threads
                bitmap.Freeze();
                ImageRendered(bitmap);
            }
        }

        deleteGlObjects(gl, activeVaoHandle, activeGlProgramHandle);
    }

    private void initGLObjects(OpenGL gl, out uint vaoHandle, out uint programHandle)
    {
        uint[] vaos = new uint[1];
        gl.GenVertexArrays(1, vaos);
        vaoHandle = vaos[0];

        programHandle = gl.CreateProgram();
    }

    private void deleteGlObjects(OpenGL gl, uint vaoHandle, uint programHandle)
    {
        gl.DeleteVertexArrays(1, new uint[]{vaoHandle});
        gl.DeleteProgram(programHandle);
    }

    private void linkProgram(OpenGL gl, ActiveProgramValues programValues, uint glProgramHandle)
    {
//TODO add debug assertions in case a shader fails to compile, or a program fails to link
        var glShaderHandles = new List<uint>(programValues.AttachedShaderValues.Count);
        foreach (var shaderValues in programValues.AttachedShaderValues)
        {
            var glShaderHandle = gl.CreateShader(shaderValues.Stage.GlEnumValue());
            glShaderHandles.Add(glShaderHandle);

            gl.ShaderSource(glShaderHandle, shaderValues.Source);
            gl.CompileShader(glShaderHandle);
                
            gl.AttachShader(glProgramHandle, glShaderHandle);
        }

        gl.LinkProgram(glProgramHandle);

        foreach (var glShaderHandle in glShaderHandles)
        {
            gl.DetachShader(glProgramHandle, glShaderHandle);
            gl.DeleteShader(glShaderHandle);
        }
    }

    /// <summary>
    /// This method converts the output from the OpenGL render context provider to a 
    /// FormatConvertedBitmap in order to show it in the image.
    /// </summary>
    /// <param name="hBitmap">The handle of the bitmap from the OpenGL render context.</param>
    /// <returns>Returns the new format converted bitmap.</returns>
    private static FormatConvertedBitmap GetFormattedBitmapSource(IntPtr hBitmap)
    {
        var newFormatedBitmapSource = new FormatConvertedBitmap();
        newFormatedBitmapSource.BeginInit();
        newFormatedBitmapSource.Source = BitmapConversion.HBitmapToBitmapSource(hBitmap);
//TODO We have to remove the alpha channel - for some reason it
// comes out as 0.0 meaning the drawing comes out transparent.
        newFormatedBitmapSource.DestinationFormat = PixelFormats.Rgb24;
        newFormatedBitmapSource.EndInit();

        return newFormatedBitmapSource;
    }
    
    public void Stop()
    {
        cancelTokenSource.Cancel();
        cancelTokenSource.Dispose();
    }

    public void StopAndWait()
    {
        Stop();
        glThread.Join();
    }

    private bool createRenderContext(OpenGL gl, int width, int height)
    {
        return gl.Create(
            OpenGLVersion.OpenGL3_3,
            RenderContextType.FBO,
            width,
            height,
            32,
            null);
    }

    private class ActiveProgramValues
    {
        public readonly ICollection<ActiveShaderValues> AttachedShaderValues;

        public readonly bool Valid;

        public ActiveProgramValues(Program program)
        {
            AttachedShaderValues = program.ShadersByStage.Values
                .Select(shader => new ActiveShaderValues(shader)).ToList();
            Valid = program.LinkageValidity == Validity.Valid;
        }
    }

    private struct ActiveShaderValues
    {
        public readonly ProgramStage Stage;

        public readonly string Source;

        public ActiveShaderValues(Shader shader)
        {
            Stage = shader.Stage;
            Source = shader.Source;
        }
    }
}

}

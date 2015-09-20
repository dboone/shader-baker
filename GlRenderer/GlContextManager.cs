using ShaderBaker.GlRenderer.Task;
using ShaderBaker.GlUtilities;
using SharpGL;
using SharpGL.RenderContextProviders;
using SharpGL.Version;
using SharpGL.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShaderBaker.GlRenderer
{
    /// <summary>
    /// Manages an OpenGL context on a dedicated thread. This
    /// guarantees that all OpenGL calls are executed on the
    /// same thread.
    /// </summary>
    public class GlContextManager
    {
        private readonly CancellationTokenSource cancelTokenSource;
        private readonly Thread glThread;
        private readonly GlTaskQueue glTaskQueue;
        private readonly SingletonGlTask resizeTask;
        
        public delegate void RenderHandler(OpenGL gl);
        public event RenderHandler Render;
        
        public delegate void RenderCompleteHandler(ImageSource image);
        public event RenderCompleteHandler RenderComplete;

        public GlContextManager()
        {
            cancelTokenSource = new CancellationTokenSource();
            glThread = new Thread(runGlThread);
            glTaskQueue = new GlTaskQueue();
            resizeTask = new SingletonGlTask(glTaskQueue);
        }

        public void SubmitGlTask(IGlTask task)
        {
            glTaskQueue.SubmitTask(task);
        }

        public SingletonGlTask CreateSingletonGlTask()
        {
            return new SingletonGlTask(glTaskQueue);
        }

        public void Start()
        {
            var cancelTokenSource = new CancellationTokenSource();
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
                glTaskQueue.ExecuteTasks(gl);
                render(gl);
                Thread.Sleep(500);
            }
        }

        private void render(OpenGL gl)
        {
            Render(gl);

            var provider = gl.RenderContextProvider as FBORenderContextProvider;
            Debug.Assert(provider != null, "Render context provider is not an FBO renderer");



// according to our research, this call does absolutely nothing
//            gl.Blit(IntPtr.Zero);



            var hBitmap = provider.InternalDIBSection.HBitmap;

            if (hBitmap != IntPtr.Zero)
            {
                RenderComplete(GetFormatedBitmapSource(hBitmap));
            }
        }

        /// <summary>
        /// This method converts the output from the OpenGL render context provider to a 
        /// FormatConvertedBitmap in order to show it in the image.
        /// </summary>
        /// <param name="hBitmap">The handle of the bitmap from the OpenGL render context.</param>
        /// <returns>Returns the new format converted bitmap.</returns>
        private static FormatConvertedBitmap GetFormatedBitmapSource(IntPtr hBitmap)
        {
            //TODO: We have to remove the alpha channel - for some reason it
            // comes out as 0.0  meaning the drawing comes out transparent.

            var newFormatedBitmapSource = new FormatConvertedBitmap();
            newFormatedBitmapSource.BeginInit();
            newFormatedBitmapSource.Source = BitmapConversion.HBitmapToBitmapSource(hBitmap);
            newFormatedBitmapSource.DestinationFormat = PixelFormats.Rgb24;
            newFormatedBitmapSource.EndInit();

            return newFormatedBitmapSource;
        }
        
        public void Stop()
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
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

        public void ResizeRenderContext(int width, int height)
        {
            resizeTask.Submit(new ResizeRenderContextTask(width, height));
        }

        private class ResizeRenderContextTask : IGlTask
        {
            private int width, height;

            public ResizeRenderContextTask(int width, int height)
            {
                this.width = width;
                this.height = height;
            }

            public void Execute(OpenGL gl)
            {
                gl.SetDimensions(width, height);
                gl.Viewport(0, 0, width, height);
            }
        }
    }
}

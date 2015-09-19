using SharpGL;
using SharpGL.RenderContextProviders;
using SharpGL.SceneGraph;
using SharpGL.Version;
using SharpGL.WPF;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ShaderBaker.View
{
    public partial class OpenGlControl : UserControl
    {
        private readonly OpenGL gl;
        
        private readonly DispatcherTimer timer;

        /// <summary>
        /// Occurs when OpenGL should be initialised.
        /// </summary>
        [Description("Called when OpenGL has been initialized."), Category("SharpGL")]
        public event OpenGLEventHandler OpenGLInitialized;

        /// <summary>
        /// Occurs when OpenGL drawing should occur.
        /// </summary>
        [Description("Called whenever OpenGL drawing can should occur."), Category("SharpGL")]
        public event OpenGLEventHandler OpenGLDraw;
        
        private static readonly DependencyProperty RenderContextTypeProperty =
          DependencyProperty.Register(
              "RenderContextType",
              typeof(RenderContextType),
              typeof(OpenGlControl),
              new PropertyMetadata(
                  RenderContextType.DIBSection,
                  new PropertyChangedCallback(OnRenderContextTypeChanged)));
        
        public RenderContextType RenderContextType
        {
            get { return (RenderContextType) GetValue(RenderContextTypeProperty); }
            set { SetValue(RenderContextTypeProperty, value); }
        }
        
        private static void OnRenderContextTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            OpenGlControl me = o as OpenGlControl;
        }

        private static readonly DependencyProperty OpenGLVersionProperty =
          DependencyProperty.Register(
              "OpenGLVersion",
              typeof(OpenGLVersion),
              typeof(OpenGlControl),
              new PropertyMetadata(OpenGLVersion.OpenGL2_1));
        
        public OpenGLVersion OpenGLVersion
        {
            get { return (OpenGLVersion) GetValue(OpenGLVersionProperty); }
            set { SetValue(OpenGLVersionProperty, value); }
        }
        
        public OpenGL OpenGL
        {
            get;
        }
        
        public OpenGlControl()
        {
            InitializeComponent();

            gl = new OpenGL();
            timer = new DispatcherTimer();

            Unloaded += OpenGLControl_Unloaded;
            Loaded += OpenGLControl_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event of the OpenGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> Instance containing the event data.</param>
        private void OpenGLControl_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SizeChanged += OpenGLControl_SizeChanged;

            UpdateOpenGLControl((int) RenderSize.Width, (int) RenderSize.Height);

            //  DispatcherTimer setup
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        /// <summary>
        /// Handles the Unloaded event of the OpenGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> Instance containing the event data.</param>
        private void OpenGLControl_Unloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SizeChanged -= OpenGLControl_SizeChanged;

            timer.Stop();
            timer.Tick -= timer_Tick;
        }

        /// <summary>
        /// Handles the SizeChanged event of the OpenGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> Instance containing the event data.</param>
        void OpenGLControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateOpenGLControl((int) e.NewSize.Width, (int) e.NewSize.Height);
        }

        /// <summary>
        /// This method is used to set the dimensions and the viewport of the opengl control.
        /// </summary>
        /// <param name="width">The width of the OpenGL drawing area.</param>
        /// <param name="height">The height of the OpenGL drawing area.</param>
        private void UpdateOpenGLControl(int width, int height)
        {
            // Lock on OpenGL.
            lock (gl)
            {
                gl.SetDimensions(width, height);
                
                gl.Viewport(0, 0, width, height);
            }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or 
        /// internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            lock (gl)
            {
                gl.Create(OpenGLVersion, RenderContextType, 1, 1, 32, null);
            }

            OpenGLInitialized(this, new OpenGLEventArgs(gl));
        }

        /// <summary>
        /// Handles the Tick event of the timer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void timer_Tick(object sender, EventArgs e)
        {
            lock (gl)
            {
                gl.MakeCurrent();
                
                OpenGLDraw(this, new OpenGLEventArgs(gl));

                //  Render.
                gl.Blit(IntPtr.Zero);

                switch (RenderContextType)
                {
                    case RenderContextType.DIBSection:
                    {
                        var provider = gl.RenderContextProvider as DIBSectionRenderContextProvider;
                        var hBitmap = provider.DIBSection.HBitmap;

                        if (hBitmap != IntPtr.Zero)
                        {
                            var newFormatedBitmapSource = GetFormatedBitmapSource(hBitmap);

                            //  Copy the pixels over.
                            image.Source = newFormatedBitmapSource;
                        }
                        break;
                    }
                    case RenderContextType.FBO:
                    {
                        var provider = gl.RenderContextProvider as FBORenderContextProvider;
                        var hBitmap = provider.InternalDIBSection.HBitmap;

                        if (hBitmap != IntPtr.Zero)
                        {
                            var newFormatedBitmapSource = GetFormatedBitmapSource(hBitmap);

                            //  Copy the pixels over.
                            image.Source = newFormatedBitmapSource;
                        }
                        break;
                    }
                    case RenderContextType.NativeWindow:
                    case RenderContextType.HiddenWindow:
                    default:
                        break;
                }
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
            //  TODO: We have to remove the alpha channel - for some reason it comes out as 0.0 
            //  meaning the drawing comes out transparent.

            FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();
            newFormatedBitmapSource.BeginInit();
            newFormatedBitmapSource.Source = BitmapConversion.HBitmapToBitmapSource(hBitmap);
            newFormatedBitmapSource.DestinationFormat = PixelFormats.Rgb24;
            newFormatedBitmapSource.EndInit();

            return newFormatedBitmapSource;
        }
    }
}

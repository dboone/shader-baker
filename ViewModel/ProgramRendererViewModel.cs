using GlRenderer.ShaderBaker;
using ShaderBaker.GlRenderer;
using ShaderBaker.GlRenderer.Task;
using ShaderBaker.Utilities;
using SharpGL;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ShaderBaker.ViewModel
{
    class ProgramRendererViewModel : ViewModelBase
    {
        private readonly string VERTEX_SHADER_1_SOURCE =
              "#version 330\n"
            + "\n"
            + "void main()\n"
            + "{\n"
            + "    vec2 vertices[3] = vec2[3](\n"
            + "        vec2(-0.5, -0.5),\n"
            + "        vec2(0.5, -0.5),\n"
            + "        vec2(0.0, 0.5));\n"
            + "    gl_Position = vec4(vertices[gl_VertexID], 0.0, 1.0);\n"
            + "}\n";

        private readonly string VERTEX_SHADER_2_SOURCE =
              "#version 330\n"
            + "\n"
            + "void main()\n"
            + "{\n"
            + "    vec2 vertices[3] = vec2[3](\n"
            + "        vec2(-0.5, 0.5),\n"
            + "        vec2(0.0, -0.5),\n"
            + "        vec2(0.5, 0.5));\n"
            + "    gl_Position = vec4(vertices[gl_VertexID], 0.0, 1.0);\n"
            + "}\n";

        private ShaderCompiler shaderCompiler;
        private NullShaderInputs programInputs;

        private readonly GlContextManager glContextManager;
        
        private ImageSource renderedImage;
        public ImageSource RenderedImage
        {
            get { return renderedImage; }

            private set
            {
                renderedImage = value;
                OnPropertyChanged("RenderedImage");
            }
        }

        private readonly Dispatcher uiDispatcher;

        public ProgramRendererViewModel()
        {
            uiDispatcher = Application.Current.Dispatcher;
            glContextManager = new GlContextManager();
            glContextManager.RenderComplete += onRenderComplete;
            glContextManager.Render += onRender;
        }

        public void Initialize()
        {
            glContextManager.SubmitGlTask(new InitializeTask(this));
            glContextManager.Start();
        }

        private class InitializeTask : IGlTask
        {
            private readonly ProgramRendererViewModel rendererView;

            public InitializeTask(ProgramRendererViewModel rendererView)
            {
                this.rendererView = rendererView;
            }

            public void Execute(OpenGL gl)
            {
                rendererView.initialize(gl);
            }
        }

        private void updateVs(object sender, EventArgs e)
        {
            if (shaderCompiler.VertexShaderSource == VERTEX_SHADER_1_SOURCE)
            {
                shaderCompiler.VertexShaderSource = VERTEX_SHADER_2_SOURCE;
            }
            else
            {
                shaderCompiler.VertexShaderSource = VERTEX_SHADER_1_SOURCE;
            }
        }

        private void initialize(OpenGL gl)
        {
            gl.Disable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            shaderCompiler = new ShaderCompiler(gl, glContextManager);
            shaderCompiler.ShaderCompiled += onShaderCompiled;
            shaderCompiler.ProgramLinked += onProgramLinked;
            programInputs = new NullShaderInputs(gl);


            DispatcherTimer updateVsTimer = new DispatcherTimer(
                DispatcherPriority.Normal,
                Application.Current.Dispatcher);

            updateVsTimer.Interval = TimeSpan.FromSeconds(0.5);
            updateVsTimer.Tick += updateVs;
            updateVsTimer.Start();

            shaderCompiler.VertexShaderSource = VERTEX_SHADER_1_SOURCE;

            shaderCompiler.FragmentShaderSource =
                 "#version 330\n"
                + "\n"
                + "out vec4 color;"
                + "\n"
                + "void main()\n"
                + "{\n"
                + "    color = vec4(0.4, 0.7, 1.0, 1.0);\n"
                + "}\n";
        }

        public void OnResize(Size newSize)
        {
            glContextManager.ResizeRenderContext((int) newSize.Width, (int) newSize.Height);
        }

        private void onShaderCompiled(ShaderCompiler sender, uint shaderHandle, Option<string> compileStatus)
        {
            if (compileStatus.hasValue())
            {
                Console.WriteLine("Shader failed to compile:\n" + compileStatus.get());
            }
        }

        private void onProgramLinked(ShaderCompiler sender, Option<string> linkStatus)
        {
            if (linkStatus.hasValue())
            {
                Console.WriteLine("Program failed to link:\n" + linkStatus.get());
            }
        }

        private void onRender(OpenGL gl)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);

            shaderCompiler.UseRenderProgram(gl);
            programInputs.Prepare(gl);

            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);

            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }

        private void onRenderComplete(ImageSource image)
        {
            uiDispatcher.BeginInvoke(
                (Action) (() =>
                {
                    RenderedImage = image;
                }));
        }

//TODO call this method sometime - make sure it is called from the correct thread
        private void DeleteOpenGlObjects(OpenGL gl)
        {
            shaderCompiler.DisposeGlObjects(gl);
            programInputs.DisposeOpenGlObjects(gl);
        }

        public void Shutdown()
        {
            glContextManager.Stop();
        }
    }
}

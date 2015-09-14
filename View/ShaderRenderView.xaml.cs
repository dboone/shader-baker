using SharpGL;
using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ShaderBaker.View
{
    /// <summary>
    /// Interaction logic for ShaderRenderView.xaml
    /// </summary>
    public partial class ShaderRenderView : UserControl
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

        private volatile bool recompileVertexShader;

        private volatile string vertexShaderSource;
        public string VertexShaderSource
        {
            get { return vertexShaderSource; }
            set
            {
                vertexShaderSource = value;
                recompileVertexShader = true;
            }
        }

        private volatile bool recompileFragmentShader;
        
        private volatile string fragmentShaderSource;
        public string FragmentShaderSource
        {
            get { return fragmentShaderSource; }
            set
            {
                fragmentShaderSource = value;
                recompileFragmentShader = true;
            }
        }

        private uint vaoHandle;

        private uint renderProgramHandle;
        private uint compileProgramHandle;

        private uint vertexShaderHandle;
        private uint fragmentShaderHandle;

        public ShaderRenderView()
        {
            InitializeComponent();

            renderProgramHandle = 0;
            compileProgramHandle = 0;
            vaoHandle = 0;
            vertexShaderHandle = 0;
            fragmentShaderHandle = 0;

            VertexShaderSource = VERTEX_SHADER_1_SOURCE;

            FragmentShaderSource =
                 "#version 330\n"
                + "\n"
                + "out vec4 color;"
                + "\n"
                + "void main()\n"
                + "{\n"
                + "    color = vec4(0.4, 0.7, 1.0, 1.0);\n"
                + "}\n";

            DispatcherTimer updateVsTimer = new DispatcherTimer();
            updateVsTimer.Interval = TimeSpan.FromSeconds(0.5);
            updateVsTimer.Tick += updateVs;
            updateVsTimer.Start();
        }

        private void updateVs(object sender, EventArgs e)
        {
            if (VertexShaderSource == VERTEX_SHADER_1_SOURCE)
            {
                VertexShaderSource = VERTEX_SHADER_2_SOURCE;
            } else
            {
                VertexShaderSource = VERTEX_SHADER_1_SOURCE;
            }
        }

        private void GlViewPort_OpenGLInitialized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
            gl.Disable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            uint[] vao = new uint[1];
            gl.GenVertexArrays(vao.Length, vao);
            vaoHandle = vao[0];

            compileProgramHandle = gl.CreateProgram();
            renderProgramHandle = gl.CreateProgram();
            
            vertexShaderHandle = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
            fragmentShaderHandle = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);

            gl.AttachShader(compileProgramHandle, vertexShaderHandle);
            gl.AttachShader(compileProgramHandle, fragmentShaderHandle);

            gl.AttachShader(renderProgramHandle, vertexShaderHandle);
            gl.AttachShader(renderProgramHandle, fragmentShaderHandle);
        }

        private void OpenGLControl_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            bool updateProgram = false;
            if (recompileVertexShader)
            {
                compileShader(vertexShaderHandle, vertexShaderSource);
                updateProgram = true;
            }

            if (recompileFragmentShader)
            {
                compileShader(fragmentShaderHandle, fragmentShaderSource);
                updateProgram = true;
            }

            if (updateProgram)
            {
                linkProgram(compileProgramHandle);
                uint tmp = compileProgramHandle;
                compileProgramHandle = renderProgramHandle;
                renderProgramHandle = tmp;
            }
            
            OpenGL gl = args.OpenGL;

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);

            gl.UseProgram(renderProgramHandle);
            gl.BindVertexArray(vaoHandle);

            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);

            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }

        private void DeleteOpenGlObjects()
        {
//TODO call this sometime - make sure it is called from the correct thread
            OpenGL gl = GlViewPort.OpenGL;
            
            gl.DeleteVertexArrays(1, new uint[] { vaoHandle });
            gl.DeleteProgram(renderProgramHandle);
            gl.DeleteProgram(compileProgramHandle);
            gl.DeleteProgram(vertexShaderHandle);
            gl.DeleteProgram(fragmentShaderHandle);
            
            vaoHandle = 0;
            renderProgramHandle = 0;
            compileProgramHandle = 0;
            vertexShaderHandle = 0;
            fragmentShaderHandle = 0;
        }
        
        private void clearGlErrors()
        {
            OpenGL gl = GlViewPort.OpenGL;
            while (gl.GetError() != OpenGL.GL_NO_ERROR);
        }

        private void checkGlError()
        {
            OpenGL gl = GlViewPort.OpenGL;
            uint errorCode = gl.GetError();
            if (errorCode != OpenGL.GL_NO_ERROR)
            {
                Console.WriteLine("*** OpenGL Error: " + errorCode);
            }
        }
        
        private void compileShader(uint shaderHandle, string source)
        {
            OpenGL gl = GlViewPort.OpenGL;

            gl.ShaderSource(shaderHandle, source);
            
            gl.CompileShader(shaderHandle);

            int[] status = new int[1];
            gl.GetShader(shaderHandle, OpenGL.GL_COMPILE_STATUS, status);
            if (status[0] == OpenGL.GL_FALSE)
            {
                int[] logLength = new int[1];
                gl.GetShader(shaderHandle, OpenGL.GL_INFO_LOG_LENGTH, logLength);

                StringBuilder log = new StringBuilder(logLength[0]);
                gl.GetShaderInfoLog(shaderHandle, logLength[0], IntPtr.Zero, log);

                Console.WriteLine(log.ToString());
            }
        }

        private void linkProgram(uint programHandle)
        {
            OpenGL gl = GlViewPort.OpenGL;

            gl.LinkProgram(programHandle);

            int[] status = new int[1];
            gl.GetProgram(programHandle, OpenGL.GL_LINK_STATUS, status);
            if (status[0] == OpenGL.GL_FALSE)
            {
                String log = getProgramInfoLog(programHandle);
                Console.WriteLine(log);
            }
        }

        private void validateProgram(uint programHandle)
        {
            OpenGL gl = GlViewPort.OpenGL;

            gl.ValidateProgram(programHandle);

            int[] status = new int[1];
            gl.GetProgram(programHandle, OpenGL.GL_VALIDATE_STATUS, status);
            if (status[0] == OpenGL.GL_FALSE)
            {
                String log = getProgramInfoLog(programHandle);
                Console.WriteLine(log);
            }
        }
        
        private string getProgramInfoLog(uint programHandle)
        {
            OpenGL gl = GlViewPort.OpenGL;

            int[] logLength = new int[1];
            gl.GetProgram(programHandle, OpenGL.GL_INFO_LOG_LENGTH, logLength);

            StringBuilder log = new StringBuilder(logLength[0]);
            gl.GetProgramInfoLog(programHandle, logLength[0], IntPtr.Zero, log);

            return log.ToString();
        }
    }
}

using SharpGL;
using System;
using System.Text;
using System.Windows.Controls;

namespace ShaderBaker.View
{
    /// <summary>
    /// Interaction logic for ShaderRenderView.xaml
    /// </summary>
    public partial class ShaderRenderView : UserControl
    {
        private uint ProgramHandle;
        private uint VaoHandle;

        public ShaderRenderView()
        {
            InitializeComponent();

            ProgramHandle = 0;
            VaoHandle = 0;
        }

        private void OpenGLControl_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);

            gl.UseProgram(ProgramHandle);
            gl.BindVertexArray(VaoHandle);

            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);

            gl.BindVertexArray(0);
            gl.UseProgram(0);
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
                Console.WriteLine("*** OpenGL Error:" + errorCode);
            }
        }

        private void GlViewPort_OpenGLInitialized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            checkGlError();
            clearGlErrors();

            OpenGL gl = args.OpenGL;
            gl.Disable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            uint[] vao = new uint[1];
            gl.GenVertexArrays(vao.Length, vao);
            VaoHandle = vao[0];

            ProgramHandle = gl.CreateProgram();

            uint vertexShader = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
            uint fragmentShader = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);

            // TODO - obtain shader source from somewhere else
            string vsSource =
                 "#version 330\n"
                + "\n"
                + "void main()\n"
                + "{\n"
                + "    vec2 vertices[3] = vec2[3](\n"
                + "        vec2( 0.25, -0.25),\n"
                + "        vec2( 0.25,  0.25),\n"
                + "        vec2(-0.25, -0.25) );\n"
                + "    gl_Position = vec4(vertices[gl_VertexID], 0.0, 1.0);\n"
                + "}\n";

            string fsSource =
                 "#version 330\n"
                + "\n"
                + "out vec4 color;"
                + "\n"
                + "void main()\n"
                + "{\n"
                + "    color = vec4(0.4, 0.7, 1.0, 1.0);\n"
                + "}\n";
            
            compileShader(vertexShader, vsSource);
            compileShader(fragmentShader, fsSource);

            gl.AttachShader(ProgramHandle, vertexShader);
            gl.AttachShader(ProgramHandle, fragmentShader);

            linkProgram(ProgramHandle);
            validateProgram(ProgramHandle);

            gl.DetachShader(ProgramHandle, vertexShader);
            gl.DetachShader(ProgramHandle, fragmentShader);

            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
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

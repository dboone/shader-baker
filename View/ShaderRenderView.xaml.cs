﻿using GlRenderer.ShaderBaker;
using ShaderBaker.GlRenderer;
using ShaderBaker.Utilities;
using SharpGL;
using System;
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
        
        private ShaderCompiler shaderCompiler;
        private NullShaderInputs programInputs;

        public ShaderRenderView()
        {
            InitializeComponent();
            
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
            
            shaderCompiler = new ShaderCompiler(gl);
            programInputs = new NullShaderInputs(gl);
        }

        private void OpenGLControl_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
            
            if (recompileVertexShader)
            {
                Option<string> compileStatus = shaderCompiler.RecompileVertexShader(gl, vertexShaderSource);
                if (compileStatus.hasValue())
                {
                    Console.WriteLine(compileStatus.get());
                }
            }

            if (recompileFragmentShader)
            {
                Option<string> compileStatus = shaderCompiler.RecompileFragmentShader(gl, fragmentShaderSource);
                if (compileStatus.hasValue())
                {
                    Console.WriteLine(compileStatus.get());
                }
            }
            
            Option<string> linkStatus = shaderCompiler.RelinkProgramIfStale(gl);
            if (linkStatus.hasValue())
            {
                Console.WriteLine(linkStatus.get());
            }

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);

            shaderCompiler.UseRenderProgram(gl);
            programInputs.Prepare(gl);

            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);

            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }
        
//TODO call this method sometime - make sure it is called from the correct thread
        private void DeleteOpenGlObjects()
        {
            OpenGL gl = GlViewPort.OpenGL;
            shaderCompiler.DisposeGlObjects(gl);
            programInputs.DisposeOpenGlObjects(gl);
        }
    }
}
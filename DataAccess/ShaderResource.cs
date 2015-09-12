using System;

namespace ShaderBaker.DataAccess
{
     /// <summary>
     /// ShaderResource could possibly be an abstract class, but
     /// for now we use a simple concrete class that holds a string.
     ///
     /// Eventually we will pull the shader text from a file. This
     /// class could be adapted to perform such a task.
     /// </summary>
    class ShaderResource
    {
        private string text;

        public ShaderResource()
        {
            string newline = Environment.NewLine;
            text = "#version 330"           + newline + newline
                 + "out vec4 FragColor;"    + newline + newline
                 + "void main()"            + newline
                 + "{"                      + newline
                 + "    FragColor = vec4(1.0, 0.0, 0.0, 1.0);" + newline
                 + "}"                      + newline;
        }

        public string GetText()
        {
            return text;
        }
    }
}

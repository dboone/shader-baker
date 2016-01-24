using ShaderBaker.GlRenderer;
using ShaderBaker.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShaderBaker.Serial
{

/// <summary>
/// Class for serializing Shader Baker projects
/// </summary>
/// <remarks>
/// The format to which it serializes files is for development purposes only.
/// It is not robust enough to release. It does not encode a version for backwards/forwards
/// compatibility, is not extensible, and does not handle endianness differences between
/// machines. It simply keeps us developers from having to recreate things every time we
/// restart the application.
/// </remarks>
public class ProjectSerializer
{
    public static void WriteProject(
        Stream outputStream, ICollection<Shader> shaders, ICollection<Program> programs)
    {
        writeInt(outputStream, shaders.Count);
        var shaderToIdMap = new Dictionary<Shader, uint>(new IdentityEqualityComparer<Shader>());
        uint nextShaderId = 0;
        foreach (var shader in shaders)
        {
            shaderToIdMap[shader] = nextShaderId;
            ++nextShaderId;
            
            writeUint(outputStream, (uint) shader.Stage);
            writeString(outputStream, shader.Name);
            writeString(outputStream, shader.Source);
        }
        
        writeInt(outputStream, programs.Count);
        foreach (var program in programs)
        {
            writeString(outputStream, program.Name);
            writeInt(outputStream, program.ShadersByStage.Count);
            foreach (var attachedShader in program.ShadersByStage.Values)
            {
                var shaderId = shaderToIdMap[attachedShader];
                writeUint(outputStream, shaderId);
            }
        }
    }

    public static void ReadProject(Stream inputStream, out Shader[] shaders, out Program[] programs)
    {
        int numberShaders = readInt(inputStream);
        var idToShaderMap = new Dictionary<uint, Shader>(numberShaders);
        shaders = new Shader[numberShaders];
        for (uint shaderId = 0; shaderId < numberShaders; ++shaderId)
        {
            var shader = new Shader((ProgramStage) readUint(inputStream));
            shader.Name = readString(inputStream);
            shader.Source = readString(inputStream);

            shaders[shaderId] = shader;
            idToShaderMap[shaderId] = shader;
        }
        
        int numberPrograms = readInt(inputStream);
        programs = new Program[numberPrograms];
        for (uint i = 0; i < numberPrograms; ++i)
        {
            Program program = new Program();
            program.Name = readString(inputStream);
            var attachedShaderCount = readInt(inputStream);
            for (uint j = 0; j < attachedShaderCount; ++j)
            {
                var shaderId = readUint(inputStream);
                var shader = idToShaderMap[shaderId];
                program.AttachShader(shader);
            }
            programs[i] = program;
        }
    }

    private static void writeInt(Stream outputStream, int value)
    {
        outputStream.WriteByte((byte) (value >> 24));
        outputStream.WriteByte((byte) (value >> 16));
        outputStream.WriteByte((byte) (value >> 8));
        outputStream.WriteByte((byte) (value));
    }

    private static void writeUint(Stream outputStream, uint value)
    {
        writeInt(outputStream, unchecked((int) value));
    }

    private static void writeChar(Stream outputStream, char value)
    {
        outputStream.WriteByte((byte) (value >> 8));
        outputStream.WriteByte((byte) (value));
    }

    private static void writeString(Stream outputStream, string value)
    {
        writeInt(outputStream, value.Length);
        foreach (var c in value)
        {
            writeChar(outputStream, c);
        }
    }

    private static int readInt(Stream inputStream)
    {
        return (inputStream.ReadByte() << 24)
            | (inputStream.ReadByte() << 16)
            | (inputStream.ReadByte() << 8)
            | (inputStream.ReadByte());
    }

    private static uint readUint(Stream inputStream)
    {
        int value = readInt(inputStream);
        return unchecked((uint) value);
    }

    private static char readChar(Stream inputStream)
    {
        return (char) ((inputStream.ReadByte() << 8) | (inputStream.ReadByte()));
    }

    private static string readString(Stream inputStream)
    {
        var stringLength = readInt(inputStream);
        var stringBuilder = new StringBuilder(stringLength);
        for (int i = 0; i < stringLength; ++i)
        {
            stringBuilder.Append(readChar(inputStream));
        }

        return stringBuilder.ToString();
    }
}

}

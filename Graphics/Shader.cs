using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelEngine.Graphics;

public class Shader : IDisposable
{
    public int Handle { get; private set; }
    private bool disposed = false;

    // =====================================================
    // FILE-BASED SHADER (voxel.vert / voxel.frag)
    // =====================================================
    public Shader(string vertexPath, string fragmentPath)
    {
        // Load shader source
        string vertexSource = File.ReadAllText(vertexPath);
        string fragmentSource = File.ReadAllText(fragmentPath);

        // Create and compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        CheckCompileErrors(vertexShader, "VERTEX");

        // Create and compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        CheckCompileErrors(fragmentShader, "FRAGMENT");

        // Link shaders into program
        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);
        CheckCompileErrors(Handle, "PROGRAM");

        // Clean up
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    // =====================================================
    // RUNTIME SHADER (grid, gizmos, editor helpers, etc.)
    // =====================================================
    public Shader(int existingProgram)
    {
        Handle = existingProgram;
    }

    // =====================================================
    // USAGE
    // =====================================================
    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.UniformMatrix4(location, false, ref matrix);
    }

    public void SetVector3(string name, Vector3 vector)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform3(location, vector);
    }

    public void SetFloat(string name, float value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, value);
    }

    public void SetInt(string name, int value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, value);
    }

    // =====================================================
    // ERROR HANDLING (UNCHANGED)
    // =====================================================
    private void CheckCompileErrors(int shader, string type)
    {
        if (type != "PROGRAM")
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"ERROR::SHADER::{type}::COMPILATION_FAILED\n{infoLog}");
            }
        }
        else
        {
            GL.GetProgram(shader, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(shader);
                Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");
            }
        }
    }

    // =====================================================
    // CLEANUP
    // =====================================================
    public void Dispose()
    {
        if (!disposed)
        {
            GL.DeleteProgram(Handle);
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

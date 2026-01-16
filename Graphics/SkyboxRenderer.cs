using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelEngine.Graphics;

public class SkyboxRenderer : IDisposable
{
    private Shader shader;
    private int vao;
    private int vbo;

    private static readonly float[] skyboxVertices = {
        // Positions for a cube (centered at origin)
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        -1.0f,  1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f
    };

    public SkyboxRenderer()
    {
        shader = new Shader("Shaders/skybox.vert", "Shaders/skybox.frag");
        SetupMesh();
    }

    private void SetupMesh()
    {
        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, skyboxVertices.Length * sizeof(float),
                     skyboxVertices, BufferUsageHint.StaticDraw);

        // Position attribute
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        GL.BindVertexArray(0);
    }

    public void Render(Matrix4 view, Matrix4 projection, Vector3 sunDirection,
                      Vector3 moonDirection, float dayNightCycle)
    {
        // Change depth function so depth test passes when values are equal to depth buffer's content
        GL.DepthFunc(DepthFunction.Lequal);

        shader.Use();
        shader.SetMatrix4("view", view);
        shader.SetMatrix4("projection", projection);
        shader.SetVector3("sunDirection", sunDirection);
        shader.SetVector3("moonDirection", moonDirection);
        shader.SetFloat("dayNightCycle", dayNightCycle);

        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        GL.BindVertexArray(0);

        // Restore default depth function
        GL.DepthFunc(DepthFunction.Less);
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(vao);
        GL.DeleteBuffer(vbo);
        shader.Dispose();
    }
}

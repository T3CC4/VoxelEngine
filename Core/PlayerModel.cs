using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace VoxelEngine.Core;

public class PlayerModel : IDisposable
{
    private int vao, vbo;
    private int vertexCount;
    private bool disposed = false;

    // Simple rectangle model for now - easily swappable with Blockbench model later
    private static readonly float[] rectangleVertices = {
        // Front face
        -0.3f, 0.0f,  0.15f,  0.2f, 0.5f, 0.8f,  // Bottom left
         0.3f, 0.0f,  0.15f,  0.2f, 0.5f, 0.8f,  // Bottom right
         0.3f, 1.8f,  0.15f,  0.2f, 0.5f, 0.8f,  // Top right
        -0.3f, 1.8f,  0.15f,  0.2f, 0.5f, 0.8f,  // Top left

        // Back face
         0.3f, 0.0f, -0.15f,  0.1f, 0.4f, 0.7f,
        -0.3f, 0.0f, -0.15f,  0.1f, 0.4f, 0.7f,
        -0.3f, 1.8f, -0.15f,  0.1f, 0.4f, 0.7f,
         0.3f, 1.8f, -0.15f,  0.1f, 0.4f, 0.7f,

        // Left face
        -0.3f, 0.0f, -0.15f,  0.15f, 0.45f, 0.75f,
        -0.3f, 0.0f,  0.15f,  0.15f, 0.45f, 0.75f,
        -0.3f, 1.8f,  0.15f,  0.15f, 0.45f, 0.75f,
        -0.3f, 1.8f, -0.15f,  0.15f, 0.45f, 0.75f,

        // Right face
         0.3f, 0.0f,  0.15f,  0.15f, 0.45f, 0.75f,
         0.3f, 0.0f, -0.15f,  0.15f, 0.45f, 0.75f,
         0.3f, 1.8f, -0.15f,  0.15f, 0.45f, 0.75f,
         0.3f, 1.8f,  0.15f,  0.15f, 0.45f, 0.75f,

        // Top face
        -0.3f, 1.8f,  0.15f,  0.3f, 0.6f, 0.9f,
         0.3f, 1.8f,  0.15f,  0.3f, 0.6f, 0.9f,
         0.3f, 1.8f, -0.15f,  0.3f, 0.6f, 0.9f,
        -0.3f, 1.8f, -0.15f,  0.3f, 0.6f, 0.9f,

        // Bottom face
        -0.3f, 0.0f, -0.15f,  0.1f, 0.3f, 0.6f,
         0.3f, 0.0f, -0.15f,  0.1f, 0.3f, 0.6f,
         0.3f, 0.0f,  0.15f,  0.1f, 0.3f, 0.6f,
        -0.3f, 0.0f,  0.15f,  0.1f, 0.3f, 0.6f,
    };

    private static readonly uint[] rectangleIndices = {
        // Front
        0, 1, 2, 2, 3, 0,
        // Back
        4, 5, 6, 6, 7, 4,
        // Left
        8, 9, 10, 10, 11, 8,
        // Right
        12, 13, 14, 14, 15, 12,
        // Top
        16, 17, 18, 18, 19, 16,
        // Bottom
        20, 21, 22, 22, 23, 20
    };

    public PlayerModel()
    {
        SetupMesh();
    }

    private void SetupMesh()
    {
        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();
        int ebo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, rectangleVertices.Length * sizeof(float),
                     rectangleVertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, rectangleIndices.Length * sizeof(uint),
                     rectangleIndices, BufferUsageHint.StaticDraw);

        // Position attribute
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

        // Color attribute
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);

        vertexCount = rectangleIndices.Length;
    }

    public void Render()
    {
        GL.BindVertexArray(vao);
        GL.DrawElements(PrimitiveType.Triangles, vertexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

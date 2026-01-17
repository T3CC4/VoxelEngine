using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.Graphics;

public class VoxelMesh : IDisposable
{
    private int vao, vbo, ebo;
    private int vertexCount;
    private bool disposed = false;
    private VoxelWorld? world;

    private void AddFace(List<float> vertices, List<uint> indices, ref uint currentIndex,
        int x, int y, int z, int face, Vector3 color, VoxelType voxelType)
    {
        Vector3 position = new Vector3(x, y, z);
        Vector3 normal = GetFaceNormal(face);
        Vector3[] faceVertices = GetFaceVertices(face);

        int isWater = (voxelType == VoxelType.Water) ? 1 : 0;

        // Calculate per-vertex AO for smooth lighting
        float[] aoValues = CalculateVertexAO(x, y, z, face);

        for (int i = 0; i < 4; i++)
        {
            Vector3 vertex = position + faceVertices[i];

            // Position
            vertices.Add(vertex.X);
            vertices.Add(vertex.Y);
            vertices.Add(vertex.Z);

            // Color
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);

            // Normal
            vertices.Add(normal.X);
            vertices.Add(normal.Y);
            vertices.Add(normal.Z);

            // Ambient Occlusion (per-vertex)
            vertices.Add(aoValues[i]);

            // Is Water flag
            vertices.Add(isWater);
        }

        // Indices for two triangles (quad)
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        indices.Add(currentIndex + 0);

        currentIndex += 4;
    }

    private float[] CalculateVertexAO(int x, int y, int z, int face)
    {
        // Get the face's tangent and bitangent vectors for checking neighboring blocks
        Vector3Int normal = GetFaceDirection(face);
        Vector3Int tangent, bitangent;

        // Determine tangent and bitangent based on face normal
        if (Math.Abs(normal.Y) > 0.5f)
        {
            // Top/Bottom face
            tangent = new Vector3Int(1, 0, 0);
            bitangent = new Vector3Int(0, 0, 1);
        }
        else if (Math.Abs(normal.X) > 0.5f)
        {
            // Left/Right face
            tangent = new Vector3Int(0, 1, 0);
            bitangent = new Vector3Int(0, 0, 1);
        }
        else
        {
            // Front/Back face
            tangent = new Vector3Int(1, 0, 0);
            bitangent = new Vector3Int(0, 1, 0);
        }

        float[] aoValues = new float[4];

        // Check corners for each vertex of the quad
        Vector3Int[] cornerOffsets = new[]
        {
            -tangent - bitangent,  // Vertex 0
             tangent - bitangent,  // Vertex 1
             tangent + bitangent,  // Vertex 2
            -tangent + bitangent   // Vertex 3
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3Int vertexPos = new Vector3Int(x, y, z) + normal; // Position at face
            Vector3Int cornerOffset = cornerOffsets[i];

            // Check 3 blocks around this vertex corner
            Vector3Int side1 = vertexPos + new Vector3Int(cornerOffset.X, 0, 0);
            Vector3Int side2 = vertexPos + new Vector3Int(0, cornerOffset.Y, 0);
            Vector3Int side3 = vertexPos + new Vector3Int(0, 0, cornerOffset.Z);
            Vector3Int corner = vertexPos + cornerOffset;

            int solidCount = 0;
            if (world != null)
            {
                if (IsSolidAt(side1)) solidCount++;
                if (IsSolidAt(side2)) solidCount++;
                if (IsSolidAt(side3)) solidCount++;
                if (IsSolidAt(corner)) solidCount++;
            }

            // Convert solid count to AO value (more solid neighbors = darker)
            aoValues[i] = 1.0f - (solidCount * 0.15f);
        }

        return aoValues;
    }

    public void BuildMesh(Chunk chunk, VoxelWorld worldRef)
    {
        world = worldRef;
        BuildMeshInternal(chunk);
    }

    private void BuildMeshInternal(Chunk chunk)
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        uint currentIndex = 0;

        Vector3Int chunkWorldPos = chunk.Position * Chunk.ChunkSize;

        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    var voxel = chunk.GetVoxel(x, y, z);
                    if (!voxel.IsActive) continue;

                    Vector3Int worldPos = chunkWorldPos + new Vector3Int(x, y, z);
                    Vector3 color = voxel.Type.GetColor();

                    // Check each face
                    for (int face = 0; face < 6; face++)
                    {
                        Vector3Int neighborPos = worldPos + GetFaceDirection(face);
                        var neighbor = world!.GetVoxel(neighborPos);

                        // Only render face if neighbor is empty or transparent
                        if (!neighbor.IsActive || neighbor.Type == VoxelType.Water)
                        {
                            AddFace(vertices, indices, ref currentIndex, (int)worldPos.X, (int)worldPos.Y, (int)worldPos.Z, face, color, voxel.Type);
                        }
                    }
                }
            }
        }

        vertexCount = indices.Count;
        SetupMesh(vertices.ToArray(), indices.ToArray());
    }

    private bool IsSolidAt(Vector3Int pos)
    {
        if (world == null) return false;
        var voxel = world.GetVoxel(pos);
        return voxel.IsActive && voxel.Type != VoxelType.Air && voxel.Type != VoxelType.Water;
    }

    private Vector3Int GetFaceDirection(int face)
    {
        return face switch
        {
            0 => new Vector3Int(0, 1, 0),   // Top
            1 => new Vector3Int(0, -1, 0),  // Bottom
            2 => new Vector3Int(0, 0, 1),   // Front
            3 => new Vector3Int(0, 0, -1),  // Back
            4 => new Vector3Int(1, 0, 0),   // Right
            5 => new Vector3Int(-1, 0, 0),  // Left
            _ => Vector3Int.Zero
        };
    }

    private Vector3 GetFaceNormal(int face)
    {
        return face switch
        {
            0 => new Vector3(0, 1, 0),   // Top
            1 => new Vector3(0, -1, 0),  // Bottom
            2 => new Vector3(0, 0, 1),   // Front
            3 => new Vector3(0, 0, -1),  // Back
            4 => new Vector3(1, 0, 0),   // Right
            5 => new Vector3(-1, 0, 0),  // Left
            _ => Vector3.Zero
        };
    }

    private Vector3[] GetFaceVertices(int face)
    {
        return face switch
        {
            0 => new[] // Top
            {
                new Vector3(0, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0)
            },
            1 => new[] // Bottom
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1),
                new Vector3(0, 0, 1)
            },
            2 => new[] // Front
            {
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(1, 1, 1),
                new Vector3(0, 1, 1)
            },
            3 => new[] // Back
            {
                new Vector3(1, 0, 0),
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0)
            },
            4 => new[] // Right
            {
                new Vector3(1, 0, 1),
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 1)
            },
            5 => new[] // Left
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 1),
                new Vector3(0, 1, 0)
            },
            _ => Array.Empty<Vector3>()
        };
    }

    private void SetupMesh(float[] vertices, uint[] indices)
    {
        // Delete old buffers if they exist
        if (vao != 0) GL.DeleteVertexArray(vao);
        if (vbo != 0) GL.DeleteBuffer(vbo);
        if (ebo != 0) GL.DeleteBuffer(ebo);

        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();
        ebo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        int stride = 11 * sizeof(float);

        // Position attribute
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Normal attribute
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        // Ambient Occlusion attribute
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 9 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        // IsWater attribute (as integer)
        GL.VertexAttribIPointer(4, 1, VertexAttribIntegerType.Int, stride, (IntPtr)(10 * sizeof(float)));
        GL.EnableVertexAttribArray(4);

        GL.BindVertexArray(0);
    }

    public void Render()
    {
        if (vertexCount == 0) return;

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
            GL.DeleteBuffer(ebo);
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

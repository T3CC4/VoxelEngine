namespace VoxelEngine.Core;

public class VoxelWorld
{
    private readonly Dictionary<Vector3Int, Chunk> chunks;
    public Vector3Int WorldSize { get; }

    public VoxelWorld(Vector3Int worldSize)
    {
        WorldSize = worldSize;
        chunks = new Dictionary<Vector3Int, Chunk>();
        InitializeWorld();
    }

    private void InitializeWorld()
    {
        for (int x = 0; x < WorldSize.X; x++)
        {
            for (int y = 0; y < WorldSize.Y; y++)
            {
                for (int z = 0; z < WorldSize.Z; z++)
                {
                    var chunkPos = new Vector3Int(x, y, z);
                    chunks[chunkPos] = new Chunk(chunkPos);
                }
            }
        }
    }

    public Chunk? GetChunk(Vector3Int chunkPosition)
    {
        return chunks.TryGetValue(chunkPosition, out var chunk) ? chunk : null;
    }

    public Voxel GetVoxel(Vector3Int worldPosition)
    {
        var chunkPos = WorldToChunkPosition(worldPosition);
        var localPos = WorldToLocalPosition(worldPosition);

        var chunk = GetChunk(chunkPos);
        return chunk?.GetVoxel(localPos.X, localPos.Y, localPos.Z) ?? Voxel.Empty;
    }

    public void SetVoxel(Vector3Int worldPosition, Voxel voxel)
    {
        var chunkPos = WorldToChunkPosition(worldPosition);
        var localPos = WorldToLocalPosition(worldPosition);

        var chunk = GetChunk(chunkPos);
        chunk?.SetVoxel(localPos.X, localPos.Y, localPos.Z, voxel);
    }

    public void SetVoxelType(Vector3Int worldPosition, VoxelType type)
    {
        var chunkPos = WorldToChunkPosition(worldPosition);
        var localPos = WorldToLocalPosition(worldPosition);

        var chunk = GetChunk(chunkPos);
        chunk?.SetVoxelType(localPos.X, localPos.Y, localPos.Z, type);
    }

    private Vector3Int WorldToChunkPosition(Vector3Int worldPosition)
    {
        return new Vector3Int(
            worldPosition.X / Chunk.ChunkSize,
            worldPosition.Y / Chunk.ChunkSize,
            worldPosition.Z / Chunk.ChunkSize
        );
    }

    private Vector3Int WorldToLocalPosition(Vector3Int worldPosition)
    {
        return new Vector3Int(
            ((worldPosition.X % Chunk.ChunkSize) + Chunk.ChunkSize) % Chunk.ChunkSize,
            ((worldPosition.Y % Chunk.ChunkSize) + Chunk.ChunkSize) % Chunk.ChunkSize,
            ((worldPosition.Z % Chunk.ChunkSize) + Chunk.ChunkSize) % Chunk.ChunkSize
        );
    }

    public IEnumerable<Chunk> GetAllChunks()
    {
        return chunks.Values;
    }

    public void GenerateTestTerrain()
    {
        int maxX = WorldSize.X * Chunk.ChunkSize;
        int maxZ = WorldSize.Z * Chunk.ChunkSize;
        int maxY = WorldSize.Y * Chunk.ChunkSize;

        for (int x = 0; x < maxX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                int height = 4 + (int)(Math.Sin(x * 0.1) * Math.Cos(z * 0.1) * 2);

                for (int y = 0; y < height && y < maxY; y++)
                {
                    VoxelType type = y == height - 1 ? VoxelType.Grass :
                                   y >= height - 3 ? VoxelType.Dirt : VoxelType.Stone;
                    SetVoxelType(new Vector3Int(x, y, z), type);
                }
            }
        }
    }
}

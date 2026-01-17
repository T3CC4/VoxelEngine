using OpenTK.Mathematics;

namespace VoxelEngine.Core;

public class InfiniteVoxelWorld
{
    private readonly Dictionary<Vector3Int, Chunk> chunks = new();
    private readonly int renderDistance;
    private readonly int verticalChunks;
    private Vector3Int lastCenterChunk = new Vector3Int(int.MaxValue, 0, int.MaxValue);

    public InfiniteVoxelWorld(int renderDistance = 8, int verticalChunks = 4)
    {
        this.renderDistance = renderDistance;
        this.verticalChunks = verticalChunks;
    }

    public void UpdateChunksAroundPosition(Vector3 worldPosition)
    {
        Vector3Int centerChunk = WorldToChunkPosition(new Vector3Int(
            (int)Math.Floor(worldPosition.X),
            0,
            (int)Math.Floor(worldPosition.Z)
        ));

        // Only update if center chunk has changed
        if (centerChunk == lastCenterChunk)
            return;

        lastCenterChunk = centerChunk;

        // Generate chunks within render distance
        var chunksToKeep = new HashSet<Vector3Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                // Check if within circular render distance
                if (x * x + z * z <= renderDistance * renderDistance)
                {
                    for (int y = 0; y < verticalChunks; y++)
                    {
                        Vector3Int chunkPos = new Vector3Int(
                            centerChunk.X + x,
                            y,
                            centerChunk.Z + z
                        );

                        chunksToKeep.Add(chunkPos);

                        // Create chunk if it doesn't exist
                        if (!chunks.ContainsKey(chunkPos))
                        {
                            chunks[chunkPos] = new Chunk(chunkPos);
                        }
                    }
                }
            }
        }

        // Remove chunks outside render distance
        var chunksToRemove = chunks.Keys.Where(pos => !chunksToKeep.Contains(pos)).ToList();
        foreach (var pos in chunksToRemove)
        {
            chunks.Remove(pos);
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

    public List<Vector3Int> GetAffectedChunks(Vector3Int worldPosition)
    {
        var affected = new List<Vector3Int>();
        var chunkPos = WorldToChunkPosition(worldPosition);
        var localPos = WorldToLocalPosition(worldPosition);

        // Always add the chunk containing this voxel
        affected.Add(chunkPos);

        // Check if voxel is on chunk boundaries and add neighboring chunks
        if (localPos.X == 0)
            affected.Add(chunkPos + new Vector3Int(-1, 0, 0));
        else if (localPos.X == Chunk.ChunkSize - 1)
            affected.Add(chunkPos + new Vector3Int(1, 0, 0));

        if (localPos.Y == 0)
            affected.Add(chunkPos + new Vector3Int(0, -1, 0));
        else if (localPos.Y == Chunk.ChunkSize - 1)
            affected.Add(chunkPos + new Vector3Int(0, 1, 0));

        if (localPos.Z == 0)
            affected.Add(chunkPos + new Vector3Int(0, 0, -1));
        else if (localPos.Z == Chunk.ChunkSize - 1)
            affected.Add(chunkPos + new Vector3Int(0, 0, 1));

        // Remove chunks that don't exist
        return affected.Where(pos => chunks.ContainsKey(pos)).ToList();
    }

    private Vector3Int WorldToChunkPosition(Vector3Int worldPosition)
    {
        return new Vector3Int(
            worldPosition.X >= 0 ? worldPosition.X / Chunk.ChunkSize : (worldPosition.X - Chunk.ChunkSize + 1) / Chunk.ChunkSize,
            worldPosition.Y >= 0 ? worldPosition.Y / Chunk.ChunkSize : (worldPosition.Y - Chunk.ChunkSize + 1) / Chunk.ChunkSize,
            worldPosition.Z >= 0 ? worldPosition.Z / Chunk.ChunkSize : (worldPosition.Z - Chunk.ChunkSize + 1) / Chunk.ChunkSize
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

    public IEnumerable<Vector3Int> GetAllChunkPositions()
    {
        return chunks.Keys;
    }

    public bool HasChunk(Vector3Int chunkPos)
    {
        return chunks.ContainsKey(chunkPos);
    }
}

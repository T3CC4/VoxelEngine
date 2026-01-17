using OpenTK.Mathematics;

namespace VoxelEngine.Core;

public interface IVoxelWorld
{
    Voxel GetVoxel(Vector3Int worldPosition);
    void SetVoxel(Vector3Int worldPosition, Voxel voxel);
    void SetVoxelType(Vector3Int worldPosition, VoxelType type);
    List<Vector3Int> GetAffectedChunks(Vector3Int worldPosition);
    Chunk? GetChunk(Vector3Int chunkPosition);
    IEnumerable<Chunk> GetAllChunks();
}

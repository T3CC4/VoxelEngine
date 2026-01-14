namespace VoxelEngine.Core;

public class Chunk
{
    public const int ChunkSize = 32;
    public Vector3Int Position { get; }
    private readonly Voxel[,,] voxels;

    public Chunk(Vector3Int position)
    {
        Position = position;
        voxels = new Voxel[ChunkSize, ChunkSize, ChunkSize];
        InitializeEmpty();
    }

    private void InitializeEmpty()
    {
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    voxels[x, y, z] = Voxel.Empty;
                }
            }
        }
    }

    public Voxel GetVoxel(int x, int y, int z)
    {
        if (IsValidPosition(x, y, z))
            return voxels[x, y, z];
        return Voxel.Empty;
    }

    public void SetVoxel(int x, int y, int z, Voxel voxel)
    {
        if (IsValidPosition(x, y, z))
        {
            voxels[x, y, z] = voxel;
        }
    }

    public void SetVoxelType(int x, int y, int z, VoxelType type)
    {
        if (IsValidPosition(x, y, z))
        {
            voxels[x, y, z] = new Voxel(type, type != VoxelType.Air);
        }
    }

    private bool IsValidPosition(int x, int y, int z)
    {
        return x >= 0 && x < ChunkSize &&
               y >= 0 && y < ChunkSize &&
               z >= 0 && z < ChunkSize;
    }

    public IEnumerable<(Vector3Int position, Voxel voxel)> GetAllVoxels()
    {
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    var voxel = voxels[x, y, z];
                    if (voxel.IsActive)
                    {
                        yield return (new Vector3Int(x, y, z), voxel);
                    }
                }
            }
        }
    }
}

namespace VoxelEngine.Core;

public struct Voxel
{
    public VoxelType Type { get; set; }
    public bool IsActive { get; set; }

    public Voxel(VoxelType type, bool isActive = true)
    {
        Type = type;
        IsActive = isActive;
    }

    public static Voxel Empty => new(VoxelType.Air, false);

    public bool IsSolid => IsActive && Type.IsSolid();
}

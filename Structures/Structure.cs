using Newtonsoft.Json;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Structures;

public enum StructureCategory
{
    Architecture,  // Houses, buildings, etc.
    Ambient       // Trees, rocks, decorations, etc.
}

public class Structure
{
    public string Name { get; set; } = "Unnamed";
    public StructureCategory Category { get; set; }
    public Vector3Int Size { get; set; }
    public List<VoxelData> Voxels { get; set; } = new();

    // Biome associations (empty means all biomes)
    public List<BiomeType> AllowedBiomes { get; set; } = new();

    // Spawn frequency (0.0 to 1.0, where 1.0 = 100% chance per eligible location)
    public float SpawnFrequency { get; set; } = 0.05f;

    [JsonIgnore]
    public string FileName => $"{Category}_{Name}.json";

    public Structure()
    {
    }

    public Structure(string name, StructureCategory category)
    {
        Name = name;
        Category = category;
    }

    public void AddVoxel(Vector3Int localPosition, VoxelType type)
    {
        // Remove existing voxel at this position if any
        Voxels.RemoveAll(v => v.Position == localPosition);

        if (type != VoxelType.Air)
        {
            Voxels.Add(new VoxelData { Position = localPosition, Type = type });

            // Update size
            Size = new Vector3Int(
                Math.Max(Size.X, localPosition.X + 1),
                Math.Max(Size.Y, localPosition.Y + 1),
                Math.Max(Size.Z, localPosition.Z + 1)
            );
        }
    }

    public void RemoveVoxel(Vector3Int localPosition)
    {
        Voxels.RemoveAll(v => v.Position == localPosition);
    }

    public VoxelType GetVoxel(Vector3Int localPosition)
    {
        var voxel = Voxels.FirstOrDefault(v => v.Position == localPosition);
        return voxel?.Type ?? VoxelType.Air;
    }

    public void PlaceInWorld(IVoxelWorld world, Vector3Int worldPosition)
    {
        foreach (var voxel in Voxels)
        {
            world.SetVoxelType(worldPosition + voxel.Position, voxel.Type);
        }
    }

    public void PlaceOnGround(IVoxelWorld world, Vector3Int worldPosition, int maxSearchDown = 64)
    {
        // Find ground level starting from worldPosition and searching down
        int groundY = FindGroundLevel(world, worldPosition, maxSearchDown);

        if (groundY == -1)
        {
            // No ground found, place at original position
            PlaceInWorld(world, worldPosition);
            return;
        }

        // Place structure with base at ground level
        Vector3Int groundPosition = new Vector3Int(worldPosition.X, groundY, worldPosition.Z);
        PlaceInWorld(world, groundPosition);
    }

    private int FindGroundLevel(IVoxelWorld world, Vector3Int startPosition, int maxSearchDown)
    {
        // Search downward from start position to find first solid block
        for (int y = startPosition.Y; y >= startPosition.Y - maxSearchDown && y >= 0; y--)
        {
            Vector3Int checkPos = new Vector3Int(startPosition.X, y, startPosition.Z);
            var voxel = world.GetVoxel(checkPos);

            if (voxel.IsActive && voxel.Type.IsSolid() && voxel.Type != VoxelType.Water)
            {
                // Found solid ground, return position above it
                return y + 1;
            }
        }

        return -1; // No ground found
    }

    public int GetBaseHeight()
    {
        // Return the minimum Y position of all voxels (the bottom of the structure)
        if (Voxels.Count == 0)
            return 0;

        return Voxels.Min(v => v.Position.Y);
    }

    public bool CanSpawnInBiome(BiomeType biome)
    {
        // If no biomes specified, can spawn in all biomes
        if (AllowedBiomes.Count == 0)
            return true;

        return AllowedBiomes.Contains(biome);
    }

    public void Save(string structuresPath)
    {
        string categoryPath = Path.Combine(structuresPath, Category.ToString());
        Directory.CreateDirectory(categoryPath);

        string filePath = Path.Combine(categoryPath, FileName);
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static Structure? Load(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Structure>(json);
    }

    public static List<Structure> LoadAllFromCategory(string structuresPath, StructureCategory category)
    {
        string categoryPath = Path.Combine(structuresPath, category.ToString());
        if (!Directory.Exists(categoryPath))
            return new List<Structure>();

        var structures = new List<Structure>();
        foreach (var file in Directory.GetFiles(categoryPath, "*.json"))
        {
            var structure = Load(file);
            if (structure != null)
                structures.Add(structure);
        }

        return structures;
    }
}

public class VoxelData
{
    public Vector3Int Position { get; set; }
    public VoxelType Type { get; set; }
}

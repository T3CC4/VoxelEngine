using Newtonsoft.Json;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Decorations;

/// <summary>
/// Represents a decoration like grass or flowers that uses mini-voxels
/// within a single block. Mini-voxels are smaller than regular voxels
/// to allow for more detailed decorations.
///
/// MINIVOXEL SIZING SYSTEM:
/// - Decorations are limited to exactly 1 block (1.0 world unit) in size
/// - Resolution defines subdivision: Resolution=4 means 4x4x4 mini-voxels per block
/// - Mini-voxel size = 1.0 / Resolution (e.g., 0.25 for Resolution=4)
/// - Mini-voxel positions range from (0,0,0) to (Resolution-1, Resolution-1, Resolution-1)
/// - World coordinates range from (0,0,0) to (1,1,1)
/// - Common resolutions: 4 (coarse), 8 (medium), 16 (fine detail)
/// - This ensures perfect alignment with the block grid
/// </summary>
public class Decoration
{
    public string Name { get; set; } = "Unnamed";

    /// <summary>
    /// Mini-voxel resolution (e.g., 4 means 4x4x4 mini-voxels per block).
    /// This determines how fine the decoration detail can be.
    /// Higher values allow more detail but increase memory/rendering cost.
    /// </summary>
    public int Resolution { get; set; } = 4;

    // List of mini-voxel positions and colors
    public List<MiniVoxelData> MiniVoxels { get; set; } = new();

    // Biomes where this decoration can spawn (empty = all biomes)
    public List<BiomeType> AllowedBiomes { get; set; } = new();

    // Density/spawn chance (0.0 to 1.0)
    public float Density { get; set; } = 0.3f;

    // Ground blocks required for this decoration to spawn
    public List<VoxelType> RequiredGroundBlocks { get; set; } = new();

    [JsonIgnore]
    public string FileName => $"Decoration_{Name}.json";

    public Decoration()
    {
    }

    public Decoration(string name, int resolution = 4)
    {
        Name = name;
        Resolution = resolution;
    }

    public void AddMiniVoxel(Vector3Int localPosition, Color color)
    {
        // Remove existing mini-voxel at this position if any
        MiniVoxels.RemoveAll(v => v.Position == localPosition);

        MiniVoxels.Add(new MiniVoxelData
        {
            Position = localPosition,
            Color = color
        });
    }

    public void RemoveMiniVoxel(Vector3Int localPosition)
    {
        MiniVoxels.RemoveAll(v => v.Position == localPosition);
    }

    public bool CanSpawnInBiome(BiomeType biome)
    {
        // If no biomes specified, can spawn in all biomes
        if (AllowedBiomes.Count == 0)
            return true;

        return AllowedBiomes.Contains(biome);
    }

    public bool CanSpawnOnGround(VoxelType groundBlock)
    {
        // If no ground blocks specified, can spawn on any solid block
        if (RequiredGroundBlocks.Count == 0)
            return groundBlock.IsSolid() && groundBlock != VoxelType.Water;

        return RequiredGroundBlocks.Contains(groundBlock);
    }

    public void Save(string decorationsPath)
    {
        Directory.CreateDirectory(decorationsPath);

        string filePath = Path.Combine(decorationsPath, FileName);
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static Decoration? Load(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Decoration>(json);
    }

    public static List<Decoration> LoadAll(string decorationsPath)
    {
        if (!Directory.Exists(decorationsPath))
            return new List<Decoration>();

        var decorations = new List<Decoration>();
        foreach (var file in Directory.GetFiles(decorationsPath, "Decoration_*.json"))
        {
            var decoration = Load(file);
            if (decoration != null)
                decorations.Add(decoration);
        }

        return decorations;
    }
}

public class MiniVoxelData
{
    public Vector3Int Position { get; set; }
    public Color Color { get; set; }
}

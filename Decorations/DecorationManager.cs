using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Decorations;

/// <summary>
/// Manages loading and saving of decorations
/// </summary>
public class DecorationManager
{
    public List<Decoration> Decorations { get; private set; } = new();
    private string decorationsPath;

    public DecorationManager(string basePath = "Decorations")
    {
        decorationsPath = basePath;
        Directory.CreateDirectory(decorationsPath);
        LoadDecorations();
        EnsureDefaultDecorations();
    }

    private void LoadDecorations()
    {
        Decorations = Decoration.LoadAll(decorationsPath);
    }

    private void EnsureDefaultDecorations()
    {
        if (Decorations.Count == 0)
        {
            CreateDefaultDecorations();
        }
    }

    private void CreateDefaultDecorations()
    {
        // Create a simple grass decoration
        var grass = new Decoration("TallGrass", 4);
        grass.Density = 0.4f;
        grass.RequiredGroundBlocks.Add(VoxelType.Grass);
        grass.AllowedBiomes.Add(BiomeType.Plains);
        grass.AllowedBiomes.Add(BiomeType.Forest);

        // Create grass blades (simple cross pattern)
        var grassColor = new Vector3(0.3f, 0.7f, 0.2f);
        // Vertical blades
        for (int y = 0; y < 3; y++)
        {
            grass.AddMiniVoxel(new Vector3Int(1, y, 1), grassColor);
            grass.AddMiniVoxel(new Vector3Int(2, y, 2), grassColor);
            grass.AddMiniVoxel(new Vector3Int(1, y, 2), grassColor);
            grass.AddMiniVoxel(new Vector3Int(2, y, 1), grassColor);
        }

        SaveDecoration(grass);

        // Create a simple flower decoration
        var flower = new Decoration("Flower", 4);
        flower.Density = 0.2f;
        flower.RequiredGroundBlocks.Add(VoxelType.Grass);
        flower.AllowedBiomes.Add(BiomeType.Plains);
        flower.AllowedBiomes.Add(BiomeType.Forest);

        // Flower stem (green)
        var stemColor = new Vector3(0.2f, 0.6f, 0.2f);
        flower.AddMiniVoxel(new Vector3Int(2, 0, 2), stemColor);
        flower.AddMiniVoxel(new Vector3Int(2, 1, 2), stemColor);

        // Flower petals (red)
        var petalColor = new Vector3(0.9f, 0.2f, 0.2f);
        flower.AddMiniVoxel(new Vector3Int(2, 2, 2), petalColor); // Center
        flower.AddMiniVoxel(new Vector3Int(1, 2, 2), petalColor); // Left
        flower.AddMiniVoxel(new Vector3Int(3, 2, 2), petalColor); // Right
        flower.AddMiniVoxel(new Vector3Int(2, 2, 1), petalColor); // Front
        flower.AddMiniVoxel(new Vector3Int(2, 2, 3), petalColor); // Back

        SaveDecoration(flower);

        Decorations.Add(grass);
        Decorations.Add(flower);
    }

    public void SaveDecoration(Decoration decoration)
    {
        decoration.Save(decorationsPath);
    }

    public void ReloadDecorations()
    {
        LoadDecorations();
    }

    public List<Decoration> GetDecorationsForBiome(BiomeType biome)
    {
        return Decorations.Where(d => d.CanSpawnInBiome(biome)).ToList();
    }
}

using Newtonsoft.Json;
using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.Models;

/// <summary>
/// Blockbench model loader for voxel-based models
/// Supports .bbmodel format (Blockbench native format)
/// </summary>
public class BlockbenchModel
{
    public string Name { get; set; } = "Unnamed Model";
    public List<ModelCube> Cubes { get; set; } = new();
    public Vector3 Size { get; set; }

    public static BlockbenchModel? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Model file not found: {filePath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var bbData = JsonConvert.DeserializeObject<BlockbenchData>(json);

            if (bbData == null)
                return null;

            var model = new BlockbenchModel
            {
                Name = bbData.name ?? "Unnamed"
            };

            // Convert Blockbench elements to our cubes
            if (bbData.elements != null)
            {
                foreach (var element in bbData.elements)
                {
                    var cube = new ModelCube
                    {
                        From = new Vector3(element.from[0], element.from[1], element.from[2]) / 16.0f,
                        To = new Vector3(element.to[0], element.to[1], element.to[2]) / 16.0f,
                        Color = ParseColor(element.color)
                    };

                    model.Cubes.Add(cube);
                }
            }

            return model;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Blockbench model: {ex.Message}");
            return null;
        }
    }

    private static Vector3 ParseColor(int colorInt)
    {
        // Blockbench stores colors as integers (0-9)
        // Map them to nice voxel colors
        return colorInt switch
        {
            0 => new Vector3(0.8f, 0.3f, 0.3f),  // Red
            1 => new Vector3(0.3f, 0.8f, 0.3f),  // Green
            2 => new Vector3(0.3f, 0.3f, 0.8f),  // Blue
            3 => new Vector3(0.8f, 0.8f, 0.3f),  // Yellow
            4 => new Vector3(0.8f, 0.3f, 0.8f),  // Magenta
            5 => new Vector3(0.3f, 0.8f, 0.8f),  // Cyan
            6 => new Vector3(0.9f, 0.6f, 0.3f),  // Orange
            7 => new Vector3(0.6f, 0.4f, 0.2f),  // Brown
            8 => new Vector3(0.9f, 0.9f, 0.9f),  // White
            9 => new Vector3(0.3f, 0.3f, 0.3f),  // Dark gray
            _ => new Vector3(0.7f, 0.7f, 0.7f)   // Light gray
        };
    }

    /// <summary>
    /// Convert Blockbench model to a Structure for placement in world
    /// </summary>
    public Structure ToStructure(StructureCategory category = StructureCategory.Ambient)
    {
        var structure = new Structure(Name, category);

        foreach (var cube in Cubes)
        {
            // Convert cube to voxels
            Vector3Int minPos = new Vector3Int(
                (int)MathF.Floor(cube.From.X),
                (int)MathF.Floor(cube.From.Y),
                (int)MathF.Floor(cube.From.Z)
            );

            Vector3Int maxPos = new Vector3Int(
                (int)MathF.Ceiling(cube.To.X),
                (int)MathF.Ceiling(cube.To.Y),
                (int)MathF.Ceiling(cube.To.Z)
            );

            // Fill the cube with voxels
            for (int x = minPos.X; x < maxPos.X; x++)
            {
                for (int y = minPos.Y; y < maxPos.Y; y++)
                {
                    for (int z = minPos.Z; z < maxPos.Z; z++)
                    {
                        // Choose voxel type based on color
                        VoxelType type = ColorToVoxelType(cube.Color);
                        structure.AddVoxel(new Vector3Int(x, y, z), type);
                    }
                }
            }
        }

        return structure;
    }

    private VoxelType ColorToVoxelType(Vector3 color)
    {
        // Match color to closest voxel type
        float r = color.X;
        float g = color.Y;
        float b = color.Z;

        if (g > r && g > b) return VoxelType.Grass;    // Green -> Grass
        if (r > g && b < 0.4f) return VoxelType.Brick; // Red -> Brick
        if (b > r && b > g) return VoxelType.Water;    // Blue -> Water
        if (r > 0.5f && g > 0.4f) return VoxelType.Sand; // Yellow -> Sand
        if (r < 0.5f && g < 0.5f && b < 0.5f) return VoxelType.Stone; // Dark -> Stone

        return VoxelType.Dirt; // Default
    }
}

public class ModelCube
{
    public Vector3 From { get; set; }
    public Vector3 To { get; set; }
    public Vector3 Color { get; set; }
}

// Blockbench file format structure
#pragma warning disable CS8618
internal class BlockbenchData
{
    public string? name { get; set; }
    public BlockbenchElement[]? elements { get; set; }
}

internal class BlockbenchElement
{
    public float[] from { get; set; } = new float[3];
    public float[] to { get; set; } = new float[3];
    public int color { get; set; }
}
#pragma warning restore CS8618

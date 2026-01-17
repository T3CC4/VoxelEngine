using Newtonsoft.Json;
using VoxelEngine.Core;

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

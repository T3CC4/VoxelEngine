using VoxelEngine.Core;

namespace VoxelEngine.Structures;

public class StructureManager
{
    private List<Structure> architectureStructures = new();
    private List<Structure> ambientStructures = new();
    private string structuresPath;

    public IReadOnlyList<Structure> ArchitectureStructures => architectureStructures;
    public IReadOnlyList<Structure> AmbientStructures => ambientStructures;

    public StructureManager(string basePath = "Structures")
    {
        structuresPath = basePath;
        Directory.CreateDirectory(structuresPath);
        Directory.CreateDirectory(Path.Combine(structuresPath, "Architecture"));
        Directory.CreateDirectory(Path.Combine(structuresPath, "Ambient"));

        LoadAll();
        CreateDefaultStructures();
    }

    public void LoadAll()
    {
        architectureStructures = Structure.LoadAllFromCategory(structuresPath, StructureCategory.Architecture);
        ambientStructures = Structure.LoadAllFromCategory(structuresPath, StructureCategory.Ambient);
    }

    public void SaveStructure(Structure structure)
    {
        structure.Save(structuresPath);

        // Reload the category
        if (structure.Category == StructureCategory.Architecture)
        {
            architectureStructures = Structure.LoadAllFromCategory(structuresPath, StructureCategory.Architecture);
        }
        else
        {
            ambientStructures = Structure.LoadAllFromCategory(structuresPath, StructureCategory.Ambient);
        }
    }

    public Structure? GetStructure(string name, StructureCategory category)
    {
        var list = category == StructureCategory.Architecture ? architectureStructures : ambientStructures;
        return list.FirstOrDefault(s => s.Name == name);
    }

    private void CreateDefaultStructures()
    {
        // Create default tree if it doesn't exist
        if (!ambientStructures.Any(s => s.Name == "Oak Tree"))
        {
            var tree = CreateDefaultTree();
            SaveStructure(tree);
        }

        // Create default house if it doesn't exist
        if (!architectureStructures.Any(s => s.Name == "Simple House"))
        {
            var house = CreateDefaultHouse();
            SaveStructure(house);
        }
    }

    private Structure CreateDefaultTree()
    {
        var tree = new Structure("Oak Tree", StructureCategory.Ambient);

        // Trunk (5 blocks tall)
        for (int y = 0; y < 5; y++)
        {
            tree.AddVoxel(new Vector3Int(0, y, 0), VoxelType.Wood);
        }

        // Leaves (simple sphere-ish shape)
        for (int x = -2; x <= 2; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    int distance = Math.Abs(x) + Math.Abs(y - 4) + Math.Abs(z);
                    if (distance <= 3)
                    {
                        tree.AddVoxel(new Vector3Int(x, y, z), VoxelType.Leaves);
                    }
                }
            }
        }

        return tree;
    }

    private Structure CreateDefaultHouse()
    {
        var house = new Structure("Simple House", StructureCategory.Architecture);

        // Floor (5x5)
        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                house.AddVoxel(new Vector3Int(x, 0, z), VoxelType.Wood);
            }
        }

        // Walls (4 blocks high)
        for (int y = 1; y <= 4; y++)
        {
            // Front and back walls
            for (int x = 0; x < 5; x++)
            {
                if (y != 2 || x != 2) // Door at front center
                    house.AddVoxel(new Vector3Int(x, y, 0), VoxelType.Brick);
                house.AddVoxel(new Vector3Int(x, y, 4), VoxelType.Brick);
            }

            // Side walls
            for (int z = 1; z < 4; z++)
            {
                house.AddVoxel(new Vector3Int(0, y, z), VoxelType.Brick);
                house.AddVoxel(new Vector3Int(4, y, z), VoxelType.Brick);
            }
        }

        // Roof (pyramid style)
        for (int level = 0; level < 3; level++)
        {
            int y = 5 + level;
            int offset = level;

            for (int x = offset; x < 5 - offset; x++)
            {
                for (int z = offset; z < 5 - offset; z++)
                {
                    house.AddVoxel(new Vector3Int(x, y, z), VoxelType.Wood);
                }
            }
        }

        return house;
    }
}

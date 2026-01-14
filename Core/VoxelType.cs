using OpenTK.Mathematics;

namespace VoxelEngine.Core;

public enum VoxelType
{
    Air = 0,
    Grass = 1,
    Dirt = 2,
    Stone = 3,
    Wood = 4,
    Leaves = 5,
    Sand = 6,
    Water = 7,
    Brick = 8,
    Glass = 9
}

public static class VoxelTypeExtensions
{
    public static bool IsSolid(this VoxelType type)
    {
        return type != VoxelType.Air && type != VoxelType.Water;
    }

    public static Vector3 GetColor(this VoxelType type)
    {
        return type switch
        {
            VoxelType.Air => new Vector3(0, 0, 0),
            VoxelType.Grass => new Vector3(0.2f, 0.8f, 0.2f),        // Bright green
            VoxelType.Dirt => new Vector3(0.6f, 0.4f, 0.2f),         // Brown
            VoxelType.Stone => new Vector3(0.5f, 0.5f, 0.5f),        // Gray
            VoxelType.Wood => new Vector3(0.4f, 0.25f, 0.15f),       // Dark brown
            VoxelType.Leaves => new Vector3(0.15f, 0.6f, 0.15f),     // Dark green
            VoxelType.Sand => new Vector3(0.9f, 0.85f, 0.5f),        // Yellow sand
            VoxelType.Water => new Vector3(0.2f, 0.4f, 0.9f),        // Blue
            VoxelType.Brick => new Vector3(0.8f, 0.2f, 0.2f),        // Red brick
            VoxelType.Glass => new Vector3(0.6f, 0.8f, 0.9f),        // Light cyan
            _ => new Vector3(1, 0, 1)                                 // Magenta for unknown
        };
    }

    public static char GetDisplayChar(this VoxelType type)
    {
        return type switch
        {
            VoxelType.Air => ' ',
            VoxelType.Grass => '▓',
            VoxelType.Dirt => '▒',
            VoxelType.Stone => '█',
            VoxelType.Wood => '║',
            VoxelType.Leaves => '♣',
            VoxelType.Sand => '░',
            VoxelType.Water => '≈',
            VoxelType.Brick => '■',
            VoxelType.Glass => '□',
            _ => '?'
        };
    }

    public static ConsoleColor GetDisplayColor(this VoxelType type)
    {
        return type switch
        {
            VoxelType.Air => ConsoleColor.Black,
            VoxelType.Grass => ConsoleColor.Green,
            VoxelType.Dirt => ConsoleColor.DarkYellow,
            VoxelType.Stone => ConsoleColor.Gray,
            VoxelType.Wood => ConsoleColor.DarkRed,
            VoxelType.Leaves => ConsoleColor.DarkGreen,
            VoxelType.Sand => ConsoleColor.Yellow,
            VoxelType.Water => ConsoleColor.Blue,
            VoxelType.Brick => ConsoleColor.Red,
            VoxelType.Glass => ConsoleColor.Cyan,
            _ => ConsoleColor.White
        };
    }
}

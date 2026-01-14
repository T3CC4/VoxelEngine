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

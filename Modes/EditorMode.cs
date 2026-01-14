namespace VoxelEngine.Modes;

using VoxelEngine.Core;
using VoxelEngine.Rendering;

public class EditorMode : IGameMode
{
    private readonly VoxelWorld world;
    private readonly ConsoleRenderer renderer;
    private Vector3Int cursorPosition;
    private VoxelType selectedVoxelType;
    private bool isRunning;

    public EditorMode(VoxelWorld world, ConsoleRenderer renderer)
    {
        this.world = world;
        this.renderer = renderer;
        cursorPosition = new Vector3Int(8, 0, 8);
        selectedVoxelType = VoxelType.Grass;
        isRunning = true;
    }

    public string ModeName => "EDITOR";

    public void Update()
    {
        renderer.RenderWorld(world, cursorPosition);
        renderer.RenderUI(ModeName, cursorPosition, selectedVoxelType, world);
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                cursorPosition += Vector3Int.Left;
                break;
            case ConsoleKey.RightArrow:
                cursorPosition += Vector3Int.Right;
                break;
            case ConsoleKey.UpArrow:
                cursorPosition += Vector3Int.Back;
                break;
            case ConsoleKey.DownArrow:
                cursorPosition += Vector3Int.Forward;
                break;
            case ConsoleKey.W:
                renderer.MoveViewLayer(1);
                cursorPosition += Vector3Int.Up;
                break;
            case ConsoleKey.S:
                if (renderer.GetCurrentLayer() > 0)
                {
                    renderer.MoveViewLayer(-1);
                    cursorPosition += Vector3Int.Down;
                }
                break;
            case ConsoleKey.Spacebar:
                PlaceVoxel();
                break;
            case ConsoleKey.Delete:
            case ConsoleKey.Backspace:
                RemoveVoxel();
                break;
            case ConsoleKey.D1:
                selectedVoxelType = VoxelType.Grass;
                break;
            case ConsoleKey.D2:
                selectedVoxelType = VoxelType.Dirt;
                break;
            case ConsoleKey.D3:
                selectedVoxelType = VoxelType.Stone;
                break;
            case ConsoleKey.D4:
                selectedVoxelType = VoxelType.Wood;
                break;
            case ConsoleKey.D5:
                selectedVoxelType = VoxelType.Leaves;
                break;
            case ConsoleKey.D6:
                selectedVoxelType = VoxelType.Sand;
                break;
            case ConsoleKey.D7:
                selectedVoxelType = VoxelType.Water;
                break;
            case ConsoleKey.D8:
                selectedVoxelType = VoxelType.Brick;
                break;
            case ConsoleKey.D9:
                selectedVoxelType = VoxelType.Glass;
                break;
            case ConsoleKey.M:
                return true;
            case ConsoleKey.Escape:
                isRunning = false;
                break;
        }
        return false;
    }

    private void PlaceVoxel()
    {
        world.SetVoxelType(cursorPosition, selectedVoxelType);
    }

    private void RemoveVoxel()
    {
        world.SetVoxelType(cursorPosition, VoxelType.Air);
    }

    public bool IsRunning() => isRunning;

    public void SaveStructure(string filename)
    {
        var lines = new List<string>();
        lines.Add($"VoxelStructure v1.0");
        lines.Add($"Size: {world.WorldSize}");

        foreach (var chunk in world.GetAllChunks())
        {
            foreach (var (pos, voxel) in chunk.GetAllVoxels())
            {
                var worldPos = new Vector3Int(
                    chunk.Position.X * Chunk.ChunkSize + pos.X,
                    chunk.Position.Y * Chunk.ChunkSize + pos.Y,
                    chunk.Position.Z * Chunk.ChunkSize + pos.Z
                );
                lines.Add($"{worldPos.X},{worldPos.Y},{worldPos.Z},{(int)voxel.Type}");
            }
        }

        File.WriteAllLines(filename, lines);
    }

    public void LoadStructure(string filename)
    {
        if (!File.Exists(filename))
            return;

        var lines = File.ReadAllLines(filename);
        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split(',');
            if (parts.Length == 4)
            {
                var pos = new Vector3Int(
                    int.Parse(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2])
                );
                var type = (VoxelType)int.Parse(parts[3]);
                world.SetVoxelType(pos, type);
            }
        }
    }
}

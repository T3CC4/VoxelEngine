namespace VoxelEngine.Rendering;

using VoxelEngine.Core;

public class ConsoleRenderer
{
    private Vector3Int viewPosition;
    private int viewLayer;
    private readonly int viewWidth;
    private readonly int viewHeight;

    public ConsoleRenderer(int width = 32, int height = 16)
    {
        viewWidth = width;
        viewHeight = height;
        viewPosition = Vector3Int.Zero;
        viewLayer = 0;
    }

    public void SetViewPosition(Vector3Int position)
    {
        viewPosition = position;
    }

    public void SetViewLayer(int layer)
    {
        viewLayer = Math.Max(0, layer);
    }

    public void MoveViewLayer(int delta)
    {
        viewLayer = Math.Max(0, viewLayer + delta);
    }

    public int GetCurrentLayer() => viewLayer;

    public void RenderWorld(VoxelWorld world, Vector3Int? cursorPosition = null)
    {
        Console.Clear();
        Console.CursorVisible = false;

        for (int z = 0; z < viewHeight; z++)
        {
            for (int x = 0; x < viewWidth; x++)
            {
                var worldPos = new Vector3Int(
                    viewPosition.X + x,
                    viewLayer,
                    viewPosition.Z + z
                );

                var voxel = world.GetVoxel(worldPos);
                bool isCursor = cursorPosition.HasValue && cursorPosition.Value == worldPos;

                if (isCursor)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write('X');
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = voxel.Type.GetDisplayColor();
                    Console.Write(voxel.Type.GetDisplayChar());
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }
    }

    public void RenderUI(string mode, Vector3Int cursorPosition, VoxelType selectedType, VoxelWorld world)
    {
        Console.WriteLine();
        Console.WriteLine($"═══════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Mode: {mode}");
        Console.ResetColor();
        Console.WriteLine($"Layer (Y): {viewLayer}");
        Console.WriteLine($"Cursor: {cursorPosition}");
        Console.WriteLine($"Selected: {selectedType} ({selectedType.GetDisplayChar()})");
        Console.WriteLine($"Voxel at cursor: {world.GetVoxel(cursorPosition).Type}");
        Console.WriteLine();
        Console.WriteLine("Controls:");
        Console.WriteLine("  Arrow Keys: Move cursor (X/Z)");
        Console.WriteLine("  W/S: Move layer up/down (Y)");
        Console.WriteLine("  Space: Place voxel");
        Console.WriteLine("  Delete: Remove voxel");
        Console.WriteLine("  1-9: Select voxel type");
        Console.WriteLine("  M: Switch mode (Editor/Game)");
        Console.WriteLine("  Esc: Exit");
    }

    public void RenderGameView(VoxelWorld world, Vector3Int playerPosition)
    {
        Console.Clear();
        Console.CursorVisible = false;

        int renderDistance = 16;
        int renderHeight = 12;

        for (int z = 0; z < renderHeight; z++)
        {
            for (int x = 0; x < viewWidth; x++)
            {
                var worldPos = new Vector3Int(
                    playerPosition.X - viewWidth / 2 + x,
                    playerPosition.Y,
                    playerPosition.Z - renderHeight / 2 + z
                );

                var voxel = world.GetVoxel(worldPos);
                bool isPlayer = worldPos == playerPosition;

                if (isPlayer)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write('@');
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = voxel.Type.GetDisplayColor();
                    Console.Write(voxel.Type.GetDisplayChar());
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine($"═══════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("GAME MODE");
        Console.ResetColor();
        Console.WriteLine($"Player Position: {playerPosition}");
        Console.WriteLine($"Standing on: {world.GetVoxel(playerPosition + Vector3Int.Down).Type}");
        Console.WriteLine();
        Console.WriteLine("Controls:");
        Console.WriteLine("  Arrow Keys: Move player (X/Z)");
        Console.WriteLine("  W/S: Move up/down (Y)");
        Console.WriteLine("  M: Switch to Editor mode");
        Console.WriteLine("  Esc: Exit");
    }
}

namespace VoxelEngine.Engine;

using VoxelEngine.Core;
using VoxelEngine.Modes;
using VoxelEngine.Rendering;

public class VoxelEngineApp
{
    private readonly VoxelWorld world;
    private readonly ConsoleRenderer renderer;
    private readonly ModeManager modeManager;
    private bool isRunning;

    public VoxelEngineApp()
    {
        world = new VoxelWorld(new Vector3Int(2, 2, 2));
        renderer = new ConsoleRenderer(32, 16);
        modeManager = new ModeManager();
        isRunning = true;

        InitializeModes();
    }

    private void InitializeModes()
    {
        var editorMode = new EditorMode(world, renderer);
        var gameMode = new GameMode(world, renderer);

        modeManager.RegisterMode("editor", editorMode);
        modeManager.RegisterMode("game", gameMode);
        modeManager.SetMode("editor");
    }

    public void Run()
    {
        Console.Clear();
        Console.WriteLine("Initializing VoxelEngine...");
        Console.WriteLine("Generating terrain...");
        world.GenerateTestTerrain();

        Thread.Sleep(1000);

        while (isRunning && modeManager.IsRunning())
        {
            modeManager.Update();

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                bool switchMode = modeManager.HandleInput(key);

                if (switchMode)
                {
                    modeManager.SwitchToNextMode();
                }

                if (key.Key == ConsoleKey.Escape)
                {
                    isRunning = false;
                }
            }

            Thread.Sleep(100);
        }

        Console.Clear();
        Console.WriteLine("VoxelEngine shut down. Goodbye!");
    }

    public static void ShowWelcomeScreen()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    VOXEL ENGINE                          ║");
        Console.WriteLine("║                      v1.0.0                              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("A powerful voxel-based world editor and game engine.");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  • Editor Mode: Create and edit voxel structures");
        Console.WriteLine("  • Game Mode: Explore your creations in real-time");
        Console.WriteLine("  • Multiple voxel types with unique properties");
        Console.WriteLine("  • Chunk-based world management");
        Console.WriteLine();
        Console.WriteLine("Press any key to start...");
        Console.ReadKey(true);
    }
}

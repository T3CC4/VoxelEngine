using VoxelEngine.Window;

class Program
{
    // ========================================
    // MODE SELECTION - Change this to switch between Editor and Game mode
    // ========================================
    private const bool EDITOR_MODE = true;  // Set to false for Game Mode
    // ========================================

    static void Main(string[] args)
    {
        try
        {
            ShowWelcomeScreen();

            using var game = new VoxelGameWindow(EDITOR_MODE);
            game.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    static void ShowWelcomeScreen()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                    VOXEL ENGINE                              ║
║                  Trove-Style Voxel Editor                    ║
╚══════════════════════════════════════════════════════════════╝
        ");
        Console.ResetColor();

        Console.WriteLine($"Mode: {(EDITOR_MODE ? "EDITOR" : "GAME")}");
        Console.WriteLine();

        if (EDITOR_MODE)
        {
            Console.WriteLine("Editor Mode Features:");
            Console.WriteLine("  • 3D Voxel Editor with ImGui UI");
            Console.WriteLine("  • Structure System (Architecture & Ambient)");
            Console.WriteLine("  • Play Mode for testing");
            Console.WriteLine("  • Save/Load structures");
        }
        else
        {
            Console.WriteLine("Game Mode Features:");
            Console.WriteLine("  • FPS controls (WASD + Mouse)");
            Console.WriteLine("  • Physics and collision");
            Console.WriteLine("  • Explore the voxel world");
        }

        Console.WriteLine();
        Console.WriteLine("Starting in 2 seconds...");
        Thread.Sleep(2000);
    }
}

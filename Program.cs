using VoxelEngine.Engine;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            VoxelEngineApp.ShowWelcomeScreen();

            var app = new VoxelEngineApp();
            app.Run();
        }
        catch (Exception ex)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An error occurred:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

namespace VoxelEngine.Modes;

public interface IGameMode
{
    string ModeName { get; }
    void Update();
    bool HandleInput(ConsoleKeyInfo key);
    bool IsRunning();
}

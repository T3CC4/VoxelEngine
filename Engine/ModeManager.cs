namespace VoxelEngine.Engine;

using VoxelEngine.Modes;

public class ModeManager
{
    private IGameMode? currentMode;
    private readonly Dictionary<string, IGameMode> modes;

    public ModeManager()
    {
        modes = new Dictionary<string, IGameMode>();
    }

    public void RegisterMode(string name, IGameMode mode)
    {
        modes[name] = mode;
    }

    public void SetMode(string name)
    {
        if (modes.TryGetValue(name, out var mode))
        {
            currentMode = mode;
        }
    }

    public IGameMode? GetCurrentMode() => currentMode;

    public void SwitchToNextMode()
    {
        if (modes.Count == 0) return;

        var modeList = modes.Values.ToList();
        if (currentMode == null)
        {
            currentMode = modeList[0];
            return;
        }

        int currentIndex = modeList.IndexOf(currentMode);
        int nextIndex = (currentIndex + 1) % modeList.Count;
        currentMode = modeList[nextIndex];
    }

    public void Update()
    {
        currentMode?.Update();
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        return currentMode?.HandleInput(key) ?? false;
    }

    public bool IsRunning()
    {
        return currentMode?.IsRunning() ?? false;
    }
}

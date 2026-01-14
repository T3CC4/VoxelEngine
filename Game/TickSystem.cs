namespace VoxelEngine.Game;

public class TickSystem
{
    private const int TicksPerSecond = 64;
    private const float TickDuration = 1.0f / TicksPerSecond;

    private float accumulator = 0.0f;
    private long currentTick = 0;

    public long CurrentTick => currentTick;
    public int TickRate => TicksPerSecond;

    private List<Action> tickActions = new();

    public void RegisterTickAction(Action action)
    {
        tickActions.Add(action);
    }

    public void Update(float deltaTime)
    {
        accumulator += deltaTime;

        while (accumulator >= TickDuration)
        {
            Tick();
            accumulator -= TickDuration;
            currentTick++;
        }
    }

    private void Tick()
    {
        foreach (var action in tickActions)
        {
            action.Invoke();
        }
    }

    public void Clear()
    {
        tickActions.Clear();
    }
}

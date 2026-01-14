namespace VoxelEngine.Modes;

using VoxelEngine.Core;
using VoxelEngine.Rendering;

public class GameMode : IGameMode
{
    private readonly VoxelWorld world;
    private readonly ConsoleRenderer renderer;
    private Vector3Int playerPosition;
    private bool isRunning;
    private double gravity = -0.5;
    private double verticalVelocity = 0;
    private bool isOnGround = false;

    public GameMode(VoxelWorld world, ConsoleRenderer renderer)
    {
        this.world = world;
        this.renderer = renderer;
        playerPosition = FindSpawnPosition();
        isRunning = true;
    }

    public string ModeName => "GAME";

    private Vector3Int FindSpawnPosition()
    {
        int maxY = world.WorldSize.Y * Chunk.ChunkSize;
        for (int y = maxY - 1; y >= 0; y--)
        {
            var testPos = new Vector3Int(8, y, 8);
            var voxel = world.GetVoxel(testPos);
            var below = world.GetVoxel(testPos + Vector3Int.Down);

            if (!voxel.IsSolid && below.IsSolid)
            {
                return testPos;
            }
        }
        return new Vector3Int(8, 10, 8);
    }

    public void Update()
    {
        ApplyPhysics();
        renderer.RenderGameView(world, playerPosition);
    }

    private void ApplyPhysics()
    {
        var belowPos = playerPosition + Vector3Int.Down;
        var voxelBelow = world.GetVoxel(belowPos);
        isOnGround = voxelBelow.IsSolid;

        if (!isOnGround)
        {
            verticalVelocity += gravity * 0.1;
            verticalVelocity = Math.Max(verticalVelocity, -2.0);

            int moveSteps = (int)Math.Abs(verticalVelocity);
            for (int i = 0; i < moveSteps; i++)
            {
                var nextPos = playerPosition + Vector3Int.Down;
                if (!world.GetVoxel(nextPos).IsSolid)
                {
                    playerPosition = nextPos;
                }
                else
                {
                    isOnGround = true;
                    verticalVelocity = 0;
                    break;
                }
            }
        }
        else
        {
            verticalVelocity = 0;
        }
    }

    public bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                TryMove(Vector3Int.Left);
                break;
            case ConsoleKey.RightArrow:
                TryMove(Vector3Int.Right);
                break;
            case ConsoleKey.UpArrow:
                TryMove(Vector3Int.Back);
                break;
            case ConsoleKey.DownArrow:
                TryMove(Vector3Int.Forward);
                break;
            case ConsoleKey.W:
                TryMove(Vector3Int.Up);
                break;
            case ConsoleKey.S:
                TryMove(Vector3Int.Down);
                break;
            case ConsoleKey.Spacebar:
                if (isOnGround)
                {
                    Jump();
                }
                break;
            case ConsoleKey.M:
                return true;
            case ConsoleKey.Escape:
                isRunning = false;
                break;
        }
        return false;
    }

    private void TryMove(Vector3Int direction)
    {
        var newPosition = playerPosition + direction;
        var voxel = world.GetVoxel(newPosition);

        if (!voxel.IsSolid)
        {
            playerPosition = newPosition;
        }
    }

    private void Jump()
    {
        verticalVelocity = 2.0;
        isOnGround = false;

        for (int i = 0; i < 2; i++)
        {
            var nextPos = playerPosition + Vector3Int.Up;
            if (!world.GetVoxel(nextPos).IsSolid)
            {
                playerPosition = nextPos;
            }
        }
    }

    public bool IsRunning() => isRunning;
}

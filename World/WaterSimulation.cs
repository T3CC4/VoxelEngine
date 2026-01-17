using OpenTK.Mathematics;
using VoxelEngine.Core;
using VoxelEngine.Game;

namespace VoxelEngine.World;

public class WaterSimulation
{
    private IVoxelWorld world;
    private TickSystem tickSystem;
    private int waterLevel;
    private int tickCounter = 0;
    private const int WaterTickInterval = 2; // Update water every 2 ticks for performance
    private HashSet<Vector3Int> waterBlocks = new();
    private Queue<Vector3Int> updateQueue = new();
    private HashSet<Vector3Int> changedPositions = new();

    public WaterSimulation(IVoxelWorld world, TickSystem tickSystem, int waterLevel)
    {
        this.world = world;
        this.tickSystem = tickSystem;
        this.waterLevel = waterLevel;

        // Register tick action
        tickSystem.RegisterTickAction(OnTick);

        // Initialize water block tracking (only for finite worlds)
        if (world is VoxelWorld finiteWorld)
        {
            ScanForWaterBlocks(finiteWorld);
        }
    }

    private void ScanForWaterBlocks(VoxelWorld finiteWorld)
    {
        waterBlocks.Clear();
        int maxX = finiteWorld.WorldSize.X * Chunk.ChunkSize;
        int maxY = finiteWorld.WorldSize.Y * Chunk.ChunkSize;
        int maxZ = finiteWorld.WorldSize.Z * Chunk.ChunkSize;

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                for (int z = 0; z < maxZ; z++)
                {
                    var pos = new Vector3Int(x, y, z);
                    var voxel = world.GetVoxel(pos);
                    if (voxel.Type == VoxelType.Water)
                    {
                        waterBlocks.Add(pos);
                    }
                }
            }
        }

        Console.WriteLine($"Water simulation initialized with {waterBlocks.Count} water blocks");
    }

    private void OnTick()
    {
        tickCounter++;
        if (tickCounter % WaterTickInterval != 0)
            return;

        // Only process water in the update queue (triggered by block changes)
        if (updateQueue.Count == 0)
            return;

        UpdateWater();
    }

    private void UpdateWater()
    {
        // Process only a limited number of water blocks per tick for performance
        int processCount = Math.Min(updateQueue.Count, 50);

        for (int i = 0; i < processCount; i++)
        {
            if (updateQueue.Count == 0)
                break;

            var waterPos = updateQueue.Dequeue();

            // Verify it's still water
            var voxel = world.GetVoxel(waterPos);
            if (voxel.Type != VoxelType.Water)
            {
                waterBlocks.Remove(waterPos);
                continue;
            }

            // Check if water should flow down
            Vector3Int below = waterPos + new Vector3Int(0, -1, 0);
            var belowVoxel = world.GetVoxel(below);

            if (belowVoxel.IsActive && belowVoxel.Type == VoxelType.Air)
            {
                // Water flows down
                world.SetVoxelType(below, VoxelType.Water);
                world.SetVoxelType(waterPos, VoxelType.Air);
                waterBlocks.Remove(waterPos);
                waterBlocks.Add(below);
                updateQueue.Enqueue(below);
                changedPositions.Add(below);
                changedPositions.Add(waterPos);
                continue;
            }

            // Check if water should spread horizontally
            // Only spread if there's solid ground below
            if (belowVoxel.IsActive && belowVoxel.Type.IsSolid())
            {
                TrySpreadWater(waterPos);
            }
        }
    }

    private void TrySpreadWater(Vector3Int waterPos)
    {
        // Check all 4 horizontal directions
        Vector3Int[] directions = new[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3Int adjacentPos = waterPos + dir;
            var adjacentVoxel = world.GetVoxel(adjacentPos);

            // Check if adjacent position is air
            if (adjacentVoxel.IsActive && adjacentVoxel.Type == VoxelType.Air)
            {
                // Check if there's solid ground below the adjacent position
                Vector3Int belowAdjacent = adjacentPos + new Vector3Int(0, -1, 0);
                var belowAdjacentVoxel = world.GetVoxel(belowAdjacent);

                // Water spreads if there's solid ground below or if it's at/below water level
                if ((belowAdjacentVoxel.IsActive && belowAdjacentVoxel.Type.IsSolid()) ||
                    adjacentPos.Y <= waterLevel)
                {
                    world.SetVoxelType(adjacentPos, VoxelType.Water);
                    waterBlocks.Add(adjacentPos);
                    updateQueue.Enqueue(adjacentPos);
                    changedPositions.Add(adjacentPos);
                    return; // Only spread in one direction per tick for performance
                }
            }
        }
    }

    public void OnWaterPlaced(Vector3Int pos)
    {
        waterBlocks.Add(pos);
        updateQueue.Enqueue(pos);
    }

    public void OnWaterRemoved(Vector3Int pos)
    {
        waterBlocks.Remove(pos);
    }

    public void OnVoxelChanged(Vector3Int pos, VoxelType newType)
    {
        if (newType == VoxelType.Water)
        {
            OnWaterPlaced(pos);
        }
        else if (waterBlocks.Contains(pos))
        {
            OnWaterRemoved(pos);
        }

        // Check if this change affects nearby water
        CheckNearbyWater(pos);
    }

    private void CheckNearbyWater(Vector3Int pos)
    {
        // Check all 6 directions
        Vector3Int[] directions = new[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            Vector3Int checkPos = pos + dir;
            var voxel = world.GetVoxel(checkPos);
            if (voxel.Type == VoxelType.Water)
            {
                updateQueue.Enqueue(checkPos);
            }
        }
    }

    public HashSet<Vector3Int> GetAndClearChangedPositions()
    {
        var positions = new HashSet<Vector3Int>(changedPositions);
        changedPositions.Clear();
        return positions;
    }
}

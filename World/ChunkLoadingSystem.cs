using System.Collections.Concurrent;
using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.World;

public class ChunkLoadingSystem
{
    private readonly InfiniteWorldGenerator worldGenerator;
    private readonly InfiniteVoxelWorld world;

    // Chunks waiting to have terrain generated (processed in background)
    private readonly ConcurrentQueue<Vector3Int> chunksToGenerate = new();

    // Chunks with generated terrain waiting to have meshes built (processed on main thread)
    private readonly ConcurrentQueue<Chunk> chunksToMesh = new();

    // Chunks currently being generated
    private readonly HashSet<Vector3Int> generatingChunks = new();

    // Background thread for terrain generation
    private Thread? generationThread;
    private volatile bool isRunning = false;

    // Stats
    public int ChunksInGenerationQueue => chunksToGenerate.Count;
    public int ChunksInMeshQueue => chunksToMesh.Count;
    public int ChunksGenerating => generatingChunks.Count;
    public bool IsLoading => chunksToGenerate.Count > 0 || chunksToMesh.Count > 0 || generatingChunks.Count > 0;

    public ChunkLoadingSystem(InfiniteWorldGenerator worldGenerator, InfiniteVoxelWorld world)
    {
        this.worldGenerator = worldGenerator;
        this.world = world;
    }

    public void Start()
    {
        if (isRunning) return;

        isRunning = true;
        generationThread = new Thread(GenerationThreadLoop)
        {
            Name = "ChunkGenerationThread",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
        generationThread.Start();
    }

    public void Stop()
    {
        isRunning = false;
        generationThread?.Join(1000);
    }

    public void QueueChunkForGeneration(Vector3Int chunkPos)
    {
        lock (generatingChunks)
        {
            if (!generatingChunks.Contains(chunkPos) && world.HasChunk(chunkPos))
            {
                var chunk = world.GetChunk(chunkPos);
                // Only queue if chunk has no voxels generated yet
                if (chunk != null && !IsChunkGenerated(chunk))
                {
                    generatingChunks.Add(chunkPos);
                    chunksToGenerate.Enqueue(chunkPos);
                }
            }
        }
    }

    private bool IsChunkGenerated(Chunk chunk)
    {
        // Quick check - if there are any active voxels, consider it generated
        // This is a simple heuristic
        foreach (var voxel in chunk.GetVoxels())
        {
            if (voxel.IsActive)
                return true;
        }
        return false;
    }

    private void GenerationThreadLoop()
    {
        while (isRunning)
        {
            if (chunksToGenerate.TryDequeue(out Vector3Int chunkPos))
            {
                try
                {
                    var chunk = world.GetChunk(chunkPos);
                    if (chunk != null)
                    {
                        // Generate terrain (this is CPU-intensive and can run in background)
                        worldGenerator.GenerateChunk(chunk, world);

                        // Queue for mesh building on main thread
                        chunksToMesh.Enqueue(chunk);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating chunk {chunkPos}: {ex.Message}");
                }
                finally
                {
                    lock (generatingChunks)
                    {
                        generatingChunks.Remove(chunkPos);
                    }
                }
            }
            else
            {
                // No work to do, sleep briefly
                Thread.Sleep(10);
            }
        }
    }

    public List<Chunk> GetChunksReadyForMeshing(int maxPerFrame = 4)
    {
        var chunks = new List<Chunk>();

        for (int i = 0; i < maxPerFrame && chunksToMesh.TryDequeue(out Chunk? chunk); i++)
        {
            if (chunk != null)
            {
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    public void Clear()
    {
        while (chunksToGenerate.TryDequeue(out _)) { }
        while (chunksToMesh.TryDequeue(out _)) { }

        lock (generatingChunks)
        {
            generatingChunks.Clear();
        }
    }
}

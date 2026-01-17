using System.Collections.Concurrent;
using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.World;

public class ChunkLoadingSystem
{
    private readonly InfiniteWorldGenerator worldGenerator;
    private readonly InfiniteVoxelWorld world;

    // Priority queue for chunk generation (closer chunks processed first)
    private readonly PriorityQueue<Vector3Int, float> priorityChunksToGenerate = new();
    private readonly object queueLock = new object();

    // Chunks with generated terrain waiting to have meshes built (processed on main thread)
    private readonly ConcurrentQueue<Chunk> chunksToMesh = new();

    // Chunks currently being generated
    private readonly HashSet<Vector3Int> generatingChunks = new();

    // Background thread for terrain generation
    private Thread? generationThread;
    private volatile bool isRunning = false;

    // Camera position for priority calculation
    private Vector3 cameraPosition = Vector3.Zero;

    // Stats
    public int ChunksInGenerationQueue { get; private set; }
    public int ChunksInMeshQueue => chunksToMesh.Count;
    public int ChunksGenerating => generatingChunks.Count;
    public bool IsLoading => ChunksInGenerationQueue > 0 || chunksToMesh.Count > 0 || generatingChunks.Count > 0;

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

    public void UpdateCameraPosition(Vector3 position)
    {
        cameraPosition = position;
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

                    // Calculate priority based on distance from camera
                    Vector3 chunkWorldPos = new Vector3(
                        chunkPos.X * Chunk.ChunkSize,
                        chunkPos.Y * Chunk.ChunkSize,
                        chunkPos.Z * Chunk.ChunkSize
                    );
                    float distance = (chunkWorldPos - cameraPosition).Length;

                    lock (queueLock)
                    {
                        priorityChunksToGenerate.Enqueue(chunkPos, distance);
                        ChunksInGenerationQueue++;
                    }
                }
            }
        }
    }

    private bool IsChunkGenerated(Chunk chunk)
    {
        // Quick check - if there are any active voxels, consider it generated
        // This is a simple heuristic
        foreach (var voxel in chunk.GetAllVoxels())
        {
            if (voxel.voxel.IsActive)
                return true;
        }
        return false;
    }

    private void GenerationThreadLoop()
    {
        while (isRunning)
        {
            Vector3Int chunkPos;
            bool hasWork = false;

            lock (queueLock)
            {
                if (priorityChunksToGenerate.Count > 0)
                {
                    chunkPos = priorityChunksToGenerate.Dequeue();
                    ChunksInGenerationQueue--;
                    hasWork = true;
                }
                else
                {
                    chunkPos = Vector3Int.Zero;
                }
            }

            if (hasWork)
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

    public List<Chunk> GetChunksReadyForMeshing(int maxPerFrame = 1, float maxTimeMs = 8.0f)
    {
        var chunks = new List<Chunk>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < maxPerFrame && chunksToMesh.TryDequeue(out Chunk? chunk); i++)
        {
            if (chunk != null)
            {
                chunks.Add(chunk);

                // Check if we've exceeded our time budget
                if (stopwatch.Elapsed.TotalMilliseconds > maxTimeMs)
                {
                    break;
                }
            }
        }

        return chunks;
    }

    public void Clear()
    {
        lock (queueLock)
        {
            priorityChunksToGenerate.Clear();
            ChunksInGenerationQueue = 0;
        }

        while (chunksToMesh.TryDequeue(out _)) { }

        lock (generatingChunks)
        {
            generatingChunks.Clear();
        }
    }
}

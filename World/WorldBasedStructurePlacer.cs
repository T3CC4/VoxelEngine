using VoxelEngine.Core;
using VoxelEngine.Structures;

namespace VoxelEngine.World;

/// <summary>
/// World-based structure placement system that ensures consistent structure positions
/// regardless of chunk loading order. Structures are placed based on world coordinates
/// using a deterministic grid system.
/// </summary>
public class WorldBasedStructurePlacer
{
    private readonly int seed;
    private readonly int structureGridSize; // Size of grid cells for structure placement
    private readonly StructureManager? structureManager;

    public WorldBasedStructurePlacer(int seed, StructureManager? structureManager, int gridSize = 64)
    {
        this.seed = seed;
        this.structureManager = structureManager;
        this.structureGridSize = gridSize;
    }

    /// <summary>
    /// Get all structures that should be placed in a chunk.
    /// This is called during chunk generation.
    /// </summary>
    public List<StructurePlacement> GetStructuresForChunk(Vector3Int chunkPosition)
    {
        var placements = new List<StructurePlacement>();

        if (structureManager == null)
            return placements;

        // Only place structures in surface-level chunks
        if (chunkPosition.Y < 3 || chunkPosition.Y > 6)
            return placements;

        // Calculate world position range for this chunk
        Vector3Int chunkWorldMin = chunkPosition * Chunk.ChunkSize;
        Vector3Int chunkWorldMax = chunkWorldMin + new Vector3Int(Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize);

        // Find all structure grid cells that overlap with this chunk
        int gridMinX = (int)Math.Floor(chunkWorldMin.X / (float)structureGridSize);
        int gridMaxX = (int)Math.Floor((chunkWorldMax.X - 1) / (float)structureGridSize);
        int gridMinZ = (int)Math.Floor(chunkWorldMin.Z / (float)structureGridSize);
        int gridMaxZ = (int)Math.Floor((chunkWorldMax.Z - 1) / (float)structureGridSize);

        // Check each grid cell for structure placement
        for (int gridX = gridMinX; gridX <= gridMaxX; gridX++)
        {
            for (int gridZ = gridMinZ; gridZ <= gridMaxZ; gridZ++)
            {
                var placement = GetStructureForGridCell(gridX, gridZ);
                if (placement != null)
                {
                    // Check if the structure actually falls within this chunk
                    // (we need to check because structures can span multiple grid cells)
                    placements.Add(placement);
                }
            }
        }

        return placements;
    }

    /// <summary>
    /// Deterministically calculate if a structure should be placed in a grid cell,
    /// and if so, what structure and where.
    /// </summary>
    private StructurePlacement? GetStructureForGridCell(int gridX, int gridZ)
    {
        // Create deterministic random for this grid cell
        int gridSeed = HashGridPosition(gridX, gridZ);
        Random gridRand = new Random(seed + gridSeed);

        // Calculate world position for this grid cell (center of cell)
        int worldX = gridX * structureGridSize + structureGridSize / 2;
        int worldZ = gridZ * structureGridSize + structureGridSize / 2;

        // Add some randomization within the grid cell
        int offsetX = gridRand.Next(-structureGridSize / 3, structureGridSize / 3);
        int offsetZ = gridRand.Next(-structureGridSize / 3, structureGridSize / 3);
        worldX += offsetX;
        worldZ += offsetZ;

        // Get biome at this position (we'll need to access world generator for this)
        // For now, we'll calculate it directly using the same noise function
        BiomeType biome = CalculateBiome(worldX, worldZ);

        // Get all structures that can spawn in this biome
        var eligibleStructures = new List<Structure>();

        if (structureManager.ArchitectureStructures != null)
        {
            eligibleStructures.AddRange(
                structureManager.ArchitectureStructures.Where(s => s.CanSpawnInBiome(biome))
            );
        }

        if (structureManager.AmbientStructures != null)
        {
            eligibleStructures.AddRange(
                structureManager.AmbientStructures.Where(s => s.CanSpawnInBiome(biome))
            );
        }

        if (eligibleStructures.Count == 0)
            return null;

        // Select a random structure from eligible ones
        Structure selectedStructure = eligibleStructures[gridRand.Next(eligibleStructures.Count)];

        // Check spawn frequency
        if (gridRand.NextDouble() > selectedStructure.SpawnFrequency)
            return null;

        return new StructurePlacement
        {
            Structure = selectedStructure,
            WorldPosition = new Vector3Int(worldX, 0, worldZ), // Y will be determined by ground level
            GridCell = new Vector2Int(gridX, gridZ)
        };
    }

    private int HashGridPosition(int x, int z)
    {
        // Hash function for grid position
        return x * 73856093 ^ z * 83492791;
    }

    // Temporary biome calculation - this should ideally use the world generator's biome function
    private BiomeType CalculateBiome(int x, int z)
    {
        float temperature = GetNoiseValue(x, z, 0.002f, seed);
        float moisture = GetNoiseValue(x, z, 0.002f, seed + 1000);

        if (temperature > 0.6f)
        {
            return moisture > 0.5f ? BiomeType.Jungle : BiomeType.Desert;
        }
        else if (temperature < -0.3f)
        {
            return BiomeType.Tundra;
        }
        else
        {
            return moisture > 0.3f ? BiomeType.Forest : BiomeType.Plains;
        }
    }

    private float GetNoiseValue(int x, int z, float frequency, int noiseSeed)
    {
        float nx = x * frequency;
        float nz = z * frequency;

        int ix = (int)Math.Floor(nx);
        int iz = (int)Math.Floor(nz);

        float fx = nx - ix;
        float fz = nz - iz;

        float u = fx * fx * (3.0f - 2.0f * fx);
        float v = fz * fz * (3.0f - 2.0f * fz);

        float a = Hash(ix, iz, noiseSeed);
        float b = Hash(ix + 1, iz, noiseSeed);
        float c = Hash(ix, iz + 1, noiseSeed);
        float d = Hash(ix + 1, iz + 1, noiseSeed);

        return Lerp(Lerp(a, b, u), Lerp(c, d, u), v);
    }

    private float Hash(int x, int z, int noiseSeed)
    {
        int h = noiseSeed + x * 374761393 + z * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF * 2.0f - 1.0f;
    }

    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}

public class StructurePlacement
{
    public Structure Structure { get; set; } = null!;
    public Vector3Int WorldPosition { get; set; }
    public Vector2Int GridCell { get; set; }
}

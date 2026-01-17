using OpenTK.Mathematics;
using VoxelEngine.Core;
using VoxelEngine.Structures;
using VoxelEngine.Decorations;

namespace VoxelEngine.World;

public class InfiniteWorldGenerator
{
    private WorldGenConfig config;
    private Random rand;
    private StructureManager? structureManager;
    private WorldBasedStructurePlacer? structurePlacer;
    private DecorationManager? decorationManager;

    public InfiniteWorldGenerator(WorldGenConfig config, StructureManager? structureManager = null, DecorationManager? decorationManager = null)
    {
        this.config = config;
        this.rand = new Random(config.Seed);
        this.structureManager = structureManager;
        this.decorationManager = decorationManager;

        if (structureManager != null)
        {
            this.structurePlacer = new WorldBasedStructurePlacer(config.Seed, structureManager);
        }
    }

    public void GenerateChunk(Chunk chunk, InfiniteVoxelWorld world)
    {
        Vector3Int chunkWorldPos = chunk.Position * Chunk.ChunkSize;

        // First pass: Generate terrain
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int z = 0; z < Chunk.ChunkSize; z++)
            {
                int worldX = chunkWorldPos.X + x;
                int worldZ = chunkWorldPos.Z + z;

                // Get biome type based on position
                BiomeType biome = GetBiome(worldX, worldZ);

                // Multi-octave noise for terrain with biome influence
                float height = GetTerrainHeight(worldX, worldZ, biome);
                int surfaceLevel = config.SurfaceLevel; // ~156
                int terrainHeight = (int)(surfaceLevel + height * config.HeightVariation);
                terrainHeight = Math.Clamp(terrainHeight, 1, config.MaxHeight - 1);

                // Only generate blocks that are within this chunk's Y range
                int minY = chunkWorldPos.Y;
                int maxY = chunkWorldPos.Y + Chunk.ChunkSize;

                for (int y = Math.Max(0, minY); y < maxY; y++)
                {
                    int localY = y - chunkWorldPos.Y;
                    if (localY < 0 || localY >= Chunk.ChunkSize) continue;

                    // Check if block should be carved out by cave
                    bool isCave = IsCave(worldX, y, worldZ);

                    if (!isCave)
                    {
                        // Place terrain block if below terrain height
                        if (y < terrainHeight)
                        {
                            VoxelType type = GetVoxelTypeForPosition(y, terrainHeight, biome, worldX, worldZ);
                            chunk.SetVoxelType(x, localY, z, type);
                        }
                    }
                }
            }
        }

        // Second pass: Fill water
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int z = 0; z < Chunk.ChunkSize; z++)
            {
                int worldX = chunkWorldPos.X + x;
                int worldZ = chunkWorldPos.Z + z;

                int minY = chunkWorldPos.Y;
                int maxY = chunkWorldPos.Y + Chunk.ChunkSize;

                for (int y = minY; y <= config.WaterLevel && y < maxY; y++)
                {
                    int localY = y - chunkWorldPos.Y;
                    if (localY < 0 || localY >= Chunk.ChunkSize) continue;

                    var voxel = chunk.GetVoxel(x, localY, z);
                    if (!voxel.IsActive || voxel.Type == VoxelType.Air)
                    {
                        chunk.SetVoxelType(x, localY, z, VoxelType.Water);
                    }
                }
            }
        }

        // Third pass: Generate structures (only in surface chunks)
        if (structurePlacer != null && chunk.Position.Y >= 3 && chunk.Position.Y <= 6)
        {
            GenerateStructuresInChunk(chunk, world, chunkWorldPos);
        }

        // Fourth pass: Generate decorations (only in surface chunks)
        if (decorationManager != null && chunk.Position.Y >= 3 && chunk.Position.Y <= 6)
        {
            GenerateDecorationsInChunk(chunk, world, chunkWorldPos);
        }
    }

    private void GenerateStructuresInChunk(Chunk chunk, InfiniteVoxelWorld world, Vector3Int chunkWorldPos)
    {
        // Use world-based structure placer for consistent structure positions
        var placements = structurePlacer!.GetStructuresForChunk(chunk.Position);

        foreach (var placement in placements)
        {
            // Find ground level for placement (search from chunk top)
            int searchStartY = (chunk.Position.Y + 1) * Chunk.ChunkSize;
            Vector3Int placementPos = new Vector3Int(
                placement.WorldPosition.X,
                searchStartY,
                placement.WorldPosition.Z
            );

            // Use PlaceOnGround to automatically find terrain and place
            placement.Structure.PlaceOnGround(world, placementPos, maxSearchDown: Chunk.ChunkSize * 2);
        }
    }

    private void GenerateDecorationsInChunk(Chunk chunk, InfiniteVoxelWorld world, Vector3Int chunkWorldPos)
    {
        // Generate decorations on surface blocks
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int z = 0; z < Chunk.ChunkSize; z++)
            {
                int worldX = chunkWorldPos.X + x;
                int worldZ = chunkWorldPos.Z + z;

                // Get biome at this position
                BiomeType biome = GetBiome(worldX, worldZ);

                // Get eligible decorations for this biome
                var eligibleDecorations = decorationManager!.GetDecorationsForBiome(biome);
                if (eligibleDecorations.Count == 0)
                    continue;

                // Find the top solid block in this column within the chunk
                for (int y = Chunk.ChunkSize - 1; y >= 0; y--)
                {
                    var voxel = chunk.GetVoxel(x, y, z);

                    // Check if this is a solid block
                    if (voxel.IsActive && voxel.Type.IsSolid() && voxel.Type != VoxelType.Water)
                    {
                        // This is the ground block
                        VoxelType groundBlock = voxel.Type;

                        // Check if the block above is air (space for decoration)
                        int worldY = chunkWorldPos.Y + y;
                        Vector3Int abovePos = new Vector3Int(worldX, worldY + 1, worldZ);
                        var aboveVoxel = world.GetVoxel(abovePos);

                        if (!aboveVoxel.IsActive || aboveVoxel.Type == VoxelType.Air)
                        {
                            // Try to place a decoration
                            TryPlaceDecoration(world, abovePos, groundBlock, biome, eligibleDecorations, worldX, worldZ);
                        }

                        // Only check the topmost solid block
                        break;
                    }
                }
            }
        }
    }

    private void TryPlaceDecoration(IVoxelWorld world, Vector3Int position, VoxelType groundBlock,
                                     BiomeType biome, List<Decoration> eligibleDecorations,
                                     int worldX, int worldZ)
    {
        // Use deterministic random based on world position
        int seed = HashPosition(worldX, worldZ);
        Random random = new Random(config.Seed + seed);

        // Try each eligible decoration
        foreach (var decoration in eligibleDecorations)
        {
            // Check if decoration can spawn on this ground block
            if (!decoration.CanSpawnOnGround(groundBlock))
                continue;

            // Check spawn density
            if (random.NextDouble() > decoration.Density)
                continue;

            // For now, we'll place a placeholder block (Leaves for decorations)
            // In a full implementation, this would store decoration data for rendering mini-voxels
            // TODO: Implement proper mini-voxel rendering system
            world.SetVoxelType(position, VoxelType.Leaves);

            // Only place one decoration per location
            return;
        }
    }

    private int HashPosition(int x, int z)
    {
        return x * 374761393 + z * 668265263;
    }

    private int HashChunkPosition(Vector3Int pos)
    {
        // Simple hash function for chunk position
        return pos.X * 73856093 ^ pos.Y * 19349663 ^ pos.Z * 83492791;
    }

    private bool IsCave(int x, int y, int z)
    {
        // Only generate caves below surface level
        if (y > config.SurfaceLevel)
            return false;

        // 3D Perlin noise for caves
        float caveNoise1 = GetNoiseValue3D(x, y, z, 0.04f, config.Seed + 5000);
        float caveNoise2 = GetNoiseValue3D(x, y, z, 0.06f, config.Seed + 6000);

        // Combine noise layers for more interesting cave shapes
        float caveValue = caveNoise1 * 0.6f + caveNoise2 * 0.4f;

        // Threshold for cave carving - adjust this to control cave density
        float caveThreshold = 0.45f;

        // Make caves rarer near surface and near bedrock
        float depthFactor = 1.0f;
        if (y > config.SurfaceLevel - 20)
        {
            float distanceFromSurface = config.SurfaceLevel - y;
            depthFactor = distanceFromSurface / 20.0f;
        }
        else if (y < 10)
        {
            depthFactor = y / 10.0f;
        }

        caveThreshold -= depthFactor * 0.1f;

        return caveValue > caveThreshold;
    }

    private float GetNoiseValue3D(int x, int y, int z, float frequency, int seed)
    {
        float nx = x * frequency;
        float ny = y * frequency;
        float nz = z * frequency;

        // Simple hash-based pseudo-noise in 3D
        int ix = (int)Math.Floor(nx);
        int iy = (int)Math.Floor(ny);
        int iz = (int)Math.Floor(nz);

        float fx = nx - ix;
        float fy = ny - iy;
        float fz = nz - iz;

        // Smooth interpolation
        float u = fx * fx * (3.0f - 2.0f * fx);
        float v = fy * fy * (3.0f - 2.0f * fy);
        float w = fz * fz * (3.0f - 2.0f * fz);

        // Get corner values
        float c000 = Hash3D(ix, iy, iz, seed);
        float c100 = Hash3D(ix + 1, iy, iz, seed);
        float c010 = Hash3D(ix, iy + 1, iz, seed);
        float c110 = Hash3D(ix + 1, iy + 1, iz, seed);
        float c001 = Hash3D(ix, iy, iz + 1, seed);
        float c101 = Hash3D(ix + 1, iy, iz + 1, seed);
        float c011 = Hash3D(ix, iy + 1, iz + 1, seed);
        float c111 = Hash3D(ix + 1, iy + 1, iz + 1, seed);

        // Trilinear interpolation
        float x1 = Lerp(c000, c100, u);
        float x2 = Lerp(c010, c110, u);
        float x3 = Lerp(c001, c101, u);
        float x4 = Lerp(c011, c111, u);

        float y1 = Lerp(x1, x2, v);
        float y2 = Lerp(x3, x4, v);

        return Lerp(y1, y2, w);
    }

    private float Hash3D(int x, int y, int z, int seed)
    {
        int h = seed + x * 374761393 + y * 668265263 + z * 1274126177;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF * 2.0f - 1.0f;
    }

    public BiomeType GetBiome(int x, int z)
    {
        // Use very low frequency noise for biomes (large regions)
        float temperature = GetNoiseValue(x, z, 0.002f, config.Seed);
        float moisture = GetNoiseValue(x, z, 0.002f, config.Seed + 1000);

        return GetBiomeFromClimate(temperature, moisture);
    }

    private BiomeType GetBiomeFromClimate(float temperature, float moisture)
    {
        // Biome selection based on temperature and moisture
        // Using smoother transitions with multiple climate zones
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

    private Dictionary<BiomeType, float> GetBiomeInfluences(int x, int z)
    {
        // Sample biomes in a small radius and calculate influence weights
        // This creates smooth transitions between biomes
        var influences = new Dictionary<BiomeType, float>();

        // Sample radius for biome blending
        int sampleRadius = 8;
        int sampleCount = 0;

        for (int dx = -sampleRadius; dx <= sampleRadius; dx += 4)
        {
            for (int dz = -sampleRadius; dz <= sampleRadius; dz += 4)
            {
                // Calculate distance-based weight (closer samples have more influence)
                float distance = MathF.Sqrt(dx * dx + dz * dz);
                float weight = Math.Max(0, 1.0f - distance / sampleRadius);

                if (weight > 0)
                {
                    // Get biome at sample position
                    float temperature = GetNoiseValue(x + dx, z + dz, 0.002f, config.Seed);
                    float moisture = GetNoiseValue(x + dx, z + dz, 0.002f, config.Seed + 1000);
                    BiomeType biome = GetBiomeFromClimate(temperature, moisture);

                    // Add weighted influence
                    if (!influences.ContainsKey(biome))
                        influences[biome] = 0;

                    influences[biome] += weight;
                    sampleCount++;
                }
            }
        }

        // Normalize influences
        float totalInfluence = influences.Values.Sum();
        if (totalInfluence > 0)
        {
            var normalizedInfluences = new Dictionary<BiomeType, float>();
            foreach (var kvp in influences)
            {
                normalizedInfluences[kvp.Key] = kvp.Value / totalInfluence;
            }
            return normalizedInfluences;
        }

        // Fallback to single biome
        var fallbackBiome = GetBiome(x, z);
        return new Dictionary<BiomeType, float> { { fallbackBiome, 1.0f } };
    }

    private VoxelType GetVoxelTypeForPosition(int y, int terrainHeight, BiomeType biome, int worldX, int worldZ)
    {
        if (y == terrainHeight - 1)
        {
            // Top layer - biome specific
            if (terrainHeight <= config.WaterLevel)
                return VoxelType.Sand;

            return biome switch
            {
                BiomeType.Desert => VoxelType.Sand,
                BiomeType.Tundra => VoxelType.Stone,
                BiomeType.Jungle => VoxelType.Grass,
                BiomeType.Forest => VoxelType.Grass,
                BiomeType.Plains => VoxelType.Grass,
                _ => VoxelType.Grass
            };
        }
        else if (y >= terrainHeight - 4)
        {
            // Sub-surface
            if (terrainHeight <= config.WaterLevel || biome == BiomeType.Desert)
                return VoxelType.Sand;

            return biome == BiomeType.Tundra ? VoxelType.Stone : VoxelType.Dirt;
        }
        else
        {
            // Deep underground
            return VoxelType.Stone;
        }
    }

    private float GetTerrainHeight(int x, int z, BiomeType biome)
    {
        float baseNoise = GetMultiOctaveNoise(x, z);

        // Use biome blending for smoother transitions
        var biomeInfluences = GetBiomeInfluences(x, z);

        // Calculate blended height multiplier
        float blendedMultiplier = 0f;
        foreach (var kvp in biomeInfluences)
        {
            float biomeMultiplier = GetBiomeHeightMultiplier(kvp.Key);
            blendedMultiplier += biomeMultiplier * kvp.Value;
        }

        return baseNoise * blendedMultiplier;
    }

    private float GetBiomeHeightMultiplier(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Desert => 0.3f,      // Flat deserts
            BiomeType.Plains => 0.5f,      // Rolling plains
            BiomeType.Forest => 0.7f,      // Moderate hills
            BiomeType.Jungle => 0.9f,      // Hills
            BiomeType.Tundra => 1.2f,      // Mountains
            _ => 1.0f
        };
    }

    private float GetMultiOctaveNoise(int x, int z)
    {
        float total = 0f;
        float frequency = config.NoiseFrequency;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < config.NoiseOctaves; i++)
        {
            total += GetNoiseValue(x, z, frequency, config.Seed + i * 100) * amplitude;
            maxValue += amplitude;
            amplitude *= config.NoisePersistence;
            frequency *= 2f;
        }

        return total / maxValue;
    }

    private float GetNoiseValue(int x, int z, float frequency, int seed)
    {
        float nx = x * frequency;
        float nz = z * frequency;

        // Simple hash-based pseudo-noise
        int ix = (int)Math.Floor(nx);
        int iz = (int)Math.Floor(nz);

        float fx = nx - ix;
        float fz = nz - iz;

        // Smooth interpolation
        float u = fx * fx * (3.0f - 2.0f * fx);
        float v = fz * fz * (3.0f - 2.0f * fz);

        // Get corner values
        float a = Hash(ix, iz, seed);
        float b = Hash(ix + 1, iz, seed);
        float c = Hash(ix, iz + 1, seed);
        float d = Hash(ix + 1, iz + 1, seed);

        // Bilinear interpolation
        return Lerp(Lerp(a, b, u), Lerp(c, d, u), v);
    }

    private float Hash(int x, int z, int seed)
    {
        int h = seed + x * 374761393 + z * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF * 2.0f - 1.0f;
    }

    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}

public enum BiomeType
{
    Plains,
    Desert,
    Forest,
    Jungle,
    Tundra
}

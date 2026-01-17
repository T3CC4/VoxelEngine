using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.World;

public class InfiniteWorldGenerator
{
    private WorldGenConfig config;
    private Random rand;

    public InfiniteWorldGenerator(WorldGenConfig config)
    {
        this.config = config;
        this.rand = new Random(config.Seed);
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
                int terrainHeight = (int)(config.BaseHeight + height * config.HeightVariation);
                terrainHeight = Math.Clamp(terrainHeight, 1, Chunk.ChunkSize * 16 - 1);

                // Only generate blocks that are within this chunk's Y range
                int minY = chunkWorldPos.Y;
                int maxY = chunkWorldPos.Y + Chunk.ChunkSize;

                for (int y = Math.Max(0, minY); y < Math.Min(terrainHeight, maxY); y++)
                {
                    int localY = y - chunkWorldPos.Y;
                    if (localY < 0 || localY >= Chunk.ChunkSize) continue;

                    VoxelType type = GetVoxelTypeForPosition(y, terrainHeight, biome, worldX, worldZ);
                    chunk.SetVoxelType(x, localY, z, type);
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
    }

    private BiomeType GetBiome(int x, int z)
    {
        // Use very low frequency noise for biomes (large regions)
        float temperature = GetNoiseValue(x, z, 0.002f, config.Seed);
        float moisture = GetNoiseValue(x, z, 0.002f, config.Seed + 1000);

        // Biome selection based on temperature and moisture
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

        // Biome-specific height modifiers
        float heightMultiplier = biome switch
        {
            BiomeType.Desert => 0.4f,      // Flat deserts
            BiomeType.Plains => 0.6f,      // Rolling plains
            BiomeType.Forest => 0.8f,      // Moderate hills
            BiomeType.Jungle => 1.0f,      // Hills
            BiomeType.Tundra => 1.2f,      // Mountains
            _ => 1.0f
        };

        return baseNoise * heightMultiplier;
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

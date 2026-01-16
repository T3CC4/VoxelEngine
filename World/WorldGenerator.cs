using System.Text.Json;
using VoxelEngine.Core;

namespace VoxelEngine.World;

public class WorldGenerator
{
    private WorldGenConfig config;
    private string configPath = "worldgen_config.json";

    public WorldGenerator()
    {
        LoadConfig();
    }

    public void LoadConfig()
    {
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<WorldGenConfig>(json) ?? new WorldGenConfig();
                Console.WriteLine("Loaded worldgen config");
            }
            catch
            {
                config = new WorldGenConfig();
                SaveConfig();
            }
        }
        else
        {
            config = new WorldGenConfig();
            SaveConfig();
        }
    }

    public void SaveConfig()
    {
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    public void GenerateTerrain(VoxelWorld world)
    {
        int maxX = world.WorldSize.X * Chunk.ChunkSize;
        int maxZ = world.WorldSize.Z * Chunk.ChunkSize;
        int maxY = world.WorldSize.Y * Chunk.ChunkSize;

        Random rand = new Random(config.Seed);

        // First pass: Generate terrain
        for (int x = 0; x < maxX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                // Multi-octave noise for terrain
                float height = GetTerrainHeight(x, z);
                int terrainHeight = (int)(config.BaseHeight + height * config.HeightVariation);
                terrainHeight = Math.Clamp(terrainHeight, 1, maxY - 1);

                for (int y = 0; y < terrainHeight && y < maxY; y++)
                {
                    VoxelType type;

                    if (y == terrainHeight - 1)
                    {
                        // Top layer
                        if (terrainHeight > config.WaterLevel + 1)
                            type = VoxelType.Grass;
                        else if (terrainHeight > config.WaterLevel - 2)
                            type = VoxelType.Sand;
                        else
                            type = VoxelType.Sand;
                    }
                    else if (y >= terrainHeight - 4)
                    {
                        // Sub-surface
                        type = terrainHeight > config.WaterLevel ? VoxelType.Dirt : VoxelType.Sand;
                    }
                    else
                    {
                        // Deep underground
                        type = VoxelType.Stone;
                    }

                    world.SetVoxelType(new Vector3Int(x, y, z), type);
                }
            }
        }

        // Second pass: Fill water in all air blocks up to water level
        // This ensures water fills properly around islands and elevated terrain
        for (int x = 0; x < maxX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                for (int y = 0; y <= config.WaterLevel && y < maxY; y++)
                {
                    var voxel = world.GetVoxel(new Vector3Int(x, y, z));
                    if (!voxel.IsActive || voxel.Type == VoxelType.Air)
                    {
                        world.SetVoxelType(new Vector3Int(x, y, z), VoxelType.Water);
                    }
                }
            }
        }
    }

    private float GetTerrainHeight(int x, int z)
    {
        // Multiple octaves of noise for more realistic terrain
        float noise = 0;
        float amplitude = 1.0f;
        float frequency = config.NoiseFrequency;

        for (int octave = 0; octave < config.NoiseOctaves; octave++)
        {
            noise += SimplexNoise(x * frequency, z * frequency) * amplitude;
            amplitude *= config.NoisePersistence;
            frequency *= 2.0f;
        }

        return noise;
    }

    private float SimplexNoise(float x, float z)
    {
        // Simple perlin-like noise
        int xi = (int)MathF.Floor(x);
        int zi = (int)MathF.Floor(z);

        float xf = x - xi;
        float zf = z - zi;

        float u = Fade(xf);
        float v = Fade(zf);

        int seed = config.Seed;

        float n00 = DotGridGradient(xi, zi, x, z, seed);
        float n10 = DotGridGradient(xi + 1, zi, x, z, seed);
        float n01 = DotGridGradient(xi, zi + 1, x, z, seed);
        float n11 = DotGridGradient(xi + 1, zi + 1, x, z, seed);

        float nx0 = MathHelper.Lerp(n00, n10, u);
        float nx1 = MathHelper.Lerp(n01, n11, u);

        return MathHelper.Lerp(nx0, nx1, v);
    }

    private float DotGridGradient(int ix, int iz, float x, float z, int seed)
    {
        // Get gradient
        float angle = GetPseudoRandom(ix, iz, seed) * MathF.PI * 2.0f;
        float gx = MathF.Cos(angle);
        float gz = MathF.Sin(angle);

        // Compute distance vector
        float dx = x - ix;
        float dz = z - iz;

        // Dot product
        return dx * gx + dz * gz;
    }

    private float GetPseudoRandom(int x, int z, int seed)
    {
        int n = x + z * 57 + seed * 131;
        n = (n << 13) ^ n;
        return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647.0f;
    }

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    public WorldGenConfig GetConfig() => config;
}

public class WorldGenConfig
{
    public int Seed { get; set; } = 12345;
    public float BaseHeight { get; set; } = 8.0f;
    public float HeightVariation { get; set; } = 12.0f;
    public float NoiseFrequency { get; set; } = 0.02f;
    public int NoiseOctaves { get; set; } = 4;
    public float NoisePersistence { get; set; } = 0.5f;
    public int WaterLevel { get; set; } = 12;
}

public static class MathHelper
{
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}

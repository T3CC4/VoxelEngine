using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.Graphics;

public class OcclusionCulling
{
    private readonly Dictionary<Vector3Int, bool> visibilityCache = new();
    private Vector3 lastCameraPos = Vector3.Zero;
    private const float CameraMovementThreshold = 32.0f;
    private const float MinOcclusionDistance = 96.0f; // Start checking occlusion at 3 chunks away
    private bool occlusionEnabled = true;

    public bool OcclusionEnabled
    {
        get => occlusionEnabled;
        set
        {
            if (!value)
            {
                visibilityCache.Clear();
            }
            occlusionEnabled = value;
        }
    }

    public void UpdateCameraPosition(Vector3 cameraPos)
    {
        // Clear cache if camera moved significantly
        if ((cameraPos - lastCameraPos).LengthSquared > CameraMovementThreshold * CameraMovementThreshold)
        {
            visibilityCache.Clear();
            lastCameraPos = cameraPos;
        }
    }

    public bool IsChunkVisibleWithOcclusion(Vector3Int chunkPos, Vector3 chunkWorldPos, Vector3 cameraPos,
                                             Vector3 cameraFront, float chunkSize, Dictionary<Vector3Int, bool> loadedChunks)
    {
        // If occlusion is disabled, always return visible
        if (!occlusionEnabled)
        {
            return true;
        }

        // Check cache first
        if (visibilityCache.TryGetValue(chunkPos, out bool cached))
        {
            return cached;
        }

        // Calculate distance
        Vector3 chunkCenter = chunkWorldPos + new Vector3(chunkSize / 2, chunkSize / 2, chunkSize / 2);
        Vector3 toChunk = chunkCenter - cameraPos;
        float distanceSquared = toChunk.LengthSquared;

        // Chunks close to camera are always visible
        if (distanceSquared < MinOcclusionDistance * MinOcclusionDistance)
        {
            visibilityCache[chunkPos] = true;
            return true;
        }

        // Check if chunk is below terrain level based on camera direction
        bool isVisible = CheckTerrainOcclusion(chunkPos, chunkWorldPos, cameraPos, cameraFront, chunkSize, loadedChunks);

        visibilityCache[chunkPos] = isVisible;
        return isVisible;
    }

    private bool CheckTerrainOcclusion(Vector3Int chunkPos, Vector3 chunkWorldPos, Vector3 cameraPos,
                                        Vector3 cameraFront, float chunkSize, Dictionary<Vector3Int, bool> loadedChunks)
    {
        Vector3 cameraChunkPos = new Vector3(
            cameraPos.X / chunkSize,
            cameraPos.Y / chunkSize,
            cameraPos.Z / chunkSize
        );

        // Underground chunk occlusion - if camera is above ground and chunk is underground
        if (chunkPos.Y < cameraChunkPos.Y - 2)
        {
            // Check if there are chunks between camera and this chunk at higher Y levels
            Vector3Int cameraChunk = new Vector3Int(
                (int)MathF.Floor(cameraChunkPos.X),
                (int)MathF.Floor(cameraChunkPos.Y),
                (int)MathF.Floor(cameraChunkPos.Z)
            );

            // Simple check: if there's a loaded chunk directly above this one, it's likely occluded
            for (int y = chunkPos.Y + 1; y <= cameraChunk.Y; y++)
            {
                Vector3Int checkPos = new Vector3Int(chunkPos.X, y, chunkPos.Z);
                if (loadedChunks.ContainsKey(checkPos))
                {
                    // Chunk above exists, this one is occluded
                    return false;
                }
            }
        }

        // Terrain behind camera occlusion
        Vector3 toChunk = (chunkWorldPos - cameraPos);
        toChunk.Y = 0; // Check only horizontal direction
        if (toChunk.LengthSquared > 0.001f)
        {
            toChunk = Vector3.Normalize(toChunk);
            Vector3 cameraForward = new Vector3(cameraFront.X, 0, cameraFront.Z);
            if (cameraForward.LengthSquared > 0.001f)
            {
                cameraForward = Vector3.Normalize(cameraForward);

                // Check if chunk is significantly behind camera's view direction
                float dot = Vector3.Dot(toChunk, cameraForward);

                // If chunk is far behind and also below, occlude it
                if (dot < -0.3f && chunkPos.Y < cameraChunkPos.Y - 1)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void Clear()
    {
        visibilityCache.Clear();
    }
}


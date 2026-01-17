using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.Graphics;

public class OcclusionCulling
{
    private readonly Dictionary<Vector3Int, bool> visibilityCache = new();
    private Vector3 lastCameraPos = Vector3.Zero;
    private const float CameraMovementThreshold = 32.0f; // Increased for less frequent cache clearing
    private const float OcclusionCheckDistance = 192.0f; // Only check occlusion for very distant chunks
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

    public bool IsChunkVisibleWithOcclusion(Vector3Int chunkPos, Vector3 chunkWorldPos, Vector3 cameraPos, float chunkSize)
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

        // Quick distance check using squared distance to avoid sqrt
        float distanceSquared = (chunkWorldPos.X - cameraPos.X) * (chunkWorldPos.X - cameraPos.X) +
                                (chunkWorldPos.Z - cameraPos.Z) * (chunkWorldPos.Z - cameraPos.Z);

        // Chunks close to camera are always visible (no occlusion check needed)
        if (distanceSquared < OcclusionCheckDistance * OcclusionCheckDistance)
        {
            visibilityCache[chunkPos] = true;
            return true;
        }

        // Simple height-based occlusion for underground chunks only
        bool isVisible = true;

        // Only occlude chunks that are significantly underground
        if (chunkPos.Y < 2)
        {
            float cameraChunkY = cameraPos.Y / chunkSize;

            // If camera is above ground and this chunk is underground, occlude
            if (cameraChunkY > chunkPos.Y + 3)
            {
                isVisible = false;
            }
        }

        visibilityCache[chunkPos] = isVisible;
        return isVisible;
    }

    public void Clear()
    {
        visibilityCache.Clear();
    }
}


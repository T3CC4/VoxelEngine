using OpenTK.Mathematics;
using VoxelEngine.Core;

namespace VoxelEngine.Graphics;

public class OcclusionCulling
{
    private readonly Dictionary<Vector3Int, bool> visibilityCache = new();
    private Vector3 lastCameraPos = Vector3.Zero;
    private const float CameraMovementThreshold = 16.0f;
    private const float OcclusionCheckDistance = 128.0f; // Only check occlusion for chunks beyond this distance

    public void UpdateCameraPosition(Vector3 cameraPos)
    {
        // Clear cache if camera moved significantly
        if ((cameraPos - lastCameraPos).Length > CameraMovementThreshold)
        {
            visibilityCache.Clear();
            lastCameraPos = cameraPos;
        }
    }

    public bool IsChunkVisibleWithOcclusion(Vector3Int chunkPos, Vector3 chunkWorldPos, Vector3 cameraPos,
                                             Dictionary<Vector3Int, bool> nearbyChunks, float chunkSize)
    {
        // Check cache first
        if (visibilityCache.TryGetValue(chunkPos, out bool cached))
        {
            return cached;
        }

        // Calculate chunk center
        Vector3 chunkCenter = chunkWorldPos + new Vector3(chunkSize / 2, chunkSize / 2, chunkSize / 2);

        // Distance from camera to chunk
        float distanceToCamera = (chunkCenter - cameraPos).Length;

        // Chunks close to camera are always visible (no occlusion check needed)
        if (distanceToCamera < OcclusionCheckDistance)
        {
            visibilityCache[chunkPos] = true;
            return true;
        }

        // For distant chunks, do simple height-based occlusion
        // If camera is above and chunk is far below ground level, it's likely occluded
        bool isVisible = IsVisibleByHeight(chunkPos, chunkWorldPos, cameraPos, chunkSize);

        // Additional check: if chunk is underground and camera can't see it, occlude
        if (isVisible && chunkPos.Y < 3) // Chunks below Y=3 (underground)
        {
            // If camera is above ground and this chunk is far underground, occlude
            Vector3Int cameraChunkPos = new Vector3Int(
                (int)(cameraPos.X / chunkSize),
                (int)(cameraPos.Y / chunkSize),
                (int)(cameraPos.Z / chunkSize)
            );

            if (cameraChunkPos.Y > chunkPos.Y + 2)
            {
                isVisible = false;
            }
        }

        visibilityCache[chunkPos] = isVisible;
        return isVisible;
    }

    private bool IsVisibleByHeight(Vector3Int chunkPos, Vector3 chunkWorldPos, Vector3 cameraPos, float chunkSize)
    {
        // Simple heuristic: chunks far below camera and far away are likely occluded by terrain
        float horizontalDistance = new Vector2(
            chunkWorldPos.X - cameraPos.X,
            chunkWorldPos.Z - cameraPos.Z
        ).Length;

        float verticalDistance = cameraPos.Y - (chunkWorldPos.Y + chunkSize);

        // If chunk is significantly below camera and far away, it's likely occluded
        if (verticalDistance > chunkSize * 3 && horizontalDistance > chunkSize * 8)
        {
            return false;
        }

        // If chunk is far below and at moderate distance, also likely occluded
        if (verticalDistance > chunkSize * 5 && horizontalDistance > chunkSize * 4)
        {
            return false;
        }

        return true;
    }

    public void Clear()
    {
        visibilityCache.Clear();
    }
}


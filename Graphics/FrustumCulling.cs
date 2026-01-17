using OpenTK.Mathematics;

namespace VoxelEngine.Graphics;

public class FrustumCulling
{
    private Plane[] planes = new Plane[6];

    public void UpdateFrustum(Matrix4 viewProjection)
    {
        // Extract frustum planes from view-projection matrix
        // Left plane
        planes[0] = new Plane(
            viewProjection.M14 + viewProjection.M11,
            viewProjection.M24 + viewProjection.M21,
            viewProjection.M34 + viewProjection.M31,
            viewProjection.M44 + viewProjection.M41
        );

        // Right plane
        planes[1] = new Plane(
            viewProjection.M14 - viewProjection.M11,
            viewProjection.M24 - viewProjection.M21,
            viewProjection.M34 - viewProjection.M31,
            viewProjection.M44 - viewProjection.M41
        );

        // Bottom plane
        planes[2] = new Plane(
            viewProjection.M14 + viewProjection.M12,
            viewProjection.M24 + viewProjection.M22,
            viewProjection.M34 + viewProjection.M32,
            viewProjection.M44 + viewProjection.M42
        );

        // Top plane
        planes[3] = new Plane(
            viewProjection.M14 - viewProjection.M12,
            viewProjection.M24 - viewProjection.M22,
            viewProjection.M34 - viewProjection.M32,
            viewProjection.M44 - viewProjection.M42
        );

        // Near plane
        planes[4] = new Plane(
            viewProjection.M14 + viewProjection.M13,
            viewProjection.M24 + viewProjection.M23,
            viewProjection.M34 + viewProjection.M33,
            viewProjection.M44 + viewProjection.M43
        );

        // Far plane
        planes[5] = new Plane(
            viewProjection.M14 - viewProjection.M13,
            viewProjection.M24 - viewProjection.M23,
            viewProjection.M34 - viewProjection.M33,
            viewProjection.M44 - viewProjection.M43
        );

        // Normalize all planes
        for (int i = 0; i < 6; i++)
        {
            planes[i].Normalize();
        }
    }

    public bool IsChunkVisible(Vector3 chunkWorldPos, float chunkSize)
    {
        // Create AABB for the chunk
        Vector3 min = chunkWorldPos;
        Vector3 max = chunkWorldPos + new Vector3(chunkSize, chunkSize, chunkSize);

        // Test chunk AABB against all frustum planes
        for (int i = 0; i < 6; i++)
        {
            // Get the positive vertex (the one furthest along the plane normal)
            Vector3 positiveVertex = new Vector3(
                planes[i].Normal.X >= 0 ? max.X : min.X,
                planes[i].Normal.Y >= 0 ? max.Y : min.Y,
                planes[i].Normal.Z >= 0 ? max.Z : min.Z
            );

            // If the positive vertex is outside this plane, the chunk is outside the frustum
            if (planes[i].DistanceToPoint(positiveVertex) < 0)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsSphereVisible(Vector3 center, float radius)
    {
        for (int i = 0; i < 6; i++)
        {
            if (planes[i].DistanceToPoint(center) < -radius)
            {
                return false;
            }
        }
        return true;
    }
}

public struct Plane
{
    public Vector3 Normal;
    public float Distance;

    public Plane(float a, float b, float c, float d)
    {
        Normal = new Vector3(a, b, c);
        Distance = d;
    }

    public void Normalize()
    {
        float length = Normal.Length;
        Normal /= length;
        Distance /= length;
    }

    public float DistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(Normal, point) + Distance;
    }
}

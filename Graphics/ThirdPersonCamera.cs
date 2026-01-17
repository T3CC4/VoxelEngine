using OpenTK.Mathematics;

namespace VoxelEngine.Graphics;

public class ThirdPersonCamera
{
    public Vector3 TargetPosition { get; set; }
    public Vector3 Position { get; private set; }
    public Vector3 Front { get; private set; }
    public Vector3 Up { get; private set; }
    public Vector3 Right { get; private set; }

    private float yaw = -90.0f;
    private float pitch = -20.0f;
    private float distance = 8.0f;
    private float minDistance = 3.0f;
    private float maxDistance = 20.0f;

    public float Yaw
    {
        get => yaw;
        set
        {
            yaw = value;
            UpdateCameraVectors();
        }
    }

    public float Pitch
    {
        get => pitch;
        set
        {
            pitch = MathHelper.Clamp(value, -89.0f, 89.0f);
            UpdateCameraVectors();
        }
    }

    public float Distance
    {
        get => distance;
        set
        {
            distance = MathHelper.Clamp(value, minDistance, maxDistance);
            UpdateCameraVectors();
        }
    }

    public float Fov { get; set; } = 60.0f;
    public float AspectRatio { get; set; }
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 1000.0f;
    public int ViewDistanceChunks { get; set; } = 12;

    public ThirdPersonCamera(Vector3 targetPosition, float aspectRatio)
    {
        TargetPosition = targetPosition;
        AspectRatio = aspectRatio;
        Up = Vector3.UnitY;
        UpdateCameraVectors();
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, TargetPosition, Up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(Fov),
            AspectRatio,
            Near,
            Far
        );
    }

    public void ProcessMouseMovement(float deltaX, float deltaY, float sensitivity = 0.2f)
    {
        deltaX *= sensitivity;
        deltaY *= sensitivity;

        Yaw += deltaX;
        Pitch -= deltaY;

        UpdateCameraVectors();
    }

    public void ProcessMouseScroll(float deltaY)
    {
        Distance -= deltaY * 0.5f;
        UpdateCameraVectors();
    }

    private void UpdateCameraVectors()
    {
        // Calculate the camera's front direction (where it's looking)
        Vector3 direction;
        direction.X = MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
        direction.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
        direction.Z = MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));

        Front = Vector3.Normalize(direction);
        Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));

        // Position camera behind and above the target
        Position = TargetPosition - Front * distance + Vector3.UnitY * 1.5f;
    }

    public Vector3 GetForwardDirection()
    {
        // Get forward direction on horizontal plane (for character movement)
        Vector3 forward = Front;
        forward.Y = 0;
        return Vector3.Normalize(forward);
    }

    public Vector3 GetRightDirection()
    {
        return Right;
    }
}

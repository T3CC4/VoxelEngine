using OpenTK.Mathematics;

namespace VoxelEngine.Graphics;

public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Front { get; private set; }
    public Vector3 Up { get; private set; }
    public Vector3 Right { get; private set; }

    private float yaw = -90.0f;
    private float pitch = 0.0f;

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

    public float Fov { get; set; } = 75.0f;
    public float AspectRatio { get; set; }
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 1000.0f;
    public int ViewDistanceChunks { get; set; } = 12;

    public Camera(Vector3 position, float aspectRatio)
    {
        Position = position;
        AspectRatio = aspectRatio;
        Up = Vector3.UnitY;
        UpdateCameraVectors();
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Front, Up);
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

    public void ProcessMouseMovement(float deltaX, float deltaY, float sensitivity = 0.1f)
    {
        deltaX *= sensitivity;
        deltaY *= sensitivity;

        Yaw += deltaX;
        Pitch -= deltaY;

        UpdateCameraVectors();
    }

    public void ProcessKeyboard(CameraMovement direction, float deltaTime, float speed = 5.0f)
    {
        float velocity = speed * deltaTime;

        switch (direction)
        {
            case CameraMovement.Forward:
                Position += Front * velocity;
                break;
            case CameraMovement.Backward:
                Position -= Front * velocity;
                break;
            case CameraMovement.Left:
                Position -= Right * velocity;
                break;
            case CameraMovement.Right:
                Position += Right * velocity;
                break;
            case CameraMovement.Up:
                Position += Up * velocity;
                break;
            case CameraMovement.Down:
                Position -= Up * velocity;
                break;
        }
    }

    public void UpdateCameraVectors()
    {
        Vector3 front;
        front.X = MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
        front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
        front.Z = MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));

        Front = Vector3.Normalize(front);
        Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));
    }
}

public enum CameraMovement
{
    Forward,
    Backward,
    Left,
    Right,
    Up,
    Down
}

using OpenTK.Mathematics;
using VoxelEngine.Core;
using VoxelEngine.Graphics;

namespace VoxelEngine.Game;

public class PlayerController
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; private set; }
    public Camera Camera { get; private set; }

    private const float Gravity = -20.0f;
    private const float JumpForce = 8.0f;
    private const float WalkSpeed = 5.0f;
    private const float SprintSpeed = 10.0f;
    private const float PlayerHeight = 1.8f;
    private const float PlayerRadius = 0.3f;

    private bool isGrounded = false;
    private IVoxelWorld world;

    public PlayerController(Vector3 startPosition, Camera camera, IVoxelWorld world)
    {
        Position = startPosition;
        Camera = camera;
        this.world = world;
        Velocity = Vector3.Zero;
    }

    public void Update(float deltaTime)
    {
        // Apply gravity
        Velocity += new Vector3(0, Gravity * deltaTime, 0);

        // Check if grounded
        isGrounded = CheckGrounded();

        if (isGrounded && Velocity.Y < 0)
        {
            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        }

        // Apply velocity
        Position += Velocity * deltaTime;

        // Simple collision detection
        ResolveCollisions();

        // Update camera position (eyes are at the top of player)
        Camera.Position = Position + new Vector3(0, PlayerHeight, 0);
    }

    public void ProcessMovement(Vector3 direction, float deltaTime, bool sprint = false)
    {
        float speed = sprint ? SprintSpeed : WalkSpeed;

        // Project movement onto horizontal plane
        Vector3 forward = Camera.Front;
        forward.Y = 0;
        forward = Vector3.Normalize(forward);

        Vector3 right = Camera.Right;

        Vector3 movement = (forward * direction.Z + right * direction.X) * speed * deltaTime;

        Position += movement;
        ResolveCollisions();
    }

    public void Jump()
    {
        if (isGrounded)
        {
            Velocity = new Vector3(Velocity.X, JumpForce, Velocity.Z);
        }
    }

    private bool CheckGrounded()
    {
        // Check slightly below player feet
        Vector3 checkPos = Position - new Vector3(0, 0.1f, 0);
        var voxel = world.GetVoxel(new Vector3Int(
            (int)MathF.Floor(checkPos.X),
            (int)MathF.Floor(checkPos.Y),
            (int)MathF.Floor(checkPos.Z)
        ));

        return voxel.IsActive && voxel.Type.IsSolid();
    }

    private void ResolveCollisions()
    {
        // Simple AABB collision with voxels
        Vector3Int playerVoxelPos = new Vector3Int(
            (int)MathF.Floor(Position.X),
            (int)MathF.Floor(Position.Y),
            (int)MathF.Floor(Position.Z)
        );

        // Check surrounding voxels
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 2; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int checkPos = playerVoxelPos + new Vector3Int(x, y, z);
                    var voxel = world.GetVoxel(checkPos);

                    if (voxel.IsActive && voxel.Type.IsSolid())
                    {
                        // Simple push-out collision
                        Vector3 voxelCenter = new Vector3(checkPos.X + 0.5f, checkPos.Y + 0.5f, checkPos.Z + 0.5f);
                        Vector3 delta = Position - voxelCenter;

                        float overlapX = PlayerRadius + 0.5f - MathF.Abs(delta.X);
                        float overlapY = PlayerHeight / 2 + 0.5f - MathF.Abs(delta.Y);
                        float overlapZ = PlayerRadius + 0.5f - MathF.Abs(delta.Z);

                        if (overlapX > 0 && overlapY > 0 && overlapZ > 0)
                        {
                            // Find minimum overlap axis and push out
                            if (overlapX < overlapY && overlapX < overlapZ)
                            {
                                Position += new Vector3(MathF.Sign(delta.X) * overlapX, 0, 0);
                            }
                            else if (overlapY < overlapZ)
                            {
                                Position += new Vector3(0, MathF.Sign(delta.Y) * overlapY, 0);
                                if (delta.Y < 0) Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
                            }
                            else
                            {
                                Position += new Vector3(0, 0, MathF.Sign(delta.Z) * overlapZ);
                            }
                        }
                    }
                }
            }
        }
    }
}

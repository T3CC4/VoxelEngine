namespace VoxelEngine.Core;

/// <summary>
/// A 2D vector with integer components
/// </summary>
public struct Vector2Int : IEquatable<Vector2Int>
{
    public int X { get; set; }
    public int Z { get; set; }

    public Vector2Int(int x, int z)
    {
        X = x;
        Z = z;
    }

    public static Vector2Int Zero => new Vector2Int(0, 0);
    public static Vector2Int One => new Vector2Int(1, 1);

    // Operators
    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        => new Vector2Int(a.X + b.X, a.Z + b.Z);

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
        => new Vector2Int(a.X - b.X, a.Z - b.Z);

    public static Vector2Int operator *(Vector2Int a, int scalar)
        => new Vector2Int(a.X * scalar, a.Z * scalar);

    public static Vector2Int operator *(int scalar, Vector2Int a)
        => new Vector2Int(a.X * scalar, a.Z * scalar);

    public static Vector2Int operator /(Vector2Int a, int scalar)
        => new Vector2Int(a.X / scalar, a.Z / scalar);

    public static bool operator ==(Vector2Int a, Vector2Int b)
        => a.X == b.X && a.Z == b.Z;

    public static bool operator !=(Vector2Int a, Vector2Int b)
        => !(a == b);

    // Methods
    public int ManhattanDistance(Vector2Int other)
        => Math.Abs(X - other.X) + Math.Abs(Z - other.Z);

    public float Distance(Vector2Int other)
    {
        int dx = X - other.X;
        int dz = Z - other.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    public override bool Equals(object? obj)
        => obj is Vector2Int other && Equals(other);

    public bool Equals(Vector2Int other)
        => X == other.X && Z == other.Z;

    public override int GetHashCode()
        => HashCode.Combine(X, Z);

    public override string ToString()
        => $"({X}, {Z})";
}

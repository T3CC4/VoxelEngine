namespace VoxelEngine.Core;

/// <summary>
/// Represents an RGBA color with float components (0.0 to 1.0 range).
/// Designed to be JSON-serializable and framework-independent.
/// </summary>
public struct Color : IEquatable<Color>
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }

    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    // Common color constants
    public static Color White => new(1f, 1f, 1f);
    public static Color Black => new(0f, 0f, 0f);
    public static Color Red => new(1f, 0f, 0f);
    public static Color Green => new(0f, 1f, 0f);
    public static Color Blue => new(0f, 0f, 1f);
    public static Color Yellow => new(1f, 1f, 0f);
    public static Color Cyan => new(0f, 1f, 1f);
    public static Color Magenta => new(1f, 0f, 1f);
    public static Color Gray => new(0.5f, 0.5f, 0.5f);
    public static Color Orange => new(1f, 0.65f, 0f);
    public static Color Purple => new(0.5f, 0f, 0.5f);
    public static Color Transparent => new(0f, 0f, 0f, 0f);

    // Grass/nature colors
    public static Color GrassGreen => new(0.3f, 0.7f, 0.2f);
    public static Color DarkGreen => new(0.15f, 0.6f, 0.15f);
    public static Color LightGreen => new(0.5f, 0.9f, 0.4f);
    public static Color Brown => new(0.6f, 0.4f, 0.2f);
    public static Color DarkBrown => new(0.4f, 0.25f, 0.15f);

    // Voxel type colors (matching VoxelType color definitions)
    public static Color VoxelGrass => new(0.2f, 0.8f, 0.2f);
    public static Color VoxelDirt => new(0.6f, 0.4f, 0.2f);
    public static Color VoxelStone => new(0.5f, 0.5f, 0.5f);
    public static Color VoxelWood => new(0.4f, 0.25f, 0.15f);
    public static Color VoxelLeaves => new(0.15f, 0.6f, 0.15f);
    public static Color VoxelSand => new(0.9f, 0.85f, 0.5f);
    public static Color VoxelWater => new(0.2f, 0.4f, 0.9f);
    public static Color VoxelBrick => new(0.8f, 0.2f, 0.2f);
    public static Color VoxelGlass => new(0.6f, 0.8f, 0.9f);

    // Conversion to OpenTK Vector3 (RGB only)
    public OpenTK.Mathematics.Vector3 ToVector3()
        => new(R, G, B);

    // Conversion to OpenTK Vector4 (RGBA)
    public OpenTK.Mathematics.Vector4 ToVector4()
        => new(R, G, B, A);

    // Conversion from OpenTK Vector3 (RGB, alpha = 1)
    public static Color FromVector3(OpenTK.Mathematics.Vector3 v)
        => new(v.X, v.Y, v.Z, 1.0f);

    // Conversion from OpenTK Vector4 (RGBA)
    public static Color FromVector4(OpenTK.Mathematics.Vector4 v)
        => new(v.X, v.Y, v.Z, v.W);

    // Implicit conversion to OpenTK Vector3
    public static implicit operator OpenTK.Mathematics.Vector3(Color c)
        => c.ToVector3();

    // Implicit conversion from OpenTK Vector3
    public static implicit operator Color(OpenTK.Mathematics.Vector3 v)
        => FromVector3(v);

    // Implicit conversion to OpenTK Vector4
    public static implicit operator OpenTK.Mathematics.Vector4(Color c)
        => c.ToVector4();

    // Implicit conversion from OpenTK Vector4
    public static implicit operator Color(OpenTK.Mathematics.Vector4 v)
        => FromVector4(v);

    // Color manipulation methods
    public Color WithAlpha(float alpha)
        => new(R, G, B, alpha);

    public Color Lerp(Color other, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            R + (other.R - R) * t,
            G + (other.G - G) * t,
            B + (other.B - B) * t,
            A + (other.A - A) * t
        );
    }

    public Color Multiply(float scalar)
        => new(R * scalar, G * scalar, B * scalar, A);

    public Color Add(Color other)
        => new(
            Math.Clamp(R + other.R, 0f, 1f),
            Math.Clamp(G + other.G, 0f, 1f),
            Math.Clamp(B + other.B, 0f, 1f),
            Math.Clamp(A + other.A, 0f, 1f)
        );

    // Create from byte values (0-255)
    public static Color FromBytes(byte r, byte g, byte b, byte a = 255)
        => new(r / 255f, g / 255f, b / 255f, a / 255f);

    // Create from hex string (#RRGGBB or #RRGGBBAA)
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length != 6 && hex.Length != 8)
            throw new ArgumentException("Hex color must be in format #RRGGBB or #RRGGBBAA");

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        byte a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

        return FromBytes(r, g, b, a);
    }

    // Convert to hex string
    public string ToHex(bool includeAlpha = false)
    {
        byte r = (byte)(R * 255);
        byte g = (byte)(G * 255);
        byte b = (byte)(B * 255);
        byte a = (byte)(A * 255);

        return includeAlpha
            ? $"#{r:X2}{g:X2}{b:X2}{a:X2}"
            : $"#{r:X2}{g:X2}{b:X2}";
    }

    // Equality
    public bool Equals(Color other)
        => Math.Abs(R - other.R) < 0.001f &&
           Math.Abs(G - other.G) < 0.001f &&
           Math.Abs(B - other.B) < 0.001f &&
           Math.Abs(A - other.A) < 0.001f;

    public override bool Equals(object? obj)
        => obj is Color other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    public override string ToString()
        => $"Color({R:F2}, {G:F2}, {B:F2}, {A:F2})";
}

namespace ReusableModules.SpatialMath;

[Serializable]
public struct Vector3
{
	public static readonly Vector3 UNIT_X = new(1f, 0f, 0f);
	public static readonly Vector3 UNIT_Y = new(0f, 1f, 0f);
	public static readonly Vector3 UNIT_Z = new(0f, 0f, 1f);
	public static readonly Vector3 ONE = new(1f, 1f, 1f);

	public float X;
	public float Y;
	public float Z;

	public Vector3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vector3(double x, double y, double z)
	{
		X = (float)x;
		Y = (float)y;
		Z = (float)z;
	}

	public readonly float Magnitude => MathF.Sqrt((X * X) + (Y * Y) + (Z * Z));
	public readonly float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);
	public readonly Vector3 Normalized => this / Magnitude;

	public static bool operator ==(Vector3 a, Vector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
	public static bool operator !=(Vector3 a, Vector3 b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;
	public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);
	public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
	public static Vector3 operator *(Vector3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
	public static Vector3 operator *(Vector3 a, double b) => new(a.X * b, a.Y * b, a.Z * b);
	public static Vector3 operator /(Vector3 a, Vector3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
	public static Vector3 operator /(Vector3 a, float b) => new(a.X / b, a.Y / b, a.Z / b);
	public static Vector3 operator /(Vector3 a, double b) => new(a.X / b, a.Y / b, a.Z / b);

	public override readonly bool Equals(object? obj) => obj is Vector3 vector && this == vector;
	public override readonly int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
	public override readonly string ToString() => $"(X:{X:F3}, Y:{Y:F3}, Z:{Z:F3})";
}

[Serializable]
public readonly struct Vector_3
{
	public static readonly Vector_3 UNIT_X = new(1f, 0f, 0f);
	public static readonly Vector_3 UNIT_Y = new(0f, 1f, 0f);
	public static readonly Vector_3 UNIT_Z = new(0f, 0f, 1f);
	public static readonly Vector_3 ONE    = new(1f, 1f, 1f);

	public readonly float X;
	public readonly float Y;
	public readonly float Z;

	public Vector_3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vector_3(double x, double y, double z)
	{
		unchecked
		{
			X = (float)x;
			Y = (float)y;
			Z = (float)z;
		}
	}

	public readonly double   Magnitude    => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
	public readonly float    SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);
	public readonly Vector_3 Normalized   => this / Magnitude;

	public static bool     operator ==(in Vector_3 a, in Vector_3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
	public static bool     operator !=(in Vector_3 a, in Vector_3 b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;
	public static Vector_3 operator + (in Vector_3 a, in Vector_3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	public static Vector_3 operator - (in Vector_3 a, in Vector_3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	public static Vector_3 operator - (in Vector_3 a)                => new(-a.X, -a.Y, -a.Z);
	public static Vector_3 operator * (in Vector_3 a, in Vector_3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
	public static Vector_3 operator * (in Vector_3 a, float b)       => new(a.X * b, a.Y * b, a.Z * b);
	public static Vector_3 operator * (in Vector_3 a, double b)      => new(a.X * b, a.Y * b, a.Z * b);
	public static Vector_3 operator / (in Vector_3 a, in Vector_3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
	public static Vector_3 operator / (in Vector_3 a, float b)       => new(a.X / b, a.Y / b, a.Z / b);
	public static Vector_3 operator / (in Vector_3 a, double b)      => new(a.X / b, a.Y / b, a.Z / b);

	public override readonly bool   Equals(object? obj) => obj is Vector_3 vector && this == vector;
	public override readonly int    GetHashCode()       => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
	public override readonly string ToString()          => $"{{ {X:F3}, {Y:F3}, {Z:F3} }}";
}

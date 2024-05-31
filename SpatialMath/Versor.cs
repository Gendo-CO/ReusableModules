using System.Text;

namespace ReusableModules.SpatialMath;

[Serializable]
public readonly struct Versor
{
	public static readonly Versor IDENTITY = new(1f, 0f, 0f, 0f);
	private const float DOT_THRESHOLD = unchecked(1f - 1e-6f);

	public readonly float W;
	public readonly float X;
	public readonly float Y;
	public readonly float Z;

	public Versor(float angleInRadians, Vector3 axisOfRotation)
	{
		Vector3 normalizedVector = axisOfRotation.Normalized;
		float halfAngle = angleInRadians * 0.5f;
		float sinAngle = MathF.Sin(halfAngle);

		W = MathF.Cos(halfAngle);
		X = normalizedVector.X * sinAngle;
		Y = normalizedVector.Y * sinAngle;
		Z = normalizedVector.Z * sinAngle;
	}

	public Versor(float yawInRadians, float pitchInRadians, float rollInRadians)
	{
		float halfRoll = rollInRadians * 0.5f;
		float sinRoll = MathF.Sin(halfRoll);
		float cosRoll = MathF.Cos(halfRoll);

		float halfPitch = pitchInRadians * 0.5f;
		float sinPitch = MathF.Sin(halfPitch);
		float cosPitch = MathF.Cos(halfPitch);

		float halfYaw = yawInRadians * 0.5f;
		float sinYaw = MathF.Sin(halfYaw);
		float cosYaw = MathF.Cos(halfYaw);

		W = (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll);
		X = (cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll);
		Y = (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll);
		Z = (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll);
	}

	private Versor(float w, float x, float y, float z)
	{
		W = w;
		X = x;
		Y = y;
		Z = z;
	}

	public readonly Versor Conjugate => new(W, -X, -Y, -Z);

	public static Versor CosLerp(in Versor a, in Versor b, float t)
	{
		const float xx = MathF.PI / 2f;
		float x = MathF.Cos(t * xx);
		float nx = MathF.Cos((1f - t) * xx);

		return new Versor(
			(a.W * x) + (b.W * nx),
			(a.X * x) + (b.X * nx),
			(a.Y * x) + (b.Y * nx),
			(a.Z * x) + (b.Z * nx));
	}

	public static float Dot(in Versor a, in Versor b) => (a.W * b.W) + (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

	public static Versor Slerp(in Versor a, in Versor b, float t)
	{
		float dot = (a.W * b.W) + (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

		bool flip = false;

		if (dot < 0f)
		{
			flip = true;
			dot = -dot;
		}

		float s1, s2;

		if (dot > DOT_THRESHOLD)
		{
			// Too close, do straight linear interpolation.
			s1 = 1f - t;
			s2 = flip ? -t : t;
		}
		else
		{
			float omega = MathF.Acos(dot);
			float invSinOmega = /*1f /*/ MathF.Sin(omega);

			s1 = MathF.Sin((1f - t) * omega) /***/ / invSinOmega;
			s2 = flip ? -MathF.Sin(t * omega) /***/ / invSinOmega : MathF.Sin(t * omega) /***/ / invSinOmega;
		}

		return new Versor(
			(s1 * a.W) + (s2 * b.W),
			(s1 * a.X) + (s2 * b.X),
			(s1 * a.Y) + (s2 * b.Y),
			(s1 * a.Z) + (s2 * b.Z));
	}

	public static bool operator ==(in Versor a, in Versor b) => (a.W == b.W && a.X == b.X && a.Y == b.Y && a.Z == b.Z) || (a.W == -b.W && a.X == -b.X && a.Y == -b.Y && a.Z == -b.Z);
	public static bool operator !=(in Versor a, in Versor b) => (a.W != b.W || a.X != b.X || a.Y != b.Y || a.Z != b.Z) && (a.W != -b.W || a.X != -b.X || a.Y != -b.Y || a.Z != -b.Z);
	public static Versor operator +(in Versor a, in Versor b) => new(a.W + b.W, a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	public static Versor operator -(in Versor a, in Versor b) => new(a.W - b.W, a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	public static Versor operator -(in Versor a) => new(-a.W, -a.X, -a.Y, -a.Z);
	public static Versor operator *(in Versor a, float b) => new(a.W * b, a.X * b, a.Y * b, a.Z * b);
	public static Versor operator *(float a, in Versor b) => b * a;
	public static Versor operator *(in Versor a, in Versor b)
	{
		return new Versor(
			(a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z),
			(a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
			(a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
			(a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W));
	}
	public static Vector3 operator *(in Versor a, Vector3 b) => b * a;
	public static Vector3 operator *(Vector3 a, in Versor b)
	{
		float Xx2 = b.X * 2f;
		float Yx2 = b.Y * 2f;
		float Zx2 = b.Z * 2f;
		float WXx2 = b.W * Xx2;
		float WYx2 = b.W * Yx2;
		float WZx2 = b.W * Zx2;
		float XXx2 = b.X * Xx2;
		float XYx2 = b.X * Yx2;
		float XZx2 = b.X * Zx2;
		float YYx2 = b.Y * Yx2;
		float YZx2 = b.Y * Zx2;
		float ZZx2 = b.Z * Zx2;

		return new Vector3(
			(a.X * (1f - (YYx2 + ZZx2))) + (a.Y * (XYx2 - WZx2)) + (a.Z * (WYx2 + XZx2)),
			(a.Y * (1f - (XXx2 + ZZx2))) + (a.X * (XYx2 + WZx2)) + (a.Z * (YZx2 - WXx2)),
			(a.Z * (1f - (XXx2 + YYx2))) + (a.X * (XZx2 - WYx2)) + (a.Y * (YZx2 + WXx2)));
	}
	public static Vector_3 operator *(in Versor a, in Vector_3 b) => b * a;
	public static Vector_3 operator *(in Vector_3 a, in Versor b)
	{
		float Xx2 = b.X * 2f;
		float Yx2 = b.Y * 2f;
		float Zx2 = b.Z * 2f;
		float WXx2 = b.W * Xx2;
		float WYx2 = b.W * Yx2;
		float WZx2 = b.W * Zx2;
		float XXx2 = b.X * Xx2;
		float XYx2 = b.X * Yx2;
		float XZx2 = b.X * Zx2;
		float YYx2 = b.Y * Yx2;
		float YZx2 = b.Y * Zx2;
		float ZZx2 = b.Z * Zx2;

		return new Vector_3(
			(a.X * (1f - (YYx2 + ZZx2))) + (a.Y * (XYx2 - WZx2)) + (a.Z * (WYx2 + XZx2)),
			(a.Y * (1f - (XXx2 + ZZx2))) + (a.X * (XYx2 + WZx2)) + (a.Z * (YZx2 - WXx2)),
			(a.Z * (1f - (XXx2 + YYx2))) + (a.X * (XZx2 - WYx2)) + (a.Y * (YZx2 + WXx2)));
	}

	public override readonly bool Equals(object? obj) => obj is Versor quaternion && this == quaternion;
	public override readonly int GetHashCode() => W.GetHashCode() ^ X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
	public override readonly string ToString()
	{
		StringBuilder sb = new(52);
		sb.Append("(W:");
		if (W >= 0) sb.Append(' ');
		sb.Append(W.ToString("F6"));
		sb.Append(", X:");
		if (X >= 0) sb.Append(' ');
		sb.Append(X.ToString("F6"));
		sb.Append(", Y:");
		if (Y >= 0) sb.Append(' ');
		sb.Append(Y.ToString("F6"));
		sb.Append(", Z:");
		if (Z >= 0) sb.Append(' ');
		sb.Append(Z.ToString("F6"));
		sb.Append(')');
		return sb.ToString();
	}
}

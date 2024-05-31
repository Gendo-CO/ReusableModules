using System.Numerics;
using System.Text;

namespace ReusableModules.Serialization;

/// <summary>
/// An implementation of a stream writer.
/// Very similar to System.IO.BinaryWriter, but this class was needed to guarantee that data
/// would be serialized in the same way that it would be deserialized by DeserializationTool.
/// </summary>
public sealed class SerializationTool : IDisposable
{
	// TODO: add internally-accessible methods that would let a source generator compose
	// a serialization method for the fields of arbitrary structs/classes

	private readonly Stream _stream;
	private bool _isDisposed = false;

	internal SerializationTool(Stream stream) => _stream = stream;

	public static SerializationTool? Request(string filePath)
	{
		// TODO: check permissions on the file
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

		FileStream fs = new(filePath, FileMode.Open);

		return new SerializationTool(fs);
	}

	public void Write(bool a) => _stream.WriteByte(a ? (byte)1 : (byte)0);
	public void Write(byte a) => _stream.WriteByte(a);
	public void Write(sbyte a) => _stream.WriteByte(unchecked((byte)a));
	public void Write(char a) => WriteInteger(a);
	public void Write(ushort a) => WriteInteger(a);
	public void Write(short a) => WriteInteger(a);
	public void Write(uint a) => WriteInteger(a);
	public void Write(int a) => WriteInteger(a);
	public void Write(ulong a) => WriteInteger(a);
	public void Write(long a) => WriteInteger(a);

	// Floating points are done like this because the IFloatingPoint<T> interface only
	// lets you write the significand and operand parts of the value separately, which
	// is weird and wasteful for this use case. We know floating points have to follow
	// the IEEE 754 standard, but IFloatingPointIeee754<T> and IBinaryFloatingPointIeee754<T>
	// don't have any write methods, so this is the next best thing.
	public unsafe void Write(float a) => WriteInteger(*((uint*)&a));
	public unsafe void Write(double a) => WriteInteger(*((ulong*)&a));

	public void Write(string a) => WriteString(a);
	public void Write(decimal a) => WriteDecimal(a);

	private unsafe void WriteInteger<T>(T a) where T : unmanaged, IBinaryInteger<T>
	{
		Span<byte> bytes = stackalloc byte[sizeof(T)];

		// Conversion to IBinaryInteger to use explicit interface implementation method
		((IBinaryInteger<T>)a).WriteBigEndian(bytes);
		_stream.Write(bytes);
	}

	// TODO: possibly limit string/byte-array size?
	private void WriteString(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			WriteInteger<int>(0);
		}

		byte[] b = Encoding.UTF8.GetBytes(s);
		WriteInteger(b.Length);
		_stream.Write(b, 0, b.Length);
	}

	private void WriteDecimal(decimal d)
	{
		// Always 4
		const int SIZE = sizeof(decimal) / sizeof(int);

		Span<int> ints = stackalloc int[SIZE];
		Span<byte> bytes = stackalloc byte[sizeof(int)];

		decimal.GetBits(d, ints);

		for (int i = 0; i < SIZE; i++)
		{
			((IBinaryInteger<int>)ints[i]).WriteBigEndian(bytes);
			_stream.Write(bytes);
		}
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		_isDisposed = true;
		_stream?.Dispose();
		GC.SuppressFinalize(this);
	}
}

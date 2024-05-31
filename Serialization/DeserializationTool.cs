using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace ReusableModules.Serialization;

/// <summary>
/// An implementation of a stream reader.
/// Very similar to System.IO.BinaryReader except this class is designed to
/// handle malformed data without throwing exceptions.
/// </summary>
public sealed class DeserializationTool : IDisposable
{
	private readonly Stream _stream;
	private bool _isDisposed = false;

	internal DeserializationTool(Stream stream) => _stream = stream;

	public static DeserializationTool? Request(string filePath)
	{
		// TODO: check permissions on the file
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

		FileStream fs = new(filePath, FileMode.Open);

		return new DeserializationTool(fs);
	}

	public bool TryReadBool(out bool a)
	{
		a = default;
		if (TryReadUnsignedInteger(out byte b))
		{
			a = b != 0;
			return true;
		}
		return false;
	}

	public bool TryReadByte(out byte a) => TryReadUnsignedInteger(out a);
	public bool TryReadSbyte(out sbyte a) => TryReadSignedInteger(out a);
	public bool TryReadChar(out char a) => TryReadUnsignedInteger(out a);
	public bool TryReadUshort(out ushort a) => TryReadUnsignedInteger(out a);
	public bool TryReadShort(out short a) => TryReadSignedInteger(out a);
	public bool TryReadUint(out uint a) => TryReadUnsignedInteger(out a);
	public bool TryReadInt(out int a) => TryReadSignedInteger(out a);
	public bool TryReadUlong(out ulong a) => TryReadUnsignedInteger(out a);
	public bool TryReadLong(out long a) => TryReadSignedInteger(out a);

	public bool TryReadFloat(out float a) => TryReadFloatingPoint<float, uint>(out a);
	public bool TryReadDouble(out double a) => TryReadFloatingPoint<double, ulong>(out a);

	public bool TryReadString([NotNullWhen(true)] out string? a)
	{
		a = default;

		if (_stream.Length - _stream.Position < sizeof(int)) return false;

		Span<byte> bytes = stackalloc byte[sizeof(int)];
		_stream.ReadExactly(bytes);
		int size = InterpretSignedInteger<int>(bytes);

		if (_stream.Length - _stream.Position < size) return false;

		byte[] buffer = new byte[size];
		_stream.ReadExactly(buffer, 0, size);

		char[] output = new char[Encoding.UTF8.GetMaxCharCount(size)];
		if (Encoding.UTF8.TryGetChars(buffer, output, out int len))
		{
			a = new string(output, 0, len);
			return true;
		}

		return false;
	}

	public bool TryReadDecimal(out decimal a)
	{
		if (_stream.Length - _stream.Position < sizeof(decimal))
		{
			a = default;
			return false;
		}

		Span<byte> bytes = stackalloc byte[sizeof(decimal)];
		_stream.ReadExactly(bytes);

		// Always 4
		const int SIZE = sizeof(decimal) / sizeof(int);

		Span<int> ints = stackalloc int[SIZE];
		for (int i = 0; i < SIZE; i++)
		{
			Span<byte> slice = bytes.Slice(i * sizeof(int), sizeof(int));
			ints[i] = InterpretSignedInteger<int>(slice);
		}

		a = new decimal(ints);
		return true;
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		_isDisposed = true;
		_stream?.Dispose();
		GC.SuppressFinalize(this);
	}

	private unsafe bool TryReadSignedInteger<T>(out T a) where T : unmanaged, IBinaryInteger<T>
	{
		if (_stream.Length - _stream.Position < sizeof(T))
		{
			a = default;
			return false;
		}

		Span<byte> bytes = stackalloc byte[sizeof(T)];
		_stream.ReadExactly(bytes);
		a = T.ReadBigEndian(bytes, false);
		return true;
	}

	private unsafe bool TryReadUnsignedInteger<T>(out T a) where T : unmanaged, IBinaryInteger<T>
	{
		if (_stream.Length - _stream.Position < sizeof(T))
		{
			a = default;
			return false;
		}

		Span<byte> bytes = stackalloc byte[sizeof(T)];
		_stream.ReadExactly(bytes);
		a = T.ReadBigEndian(bytes, true);
		return true;
	}

	// Floating points are done like this to match how they are handled in SerializationTool.
	// Please see the comment in SerializationTool for a more in-depth explanation.
	// NOTE: Generic parameters T and U *must* be the same size. Wish C# had static assertions...
	private unsafe bool TryReadFloatingPoint<T, U>(out T a) where T : unmanaged, IFloatingPoint<T> where U : unmanaged, IBinaryInteger<U>
	{
		a = default;
		if (TryReadUnsignedInteger(out U b))
		{
			a = *((T*)&b);
			return true;
		}
		return false;
	}

	// Workaround for compile-time issue with IBinaryInteger<>.ReadBigEndian().
	// I have absolutely no idea why I can't directly call <integer type>.ReadBigEndian().
	// Possibly related, though: for some reason Visual Studio's interpretation of the
	// metadata of IBinaryInteger.cs is that ReadBigEndian is a static method and not a
	// static *virtual* method, like how it is in the official .NET Github repo...
	private static T InterpretSignedInteger<T>(ReadOnlySpan<byte> bytes) where T : unmanaged, IBinaryInteger<T> => T.ReadBigEndian(bytes, false);
	private static T InterpretUnsignedInteger<T>(ReadOnlySpan<byte> bytes) where T : unmanaged, IBinaryInteger<T> => T.ReadBigEndian(bytes, true);
}

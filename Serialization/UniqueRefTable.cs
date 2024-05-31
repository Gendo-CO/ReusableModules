using System.Runtime.CompilerServices;

namespace ReusableModules.Serialization;

/// <summary>
/// Utility data structure for serialization.
/// Tracks unique references and gives an ID for each one.
/// </summary>
public ref struct UniqueRefTable
{
	// Implementation-wise, this is a hashtable. A given reference's ID is its index in
	// the backing array. Because holding references will prevent garbage collection,
	// this table is implemented as a ref struct to intentionally limit its lifetime.

	private static readonly int INITIAL_SIZE = Constants.PRIMES[1];

	private object[] _refs = new object[INITIAL_SIZE];
	private int[] _beginningIndices = new int[INITIAL_SIZE];
	private int[] _nextIndices = new int[INITIAL_SIZE];
	private int _count = 0;

	public UniqueRefTable() => Array.Fill(_beginningIndices, -1);

	/// <summary>
	/// Retrieves an ID for a reference from the table. Generates a new ID if
	/// the reference in question isn't already in the table.
	/// </summary>
	/// <param name="o">The reference to retrieve an ID for.</param>
	/// <returns>
	/// The reference's ID. The ID will be a positive number if the reference
	/// given is new to the table. If the reference already exists in the table,
	/// the returned value will be the ID bit-flipped (~id). Null references will
	/// always return Int32.MinValue.
	/// </returns>
	public int GetId(object o)
	{
		if (o == null) return int.MinValue;

		int hash = RuntimeHelpers.GetHashCode(o);
		int targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;

		for (int i = _beginningIndices[targetBucket]; i > -1; i = _nextIndices[i])
		{
			if (ReferenceEquals(o, _refs[i])) return ~i;
		}

		if (_count >= _refs.Length)
		{
			Resize();
			targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;
		}

		int currLoc = _count;
		checked { _count++; }

		_refs[currLoc] = o;
		_nextIndices[currLoc] = _beginningIndices[targetBucket];
		_beginningIndices[targetBucket] = currLoc;

		return currLoc;
	}

	private void Resize()
	{
		int newSize = _refs.Length * 2;
		newSize = Constants.PRIMES.First(x => x > newSize);

		Array.Resize(ref _refs, newSize);
		_beginningIndices = new int[newSize];
		_nextIndices = new int[newSize];

		Array.Fill(_beginningIndices, -1);

		for (int i = 0; i < _count; i++)
		{
			int targetBucket = (RuntimeHelpers.GetHashCode(_refs[i]) & 0x7FFFFFFF) % newSize;
			_nextIndices[i] = _beginningIndices[targetBucket];
			_beginningIndices[targetBucket] = i;
		}
	}
}

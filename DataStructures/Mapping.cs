namespace ReusableModules.DataStructures;

/// <summary>
/// Simplified implementation of System.Collections.Generic.Dictionary.
/// Highly specialized implementations of Dictionaries can be made using this source code as a base.
/// </summary>
/// <typeparam name="T">Key type</typeparam>
/// <typeparam name="U">Value type</typeparam>
[Serializable]
public class Mapping<T, U>
{
	// The choice to use powers of 2 for the number of buckets instead of prime numbers boils down to practical performance.
	// Using prime numbers made distribution of entries much better, decreasing the number of hash collisions by up to 50% in some tests.
	// In spite of this, the version of Mapping that used a scaling power of 2 for the array size was faster in every category (adding, removing,
	// and finding) than the version that used scaling primes for the array size, which was unexpected.
	// It may just be that processing units are so optimized for math and memory operations using powers of 2 that it outweighs the benefits
	// of using a schema to reduce hash collisions.
	private const int INITIAL_SIZE = 8;

#if PROFILE_COLLISIONS
	private ulong _collisions = 0;
#endif
	private readonly EqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;

	private int[] _beginningIndices = new int[INITIAL_SIZE]!;
	private Entry[] _entries = new Entry[INITIAL_SIZE]!;

	private int _count;
	private int _nextFreeEntry;

	private struct Entry
	{
		public T Key;
		public U Val; // avoiding ambiguity with the "value" keyword
		public int Next;
	}

	public Mapping()
	{
		for (int i = 0; i < INITIAL_SIZE; i++)
		{
			_beginningIndices[i] = -1;
			_entries[i].Next = ++_nextFreeEntry;
		}

		_nextFreeEntry = 0;
	}

	public int Count => _count;

#if PROFILE_COLLISIONS
	public ulong Collisions => _collisions;
#endif

	public bool Contains(T key) => Find(key, out _);

	public U? this[T key]
	{
		get => Find(key, out int index) ? _entries[index].Val : default;
		set => Add(key, value!);
	}

	private bool Find(T key, out int index)
	{
		if (key == null)
		{
			index = default;
			return false;
		}

		int hash = key.GetHashCode();

		for (index = _beginningIndices[(hash & 0x7FFFFFFF) % _beginningIndices.Length]; index > -1; index = _entries[index].Next)
		{
			if (hash == _entries[index].Key!.GetHashCode() && _equalityComparer.Equals(key, _entries[index].Key))
				return true;

#if PROFILE_COLLISIONS
			_collisions++;
#endif
		}

		return false;
	}

	public void Add(T key, U val)
	{
		if (key == null) return;

		if (_count >= _beginningIndices.Length) Resize();

		int hash = key.GetHashCode();
		int targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;

		for (int i = _beginningIndices[targetBucket]; i > -1; i = _entries[i].Next)
		{
			if (hash == _entries[i].Key!.GetHashCode() && _equalityComparer.Equals(key, _entries[i].Key))
			{
				_entries[i].Val = val;
				return;
			}

#if PROFILE_COLLISIONS
			_collisions++;
#endif
		}

		int indexForNewEntry = _nextFreeEntry;
		_nextFreeEntry = _entries[indexForNewEntry].Next;

		_entries[indexForNewEntry].Key = key;
		_entries[indexForNewEntry].Val = val;
		_entries[indexForNewEntry].Next = _beginningIndices[targetBucket];
		_beginningIndices[targetBucket] = indexForNewEntry;

		_count++;
	}

	private void Resize()
	{
		int newSize = _beginningIndices.Length * 2;

		_beginningIndices = new int[newSize];
		for (int i = 0; i < newSize; i++) _beginningIndices[i] = -1;

		Array.Resize(ref _entries, newSize);

		for (int i = 0; i < _count; i++)
		{
			int targetBucket = (_entries[i].Key!.GetHashCode() & 0x7FFFFFFF) % newSize;
			_entries[i].Next = _beginningIndices[targetBucket];
			_beginningIndices[targetBucket] = i;
		}

		_nextFreeEntry = _count;

		for (int i = _count; i < newSize; i++)
		{
			_entries[i].Next = ++_nextFreeEntry;
		}

		_nextFreeEntry = _count;
	}

	public bool Remove(T key)
	{
		if (key == null) return false;

		int hash = key.GetHashCode();
		int targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;

		for (ref int i = ref _beginningIndices[targetBucket]; i > -1; i = ref _entries[i].Next)
		{
			if (hash != _entries[i].Key!.GetHashCode() || !_equalityComparer.Equals(key, _entries[i].Key))
			{
#if PROFILE_COLLISIONS
				_collisions++;
#endif
				continue;
			}

			int j = _entries[i].Next;

			_entries[i].Key = default!;
			_entries[i].Val = default!;
			_entries[i].Next = _nextFreeEntry;
			_nextFreeEntry = i;
			i = j;
			_count--;

			return true;
		}

		return false;
	}
}

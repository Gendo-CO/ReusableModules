using System.Runtime.CompilerServices;

namespace ReusableModules.DataStructures;

/// <summary>
/// An implementation of a dictionary, where the values are structs that are passed by reference, and
/// the collection can be iterated using an index accessor (ex. via a for-loop) instead of needing to use
/// an enumerator (ex. via a foreach-loop)
/// </summary>
/// <typeparam name="T">Type of keys in dictionary</typeparam>
/// <typeparam name="U">Type of values in dictionary</typeparam>
[Serializable]
public sealed class IndexedStructDictionary<T, U> where U : struct
{
	//--NESTED TYPES--//

	public readonly ref struct KeyValuePair
	{
		public readonly T Key;
		public readonly ref U Val;

		public KeyValuePair(T key, ref U val)
		{
			Key = key;
			Val = ref val;
		}

		public override readonly string ToString() => $"{{ {Key}, {Val} }}";
	}

	public ref struct Enumerator
	{
		private readonly IndexedStructDictionary<T, U> _map;
		private KeyValuePair _current = new();
		private int _index = 0;

		internal Enumerator(IndexedStructDictionary<T, U> map) => _map = map;

		public readonly KeyValuePair Current => _current;

		public bool MoveNext()
		{
			if (_index < _map._count)
			{
				_current = new(_map._keys[_index], ref _map._values[_index]);
				_index++;
				return true;
			}

			return false;
		}
	}


	//--CONSTANTS & FIELDS--//

	private const int INITIAL_SIZE = 8;

#if PROFILE_COLLISIONS
	[NonSerialized] private ulong _collisions = 0;
#endif
	private int[] _beginningIndices = new int[INITIAL_SIZE];
	private int[] _nextValues = new int[INITIAL_SIZE];
	private T[] _keys = new T[INITIAL_SIZE];
	private U[] _values = new U[INITIAL_SIZE];
	private int _count;


	//--METHODS--//

	public IndexedStructDictionary()
	{
		for (int i = 0; i < INITIAL_SIZE; i++) _beginningIndices[i] = -1;
	}

#if PROFILE_COLLISIONS
	public ulong Collisions => _collisions;
#endif

	public int Count => _count;

	public bool Contains(T key) => Find(key, out _);

	public KeyValuePair At(int index) => (index < 0 || index >= _count) ? throw new ArgumentOutOfRangeException(nameof(index)) : new(_keys[index], ref _values[index]);

	public ref U this[T key]
	{
		get
		{
			if (key == null) return ref Unsafe.NullRef<U>();

			int hash = key.GetHashCode();
			int targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;

			for (int index = _beginningIndices[targetBucket]; index > -1; index = _nextValues[index])
			{
				if (hash == _keys[index]!.GetHashCode() && EqualityComparer<T>.Default.Equals(key, _keys[index]))
				{
					return ref _values[index];
				}

#if PROFILE_COLLISIONS
				_collisions++;
#endif
			}

			if (_count >= _beginningIndices.Length)
			{
				Resize();
				targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;
			}

			_keys[_count] = key;
			_nextValues[_count] = _beginningIndices[targetBucket];
			_beginningIndices[targetBucket] = _count;

			_count++;

			return ref _values[_count - 1];
		}
	}

	public void Add(T key, U val)
	{
		if (key == null) return;

		int hash = key.GetHashCode();
		int targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;

		for (int i = _beginningIndices[targetBucket]; i > -1; i = _nextValues[i])
		{
			if (hash == _keys[i]!.GetHashCode() && EqualityComparer<T>.Default.Equals(key, _keys[i]))
			{
				_values[i] = val;
				return;
			}

#if PROFILE_COLLISIONS
			_collisions++;
#endif
		}

		if (_count >= _beginningIndices.Length)
		{
			Resize();
			targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;
		}

		_keys[_count] = key;
		_values[_count] = val;
		_nextValues[_count] = _beginningIndices[targetBucket];
		_beginningIndices[targetBucket] = _count;

		_count++;
	}

	public bool Find(T key, out int index)
	{
		if (key == null)
		{
			index = -1;
			return false;
		}

		int hash = key.GetHashCode();

		for (index = _beginningIndices[(hash & 0x7FFFFFFF) % _beginningIndices.Length]; index > -1; index = _nextValues[index])
		{
			if (hash == _keys[index]!.GetHashCode() && EqualityComparer<T>.Default.Equals(key, _keys[index]))
			{
				return true;
			}

#if PROFILE_COLLISIONS
			_collisions++;
#endif
		}

		return false;
	}

	public Enumerator GetEnumerator() => new(this);

	public bool Remove(T key)
	{
		if (key == null) return false;

		int hash = key.GetHashCode();
		int targetBucket = (hash & 0x7FFFFFFF) % _beginningIndices.Length;

		for (ref int i = ref _beginningIndices[targetBucket]; i > -1; i = ref _nextValues[i])
		{
			if (hash != _keys[i]!.GetHashCode() || !EqualityComparer<T>.Default.Equals(key, _keys[i]))
			{
#if PROFILE_COLLISIONS
				_collisions++;
#endif
				continue;
			}

			_count--;

			if (i == _count)
			{
				i = _nextValues[_count];
			}
			else
			{
				_keys[i] = _keys[_count];
				_values[i] = _values[_count];

				if (_nextValues[i] == _count)
				{
					_nextValues[i] = _nextValues[_count];
				}
				else
				{
					int hash2 = _keys[_count]!.GetHashCode();
					int targetBucket2 = (hash2 & 0x7FFFFFFF) % _beginningIndices.Length;

					ref int j = ref _beginningIndices[targetBucket2];
					while (j != _count) j = ref _nextValues[j];

					j = i;
					i = _nextValues[i];
					_nextValues[j] = _nextValues[_count];
				}
			}

			_keys[_count] = default!;
			_values[_count] = default!;

			return true;
		}

		return false;
	}

	private void Resize()
	{
		int newSize = _beginningIndices.Length * 2;

		_beginningIndices = new int[newSize];
		for (int i = 0; i < newSize; i++) _beginningIndices[i] = -1;

		_nextValues = new int[newSize];
		Array.Resize(ref _keys, newSize);
		Array.Resize(ref _values, newSize);

		for (int i = 0; i < _count; i++)
		{
			int targetBucket = (_keys[i]!.GetHashCode() & 0x7FFFFFFF) % newSize;
			_nextValues[i] = _beginningIndices[targetBucket];
			_beginningIndices[targetBucket] = i;
		}
	}
}

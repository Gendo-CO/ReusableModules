using System.Reflection;

namespace ReusableModules.Events;

/// <summary>
/// Event that allows subscribing objects to be garbage-collected, unlike regular multicast delegates.
/// </summary>
public sealed class WeakEvent
{
	private struct WeakDelegateEntry
	{
		public readonly WeakReference WeakTargetRef;
		public MethodInfo? Method;
		public int Next;

		public WeakDelegateEntry(int nextFreeEntry)
		{
			WeakTargetRef = new WeakReference(null);
			Method = null;
			Next = nextFreeEntry;
		}
	}

	private WeakDelegateEntry[] _entries = new WeakDelegateEntry[8];
	private int _count = 0;
	private int _firstFreeEntryIndex = 0;
	private int _firstUsedEntryIndex = -1;

	public WeakEvent()
	{
		for (int i = 0; i < _entries.Length;)
			_entries[i] = new WeakDelegateEntry(++i);
	}

	public void Invoke()
	{
		for (ref int i = ref _firstUsedEntryIndex; i > -1;)
		{
			if (!_entries[i].Method!.IsStatic && !_entries[i].WeakTargetRef.IsAlive)
			{
				_entries[i].WeakTargetRef.Target = null;
				_entries[i].Method = null;

				int nextUsedEntry = _entries[i].Next;
				_entries[i].Next = _firstFreeEntryIndex;
				_firstFreeEntryIndex = i;
				i = nextUsedEntry;

				_count--;
				continue;
			}

			_entries[i].Method!.Invoke(_entries[i].WeakTargetRef.Target, null);
			i = ref _entries[i].Next;
		}
	}

	public void Subscribe(Action delegateToAdd)
	{
		if (delegateToAdd == null) return;

		if (_count >= _entries.Length)
		{
			int newSize = _entries.Length * 2;

			Array.Resize(ref _entries, newSize);

			for (int i = _count; i < newSize;)
				_entries[i] = new WeakDelegateEntry(++i);
		}

		_entries[_firstFreeEntryIndex].WeakTargetRef.Target = delegateToAdd.Target;
		_entries[_firstFreeEntryIndex].Method = delegateToAdd.Method;

		int newFirstFreeEntry = _entries[_firstFreeEntryIndex].Next;
		_entries[_firstFreeEntryIndex].Next = _firstUsedEntryIndex;
		_firstUsedEntryIndex = _firstFreeEntryIndex;
		_firstFreeEntryIndex = newFirstFreeEntry;

		_count++;
	}

	public bool Unsubscribe(Action delegateToRemove)
	{
		if (delegateToRemove == null) return false;

		for (ref int i = ref _firstUsedEntryIndex; i > -1; i = ref _entries[i].Next)
		{
			if (delegateToRemove.Target == _entries[i].WeakTargetRef.Target && delegateToRemove.Method == _entries[i].Method)
			{
				_entries[i].WeakTargetRef.Target = null;
				_entries[i].Method = null;

				int nextUsedEntry = _entries[i].Next;
				_entries[i].Next = _firstFreeEntryIndex;
				_firstFreeEntryIndex = i;
				i = nextUsedEntry;

				_count--;
				return true;
			}
		}

		return false;
	}
}

/// <summary>
/// Event that allows subscribing objects to be garbage-collected, unlike regular multicast delegates.
/// </summary>
/// <typeparam name="T">The type of the parameter to pass when the event is invoked.</typeparam>
public sealed class WeakEvent<T>
{
	private struct WeakDelegateEntry
	{
		public readonly WeakReference WeakTargetRef;
		public MethodInfo? Method;
		public int Next;

		public WeakDelegateEntry(int nextFreeEntry)
		{
			WeakTargetRef = new WeakReference(null);
			Method = null;
			Next = nextFreeEntry;
		}
	}

	private WeakDelegateEntry[] _entries = new WeakDelegateEntry[8];
	private int _count = 0;
	private int _firstFreeEntryIndex = 0;
	private int _firstUsedEntryIndex = -1;

	public WeakEvent()
	{
		for (int i = 0; i < _entries.Length;)
			_entries[i] = new WeakDelegateEntry(++i);
	}

	public void Invoke(T param)
	{
		object?[] paramObjects = new object?[] { param };

		for (ref int i = ref _firstUsedEntryIndex; i > -1;)
		{
			if (!_entries[i].Method!.IsStatic && !_entries[i].WeakTargetRef.IsAlive)
			{
				_entries[i].WeakTargetRef.Target = null;
				_entries[i].Method = null;

				int nextUsedEntry = _entries[i].Next;
				_entries[i].Next = _firstFreeEntryIndex;
				_firstFreeEntryIndex = i;
				i = nextUsedEntry;

				_count--;
				continue;
			}

			_entries[i].Method!.Invoke(_entries[i].WeakTargetRef.Target, paramObjects);
			i = ref _entries[i].Next;
		}
	}

	public void Subscribe(Action<T> delegateToAdd)
	{
		if (delegateToAdd == null) return;

		if (_count >= _entries.Length)
		{
			int newSize = _entries.Length * 2;

			Array.Resize(ref _entries, newSize);

			for (int i = _count; i < newSize;)
				_entries[i] = new WeakDelegateEntry(++i);
		}

		_entries[_firstFreeEntryIndex].WeakTargetRef.Target = delegateToAdd.Target;
		_entries[_firstFreeEntryIndex].Method = delegateToAdd.Method;

		int newFirstFreeEntry = _entries[_firstFreeEntryIndex].Next;
		_entries[_firstFreeEntryIndex].Next = _firstUsedEntryIndex;
		_firstUsedEntryIndex = _firstFreeEntryIndex;
		_firstFreeEntryIndex = newFirstFreeEntry;

		_count++;
	}

	public bool Unsubscribe(Action<T> delegateToRemove)
	{
		if (delegateToRemove == null) return false;

		for (ref int i = ref _firstUsedEntryIndex; i > -1; i = ref _entries[i].Next)
		{
			if (delegateToRemove.Target == _entries[i].WeakTargetRef.Target && delegateToRemove.Method == _entries[i].Method)
			{
				_entries[i].WeakTargetRef.Target = null;
				_entries[i].Method = null;

				int nextUsedEntry = _entries[i].Next;
				_entries[i].Next = _firstFreeEntryIndex;
				_firstFreeEntryIndex = i;
				i = nextUsedEntry;

				_count--;
				return true;
			}
		}

		return false;
	}
}

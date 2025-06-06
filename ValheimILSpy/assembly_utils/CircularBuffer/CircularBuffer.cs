using System;
using System.Collections;
using System.Collections.Generic;

namespace CircularBuffer;

public class CircularBuffer<T> : IEnumerable<T>, IEnumerable
{
	private readonly T[] _buffer;

	private int _start;

	private int _end;

	private int _size;

	public int Capacity => _buffer.Length;

	public bool IsFull => Size == Capacity;

	public bool IsEmpty => Size == 0;

	public int Size => _size;

	public T this[int index]
	{
		get
		{
			if (IsEmpty)
			{
				throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
			}
			if (index >= _size)
			{
				throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {_size}");
			}
			int num = InternalIndex(index);
			return _buffer[num];
		}
		set
		{
			if (IsEmpty)
			{
				throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty");
			}
			if (index >= _size)
			{
				throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer size is {_size}");
			}
			int num = InternalIndex(index);
			_buffer[num] = value;
		}
	}

	public CircularBuffer(int capacity)
		: this(capacity, new T[0])
	{
	}

	public CircularBuffer(int capacity, T[] items)
	{
		if (capacity < 1)
		{
			throw new ArgumentException("Circular buffer cannot have negative or zero capacity.", "capacity");
		}
		if (items == null)
		{
			throw new ArgumentNullException("items");
		}
		if (items.Length > capacity)
		{
			throw new ArgumentException("Too many items to fit circular buffer", "items");
		}
		_buffer = new T[capacity];
		Array.Copy(items, _buffer, items.Length);
		_size = items.Length;
		_start = 0;
		_end = ((_size != capacity) ? _size : 0);
	}

	public T Front()
	{
		ThrowIfEmpty();
		return _buffer[_start];
	}

	public T Back()
	{
		ThrowIfEmpty();
		return _buffer[((_end != 0) ? _end : Capacity) - 1];
	}

	public void PushBack(T item)
	{
		if (IsFull)
		{
			_buffer[_end] = item;
			Increment(ref _end);
			_start = _end;
		}
		else
		{
			_buffer[_end] = item;
			Increment(ref _end);
			_size++;
		}
	}

	public void PushFront(T item)
	{
		if (IsFull)
		{
			Decrement(ref _start);
			_end = _start;
			_buffer[_start] = item;
		}
		else
		{
			Decrement(ref _start);
			_buffer[_start] = item;
			_size++;
		}
	}

	public void PopBack()
	{
		ThrowIfEmpty("Cannot take elements from an empty buffer.");
		Decrement(ref _end);
		_buffer[_end] = default(T);
		_size--;
	}

	public void PopFront()
	{
		ThrowIfEmpty("Cannot take elements from an empty buffer.");
		_buffer[_start] = default(T);
		Increment(ref _start);
		_size--;
	}

	public void Clear()
	{
		_start = 0;
		_end = 0;
		_size = 0;
		Array.Clear(_buffer, 0, _buffer.Length);
	}

	public T[] ToArray()
	{
		T[] array = new T[Size];
		int num = 0;
		foreach (ArraySegment<T> item in ToArraySegments())
		{
			Array.Copy(item.Array, item.Offset, array, num, item.Count);
			num += item.Count;
		}
		return array;
	}

	public IList<ArraySegment<T>> ToArraySegments()
	{
		return new ArraySegment<T>[2]
		{
			ArrayOne(),
			ArrayTwo()
		};
	}

	public IEnumerator<T> GetEnumerator()
	{
		IList<ArraySegment<T>> list = ToArraySegments();
		foreach (ArraySegment<T> segment in list)
		{
			for (int i = 0; i < segment.Count; i++)
			{
				yield return segment.Array[segment.Offset + i];
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(message);
		}
	}

	private void Increment(ref int index)
	{
		if (++index == Capacity)
		{
			index = 0;
		}
	}

	private void Decrement(ref int index)
	{
		if (index == 0)
		{
			index = Capacity;
		}
		index--;
	}

	private int InternalIndex(int index)
	{
		return _start + ((index < Capacity - _start) ? index : (index - Capacity));
	}

	private ArraySegment<T> ArrayOne()
	{
		if (IsEmpty)
		{
			return new ArraySegment<T>(new T[0]);
		}
		if (_start < _end)
		{
			return new ArraySegment<T>(_buffer, _start, _end - _start);
		}
		return new ArraySegment<T>(_buffer, _start, _buffer.Length - _start);
	}

	private ArraySegment<T> ArrayTwo()
	{
		if (IsEmpty)
		{
			return new ArraySegment<T>(new T[0]);
		}
		if (_start < _end)
		{
			return new ArraySegment<T>(_buffer, _end, 0);
		}
		return new ArraySegment<T>(_buffer, 0, _end);
	}
}

using System;
using System.Collections;
using System.Collections.Generic;

public class BinarySearchDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, ICloneable where TKey : IComparable<TKey>
{
	private KeyValuePair<TKey, TValue>[] m_block;

	private int length;

	public TValue this[TKey key]
	{
		get
		{
			if (length <= 0)
			{
				throw new KeyNotFoundException("This BinarySearchDictionary is empty!");
			}
			bool exactMatch;
			int num = BinaryFindKeyIndex(key, out exactMatch);
			if (!exactMatch)
			{
				throw new KeyNotFoundException("Key could not be found in this BinarySearchDictionary!");
			}
			return m_block[num].Value;
		}
		set
		{
			if (length <= 0)
			{
				GuaranteeCapacity();
				m_block[0] = new KeyValuePair<TKey, TValue>(key, value);
				length++;
				return;
			}
			bool exactMatch;
			int num = BinaryFindKeyIndex(key, out exactMatch);
			if (exactMatch)
			{
				m_block[num] = new KeyValuePair<TKey, TValue>(key, value);
				return;
			}
			GuaranteeCapacity();
			if (length - num > 0)
			{
				Array.Copy(m_block, num, m_block, num + 1, length - num);
			}
			m_block[num] = new KeyValuePair<TKey, TValue>(key, value);
			length++;
		}
	}

	public ICollection<TKey> Keys
	{
		get
		{
			TKey[] array = new TKey[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = m_block[i].Key;
			}
			return array;
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			TValue[] array = new TValue[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = m_block[i].Value;
			}
			return array;
		}
	}

	public int Count => length;

	public bool IsReadOnly => false;

	public int Capacity
	{
		get
		{
			if (m_block != null)
			{
				return m_block.Length;
			}
			return 0;
		}
		set
		{
			if (Capacity < value)
			{
				if (m_block == null)
				{
					m_block = new KeyValuePair<TKey, TValue>[value];
					return;
				}
				KeyValuePair<TKey, TValue>[] block = m_block;
				m_block = new KeyValuePair<TKey, TValue>[value];
				Array.Copy(block, m_block, length);
			}
		}
	}

	public object Clone()
	{
		return MemberwiseClone();
	}

	public void Add(TKey key, TValue value)
	{
		GuaranteeCapacity();
		bool exactMatch;
		int num = BinaryFindKeyIndex(key, out exactMatch);
		if (exactMatch)
		{
			throw new ArgumentException("Duplicate keys are not allowed!");
		}
		if (length - num > 0)
		{
			Array.Copy(m_block, num, m_block, num + 1, length - num);
		}
		m_block[num] = new KeyValuePair<TKey, TValue>(key, value);
		length++;
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	public void Clear()
	{
		length = 0;
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		if (length <= 0)
		{
			return false;
		}
		bool exactMatch;
		int num = BinaryFindKeyIndex(item.Key, out exactMatch);
		if (exactMatch)
		{
			return Compare(m_block[num].Value, item.Value);
		}
		return false;
	}

	public bool ContainsKey(TKey key)
	{
		if (length <= 0)
		{
			return false;
		}
		BinaryFindKeyIndex(key, out var exactMatch);
		return exactMatch;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		if (length > 0)
		{
			Array.Copy(m_block, 0, array, arrayIndex, length);
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		for (int i = 0; i < length; i++)
		{
			yield return m_block[i];
		}
	}

	public bool Remove(TKey key)
	{
		if (length <= 0)
		{
			return false;
		}
		bool exactMatch;
		int num = BinaryFindKeyIndex(key, out exactMatch);
		if (!exactMatch)
		{
			return false;
		}
		if (length - (num + 1) > 0)
		{
			Array.Copy(m_block, num + 1, m_block, num, length - (num + 1));
		}
		length--;
		return true;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		if (length <= 0)
		{
			return false;
		}
		bool exactMatch;
		int num = BinaryFindKeyIndex(item.Key, out exactMatch);
		if (!exactMatch || !Compare(m_block[num].Value, item.Value))
		{
			return false;
		}
		if (length - (num + 1) > 0)
		{
			Array.Copy(m_block, num + 1, m_block, num, length - (num + 1));
		}
		length--;
		return true;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		if (length <= 0)
		{
			value = default(TValue);
			return false;
		}
		bool exactMatch;
		int num = BinaryFindKeyIndex(key, out exactMatch);
		if (exactMatch)
		{
			value = m_block[num].Value;
			return true;
		}
		value = default(TValue);
		return false;
	}

	public TValue GetValueOrDefault(TKey key, TValue defaultValue)
	{
		bool exactMatch;
		int num = BinaryFindKeyIndex(key, out exactMatch);
		if (exactMatch)
		{
			return m_block[num].Value;
		}
		return defaultValue;
	}

	public bool SetValue(TKey key, TValue value)
	{
		bool exactMatch;
		int num = BinaryFindKeyIndex(key, out exactMatch);
		if (exactMatch)
		{
			if (m_block[num].Value.Equals(value))
			{
				return false;
			}
			m_block[num] = new KeyValuePair<TKey, TValue>(key, value);
			return true;
		}
		GuaranteeCapacity();
		if (length - num > 0)
		{
			Array.Copy(m_block, num, m_block, num + 1, length - num);
		}
		m_block[num] = new KeyValuePair<TKey, TValue>(key, value);
		length++;
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Reserve(int size)
	{
		if (Capacity <= size)
		{
			Capacity = size;
		}
	}

	private void GuaranteeCapacity()
	{
		if (Capacity <= length)
		{
			if (Capacity == 0)
			{
				Capacity = 1;
			}
			else
			{
				Capacity += 2;
			}
		}
	}

	private int BinaryFindKeyIndex(TKey key, out bool exactMatch)
	{
		if (length <= 0)
		{
			exactMatch = false;
			return 0;
		}
		int num = 0;
		int num2 = length - 1;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			int num4 = key.CompareTo(m_block[num3].Key);
			if (num4 == 0)
			{
				exactMatch = true;
				return num3;
			}
			if (num4 < 0)
			{
				num2 = num3 - 1;
			}
			else
			{
				num = num3 + 1;
			}
		}
		int num5 = key.CompareTo(m_block[num].Key);
		exactMatch = num5 == 0;
		if (num5 > 0)
		{
			return num + 1;
		}
		return num;
	}

	private bool Compare<T>(T lhs, T rhs)
	{
		return EqualityComparer<T>.Default.Equals(lhs, rhs);
	}
}

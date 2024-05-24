using System.Collections.Generic;

public class LRUCache<T>
{
	private readonly int m_capacity;

	private readonly Dictionary<T, (LinkedListNode<T> node, T value)> m_cache;

	private readonly LinkedList<T> m_list;

	public LRUCache(int capacity)
	{
		m_capacity = capacity;
		m_cache = new Dictionary<T, (LinkedListNode<T>, T)>(capacity);
		m_list = new LinkedList<T>();
	}

	public bool TryGet(T key, out T translated)
	{
		if (!m_cache.ContainsKey(key))
		{
			translated = default(T);
			return false;
		}
		(LinkedListNode<T>, T) tuple = m_cache[key];
		m_list.Remove(tuple.Item1);
		m_list.AddFirst(tuple.Item1);
		translated = tuple.Item2;
		return true;
	}

	public void Put(T key, T value)
	{
		if (m_cache.ContainsKey(key))
		{
			(LinkedListNode<T>, T) tuple = m_cache[key];
			m_list.Remove(tuple.Item1);
			m_list.AddFirst(tuple.Item1);
			m_cache[key] = (tuple.Item1, value);
			return;
		}
		if (m_cache.Count >= m_capacity)
		{
			T value2 = m_list.Last.Value;
			m_cache.Remove(value2);
			m_list.RemoveLast();
		}
		m_cache.Add(key, (m_list.AddFirst(key), value));
	}
}

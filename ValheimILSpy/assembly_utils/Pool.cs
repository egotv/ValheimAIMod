using System.Collections.Generic;

public class Pool<T> where T : new()
{
	private static readonly Stack<T> s_available = new Stack<T>();

	public static T Create()
	{
		lock (s_available)
		{
			if (s_available.Count > 0)
			{
				return s_available.Pop();
			}
			return new T();
		}
	}

	public static void Release(T obj)
	{
		if (obj == null)
		{
			return;
		}
		lock (s_available)
		{
			s_available.Push(obj);
		}
	}
}

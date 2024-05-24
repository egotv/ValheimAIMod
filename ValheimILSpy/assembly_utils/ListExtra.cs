using System.Collections.Generic;
using System.Linq;

public static class ListExtra
{
	public static void Resize<T>(this List<T> list, int sz, T c)
	{
		int count = list.Count;
		if (sz < count)
		{
			list.RemoveRange(sz, count - sz);
		}
		else if (sz > count)
		{
			if (sz > list.Capacity)
			{
				list.Capacity = sz;
			}
			list.AddRange(Enumerable.Repeat(c, sz - count));
		}
	}

	public static void Resize<T>(this List<T> list, int sz) where T : new()
	{
		list.Resize(sz, new T());
	}
}

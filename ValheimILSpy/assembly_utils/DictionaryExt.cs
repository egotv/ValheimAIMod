using System.Collections.Generic;

public static class DictionaryExt
{
	public static void Copy<TKey, TValue>(this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> other)
	{
		foreach (KeyValuePair<TKey, TValue> item in other)
		{
			target.Add(item.Key, item.Value);
		}
	}
}

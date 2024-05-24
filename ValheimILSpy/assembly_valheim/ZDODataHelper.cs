using System;
using System.Collections.Generic;
using UnityEngine;

public static class ZDODataHelper
{
	public static void WriteData<TType>(ZPackage pkg, List<KeyValuePair<int, TType>> data, Action<TType> func)
	{
		if (data.Count == 0)
		{
			return;
		}
		if (data.Count > 100)
		{
			Debug.LogWarning("Writing a lot of data; " + data.Count + " items, is not optimal. Perhaps use a byte array or two instead?");
		}
		pkg.WriteNumItems(data.Count);
		foreach (KeyValuePair<int, TType> datum in data)
		{
			pkg.Write(datum.Key);
			func(datum.Value);
		}
	}
}

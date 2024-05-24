using System;
using System.Globalization;
using UnityEngine;

public class ZLog
{
	public static void Log(object o)
	{
		Debug.Log(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + o?.ToString() + "\n");
	}

	public static void DevLog(object o)
	{
		if (Debug.isDebugBuild)
		{
			Debug.Log(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + o?.ToString() + "\n");
		}
	}

	public static void LogError(object o)
	{
		Debug.LogError(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + o?.ToString() + "\n");
	}

	public static void LogWarning(object o)
	{
		Debug.LogWarning(DateTime.Now.ToString(CultureInfo.InvariantCulture) + ": " + o?.ToString() + "\n");
	}
}

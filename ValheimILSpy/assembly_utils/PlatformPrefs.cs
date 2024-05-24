using System;
using System.Collections.Generic;
using UnityEngine;

public class PlatformPrefs
{
	public class PlatformDefaults : Dictionary<string, PlatformPrefs>
	{
		public string m_name;

		public Func<bool> m_check;

		public Dictionary<string, PlatformPrefs> m_prefs;

		public PlatformDefaults(string name, Func<bool> check, Dictionary<string, PlatformPrefs> prefs)
		{
			m_name = name;
			m_check = check;
			m_prefs = prefs;
		}
	}

	private static PlatformDefaults[] m_defaults;

	public float m_value;

	public string m_valueString;

	public bool m_saveSeperately;

	public PlatformPrefs(float val, bool saveSeperately = true)
	{
		m_value = val;
		m_saveSeperately = saveSeperately;
	}

	public PlatformPrefs(int val, bool saveSeperately = true)
	{
		m_value = val;
		m_saveSeperately = saveSeperately;
	}

	public PlatformPrefs(string val, bool saveSeperately = true)
	{
		m_valueString = val;
		m_saveSeperately = saveSeperately;
	}

	public static implicit operator PlatformPrefs(float val)
	{
		return new PlatformPrefs(val);
	}

	public static implicit operator PlatformPrefs(int val)
	{
		return new PlatformPrefs(val);
	}

	public static implicit operator PlatformPrefs(string val)
	{
		return new PlatformPrefs(val);
	}

	public static float GetFloat(string name, float defaultValue = 0f)
	{
		if (CheckPlatformPref(ref name, out var pref))
		{
			defaultValue = pref.m_value;
		}
		return PlayerPrefs.GetFloat(name, defaultValue);
	}

	public static void SetFloat(string name, float value)
	{
		CheckPlatformPref(ref name, out var _);
		PlayerPrefs.SetFloat(name, value);
	}

	public static int GetInt(string name, int defaultValue = 0)
	{
		if (CheckPlatformPref(ref name, out var pref))
		{
			defaultValue = (int)pref.m_value;
		}
		return PlayerPrefs.GetInt(name, defaultValue);
	}

	public static void SetInt(string name, int value)
	{
		CheckPlatformPref(ref name, out var _);
		PlayerPrefs.SetInt(name, value);
	}

	public static string GetString(string name, string defaultValue = "")
	{
		if (CheckPlatformPref(ref name, out var pref))
		{
			defaultValue = pref.m_valueString;
		}
		return PlayerPrefs.GetString(name, defaultValue);
	}

	public static void SetString(string name, string value)
	{
		CheckPlatformPref(ref name, out var _);
		PlayerPrefs.SetString(name, value);
	}

	private static bool CheckPlatformPref(ref string name, out PlatformPrefs pref)
	{
		if (m_defaults == null)
		{
			ZLog.LogWarning("Fetching PlatformPrefs '" + name + "' before loading defaults");
		}
		else
		{
			PlatformDefaults[] defaults = m_defaults;
			foreach (PlatformDefaults platformDefaults in defaults)
			{
				if (platformDefaults.m_check() && platformDefaults.m_prefs.TryGetValue(name, out pref))
				{
					if (pref.m_saveSeperately)
					{
						name = platformDefaults.m_name + name;
					}
					return true;
				}
			}
		}
		pref = null;
		return false;
	}

	public static void SetDefaults(params PlatformDefaults[] defaults)
	{
		m_defaults = defaults;
	}
}

using UnityEngine;

internal class Version
{
	public const uint m_networkVersion = 27u;

	public const int m_playerVersion = 39;

	public const int m_oldestForwardCompatiblePlayerVersion = 27;

	public const int m_worldVersion = 34;

	public const int m_oldestForwardCompatibleWorldVersion = 9;

	public const int c_WorldVersionNewSaveFormat = 31;

	public const int c_WorldVersionGlobalKeys = 32;

	public const int c_WorldVersionNumItems = 33;

	public const int m_worldGenVersion = 2;

	public const int m_itemDataVersion = 106;

	public const int m_playerDataVersion = 27;

	public static readonly GameVersion FirstVersionWithNetworkVersion = new GameVersion(0, 214, 301);

	public static readonly GameVersion FirstVersionWithPlatformRestriction = new GameVersion(0, 213, 3);

	public static readonly GameVersion FirstVersionWithModifiers = new GameVersion(0, 217, 8);

	public static GameVersion CurrentVersion { get; } = new GameVersion(0, 218, 15);


	public static string GetVersionString(bool includeMercurialHash = false)
	{
		string text = CurrentVersion.ToString();
		string platformPrefix = GetPlatformPrefix();
		if (platformPrefix.Length > 0)
		{
			text = $"{platformPrefix}-{CurrentVersion}";
		}
		if (includeMercurialHash)
		{
			TextAsset textAsset = Resources.Load<TextAsset>("clientVersion");
			if (textAsset != null)
			{
				text = text + "\n" + textAsset.text;
			}
		}
		return text;
	}

	public static bool IsWorldVersionCompatible(int version)
	{
		if (version <= 34)
		{
			return version >= 9;
		}
		return false;
	}

	public static bool IsPlayerVersionCompatible(int version)
	{
		if (version <= 39)
		{
			return version >= 27;
		}
		return false;
	}

	public static Platforms GetPlatform()
	{
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			return Platforms.SteamDeckProton;
		}
		return Platforms.SteamWindows;
	}

	public static string GetPlatformPrefix(string Default = "")
	{
		return GetPlatform() switch
		{
			Platforms.SteamLinux => "l", 
			Platforms.SteamDeckProton => "dw", 
			Platforms.SteamDeckNative => "dl", 
			Platforms.MicrosoftStore => "ms", 
			_ => Default, 
		};
	}
}

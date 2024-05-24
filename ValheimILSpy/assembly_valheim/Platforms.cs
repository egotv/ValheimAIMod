using System;

[Flags]
public enum Platforms
{
	None = 0,
	SteamWindows = 1,
	SteamLinux = 2,
	SteamDeckProton = 4,
	SteamDeckNative = 8,
	MicrosoftStore = 0x10,
	Xbox = 0x20,
	All = 0x3F
}

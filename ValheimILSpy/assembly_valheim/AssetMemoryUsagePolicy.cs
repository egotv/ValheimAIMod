using System;

[Flags]
public enum AssetMemoryUsagePolicy
{
	KeepAllLoaded = 7,
	KeepSynchronousOnlyLoaded = 3,
	KeepNoneLoaded = 1,
	KeepSynchronousLoadedBit = 2,
	KeepAsynchronousLoadedBit = 4
}

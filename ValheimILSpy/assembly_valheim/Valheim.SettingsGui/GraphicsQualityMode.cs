using UnityEngine;

namespace Valheim.SettingsGui;

public enum GraphicsQualityMode
{
	Performance = 0,
	Balanced = 1,
	Quality = 2,
	Constrained = 3,
	VeryLow = 4,
	[InspectorName("VeryLow")]
	Custom = 100
}

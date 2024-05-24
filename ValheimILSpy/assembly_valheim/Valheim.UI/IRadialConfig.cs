using UnityEngine;

namespace Valheim.UI;

public interface IRadialConfig
{
	string LocalizedName { get; }

	Sprite Sprite { get; }

	void SetRadial(DynamicRadialMenu radial, int page);
}

using UnityEngine;

public interface IProjectile
{
	void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo);

	string GetTooltipString(int itemQuality);
}

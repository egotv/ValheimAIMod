using UnityEngine;

public class TriggerSpawnAbility : MonoBehaviour, IProjectile
{
	[Header("Spawn")]
	public float m_range = 10f;

	private Character m_owner;

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		m_owner = owner;
		TriggerSpawner.TriggerAllInRange(base.transform.position, m_range);
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}
}

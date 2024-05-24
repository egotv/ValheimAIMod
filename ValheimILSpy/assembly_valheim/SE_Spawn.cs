using UnityEngine;

public class SE_Spawn : StatusEffect
{
	[Header("__SE_Spawn__")]
	public float m_delay = 10f;

	public GameObject m_prefab;

	public Vector3 m_spawnOffset = new Vector3(0f, 0f, 0f);

	public EffectList m_spawnEffect = new EffectList();

	private bool m_spawned;

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!m_spawned && m_time > m_delay)
		{
			m_spawned = true;
			Vector3 position = m_character.transform.TransformVector(m_spawnOffset);
			GameObject gameObject = Object.Instantiate(m_prefab, position, Quaternion.identity);
			Projectile component = gameObject.GetComponent<Projectile>();
			if ((bool)component)
			{
				component.Setup(m_character, Vector3.zero, -1f, null, null, null);
			}
			m_spawnEffect.Create(gameObject.transform.position, gameObject.transform.rotation);
		}
	}
}

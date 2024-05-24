using UnityEngine;

public class EggHatch : MonoBehaviour
{
	public float m_triggerDistance = 5f;

	[Range(0f, 1f)]
	public float m_chanceToHatch = 1f;

	public Vector3 m_spawnOffset = new Vector3(0f, 0.5f, 0f);

	public GameObject m_spawnPrefab;

	public EffectList m_hatchEffect;

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		if (Random.value <= m_chanceToHatch)
		{
			InvokeRepeating("CheckSpawn", Random.Range(1f, 2f), 1f);
		}
	}

	private void CheckSpawn()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_triggerDistance);
			if ((bool)closestPlayer && !closestPlayer.InGhostMode())
			{
				Hatch();
			}
		}
	}

	private void Hatch()
	{
		m_hatchEffect.Create(base.transform.position, base.transform.rotation);
		Object.Instantiate(m_spawnPrefab, base.transform.TransformPoint(m_spawnOffset), Quaternion.Euler(0f, Random.Range(0, 360), 0f));
		m_nview.Destroy();
	}
}

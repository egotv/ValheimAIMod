using UnityEngine;

public class Corpse : MonoBehaviour
{
	private static readonly float m_updateDt = 2f;

	public float m_emptyDespawnDelaySec = 10f;

	private float m_emptyTimer;

	private Container m_container;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_container = GetComponent<Container>();
		if (m_nview.IsOwner() && m_nview.GetZDO().GetLong(ZDOVars.s_timeOfDeath, 0L) == 0L)
		{
			m_nview.GetZDO().Set(ZDOVars.s_timeOfDeath, ZNet.instance.GetTime().Ticks);
		}
		InvokeRepeating("UpdateDespawn", m_updateDt, m_updateDt);
	}

	private void UpdateDespawn()
	{
		if (!m_nview.IsOwner() || m_container.IsInUse())
		{
			return;
		}
		if (m_container.GetInventory().NrOfItems() <= 0)
		{
			m_emptyTimer += m_updateDt;
			if (m_emptyTimer >= m_emptyDespawnDelaySec)
			{
				ZLog.Log("Despawning looted corpse");
				m_nview.Destroy();
			}
		}
		else
		{
			m_emptyTimer = 0f;
		}
	}
}

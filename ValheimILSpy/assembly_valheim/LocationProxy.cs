using UnityEngine;

public class LocationProxy : MonoBehaviour
{
	private bool m_locationNeedsSpawn;

	private ZDO m_zdoSetToBeLoadingInZone;

	private GameObject m_instance;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		SpawnLocation();
	}

	private void Update()
	{
		if (m_locationNeedsSpawn)
		{
			SpawnLocation();
		}
	}

	private void OnDestroy()
	{
		if (m_zdoSetToBeLoadingInZone != null)
		{
			ZoneSystem.instance.UnsetLoadingInZone(m_zdoSetToBeLoadingInZone);
			m_zdoSetToBeLoadingInZone = null;
		}
	}

	public void SetLocation(string location, int seed, bool spawnNow)
	{
		int stableHashCode = location.GetStableHashCode();
		m_nview.GetZDO().Set(ZDOVars.s_location, stableHashCode);
		m_nview.GetZDO().Set(ZDOVars.s_seed, seed);
		if (spawnNow)
		{
			SpawnLocation();
		}
	}

	private bool SpawnLocation()
	{
		int @int = m_nview.GetZDO().GetInt(ZDOVars.s_location);
		int int2 = m_nview.GetZDO().GetInt(ZDOVars.s_seed);
		if (@int == 0)
		{
			return false;
		}
		if (ZoneSystem.instance.ShouldDelayProxyLocationSpawning(@int))
		{
			m_locationNeedsSpawn = true;
			if (m_zdoSetToBeLoadingInZone == null)
			{
				m_zdoSetToBeLoadingInZone = m_nview.GetZDO();
				ZoneSystem.instance.SetLoadingInZone(m_zdoSetToBeLoadingInZone);
			}
			return false;
		}
		m_instance = ZoneSystem.instance.SpawnProxyLocation(@int, int2, base.transform.position, base.transform.rotation);
		if (m_instance == null)
		{
			return false;
		}
		m_instance.transform.SetParent(base.transform, worldPositionStays: true);
		m_nview.LoadFields();
		m_locationNeedsSpawn = false;
		if (m_zdoSetToBeLoadingInZone != null)
		{
			ZoneSystem.instance.UnsetLoadingInZone(m_zdoSetToBeLoadingInZone);
			m_zdoSetToBeLoadingInZone = null;
		}
		return true;
	}
}

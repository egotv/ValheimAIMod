using System;
using UnityEngine;

public class CinderSpawner : MonoBehaviour
{
	public GameObject m_cinderPrefab;

	public float m_cinderInterval = 2f;

	public float m_cinderChance = 0.1f;

	public float m_cinderVel = 5f;

	public float m_spawnOffset = 1f;

	public Vector3 m_spawnOffsetPoint;

	public int m_spread = 4;

	public int m_instancesPerSpawn = 1;

	public bool m_spawnOnAwake;

	public bool m_spawnOnProjectileHit;

	private ZNetView m_nview;

	private Heightmap.Biome m_biome;

	private GameObject m_attachObj;

	private bool m_hasAttachObj;

	private Fireplace m_fireplace;

	private void Awake()
	{
		m_nview = GetComponentInParent<ZNetView>();
		if (m_cinderInterval > 0f)
		{
			InvokeRepeating("UpdateSpawnCinder", m_cinderInterval, m_cinderInterval);
		}
		if (m_spawnOnAwake)
		{
			SpawnCinder();
		}
		if (m_spawnOnProjectileHit)
		{
			Projectile component = GetComponent<Projectile>();
			if ((object)component != null)
			{
				component.m_onHit = (OnProjectileHit)Delegate.Combine(component.m_onHit, (OnProjectileHit)delegate
				{
					SpawnCinder();
				});
			}
		}
		m_fireplace = GetComponent<Fireplace>();
	}

	private void FixedUpdate()
	{
		if (m_hasAttachObj && !m_attachObj)
		{
			DestroyNow();
		}
	}

	public void Setup(int spread, GameObject attachObj)
	{
		m_nview.GetZDO().Set(ZDOVars.s_spread, spread);
		m_hasAttachObj = attachObj != null;
		m_attachObj = attachObj;
	}

	private int GetSpread()
	{
		return m_nview.GetZDO().GetInt(ZDOVars.s_spread, m_spread);
	}

	private void UpdateSpawnCinder()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && (!m_fireplace || m_fireplace.IsBurning()) && CanSpawnCinder() && GetSpread() > 0 && !(UnityEngine.Random.value > m_cinderChance))
		{
			SpawnCinder();
		}
	}

	public void SpawnCinder()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && CanSpawnCinder() && !ShieldGenerator.IsInsideShield(base.transform.position))
		{
			for (int i = 0; i < m_instancesPerSpawn; i++)
			{
				Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
				insideUnitSphere.y = Mathf.Abs(insideUnitSphere.y * 2f);
				insideUnitSphere.Normalize();
				UnityEngine.Object.Instantiate(m_cinderPrefab, base.transform.position + insideUnitSphere * m_spawnOffset, Quaternion.identity).GetComponent<Cinder>().Setup(insideUnitSphere * m_cinderVel, GetSpread() - 1);
			}
		}
	}

	public bool CanSpawnCinder()
	{
		return CanSpawnCinder(base.transform, ref m_biome);
	}

	public static bool CanSpawnCinder(Transform transform, ref Heightmap.Biome biome)
	{
		if (biome == Heightmap.Biome.None)
		{
			Vector3 p = transform.position;
			ZoneSystem.instance.GetGroundData(ref p, out var _, out var _, out var _, out var hmap);
			if (hmap != null)
			{
				biome = hmap.GetBiome(transform.position);
			}
		}
		if (biome != Heightmap.Biome.AshLands)
		{
			return ZoneSystem.instance.GetGlobalKey(GlobalKeys.Fire);
		}
		return true;
	}

	private void DestroyNow()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.Destroy();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(base.transform.position + m_spawnOffsetPoint, 0.05f);
	}
}

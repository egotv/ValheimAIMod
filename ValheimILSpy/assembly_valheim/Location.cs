using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Location : MonoBehaviour
{
	[FormerlySerializedAs("m_radius")]
	public float m_exteriorRadius = 20f;

	public bool m_noBuild = true;

	public float m_noBuildRadiusOverride;

	public bool m_clearArea = true;

	public string m_discoverLabel = "";

	[Header("Other")]
	public bool m_applyRandomDamage;

	[Header("Interior")]
	public bool m_hasInterior;

	public float m_interiorRadius = 20f;

	public string m_interiorEnvironment = "";

	public Transform m_interiorTransform;

	[Tooltip("Makes the dungeon entrance start at the given interior transform (including rotation) rather than straight above the entrance, which gives the dungeon much more room to fill out the entire zone. Must use together with DungeonGenerator.m_useCustomInteriorTransform to make sure seeds are deterministic.")]
	public bool m_useCustomInteriorTransform;

	public DungeonGenerator m_generator;

	public GameObject m_interiorPrefab;

	[Header("Spawners")]
	public int m_enemyMinLevelOverride = -1;

	public int m_enemyMaxLevelOverride = -1;

	public float m_enemyLevelUpOverride = -1f;

	[Tooltip("Exludes CreatureSpawners of specified groups for level up override values above.")]
	public List<int> m_excludeEnemyLevelOverrideGroups = new List<int>();

	[Tooltip("Blocks any CreatureSpawner that is set to given SpawnGroups of these IDs.")]
	public List<int> m_blockSpawnGroups = new List<int>();

	private static List<Location> m_allLocations = new List<Location>();

	public Heightmap.Biome m_biome;

	private void Awake()
	{
		m_allLocations.Add(this);
		if (m_hasInterior)
		{
			Vector3 zoneCenter = GetZoneCenter();
			GameObject obj = Object.Instantiate(position: new Vector3(zoneCenter.x, base.transform.position.y + 5000f, zoneCenter.z), original: m_interiorPrefab, rotation: Quaternion.identity, parent: base.transform);
			obj.transform.localScale = new Vector3(ZoneSystem.instance.m_zoneSize, 500f, ZoneSystem.instance.m_zoneSize);
			obj.GetComponent<EnvZone>().m_environment = m_interiorEnvironment;
		}
	}

	private Vector3 GetZoneCenter()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
		return ZoneSystem.instance.GetZonePos(zone);
	}

	private void OnDestroy()
	{
		m_allLocations.Remove(this);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, -0.01f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
		Gizmos.DrawSphere(Vector3.zero, m_exteriorRadius);
		Utils.DrawGizmoCircle(base.transform.position, m_noBuildRadiusOverride, 32);
		Gizmos.matrix = Matrix4x4.identity;
		Utils.DrawGizmoCircle(base.transform.position, m_exteriorRadius, 32);
		if (m_hasInterior)
		{
			Utils.DrawGizmoCircle(base.transform.position + new Vector3(0f, 5000f, 0f), m_interiorRadius, 32);
			Utils.DrawGizmoCircle(base.transform.position, m_interiorRadius, 32);
			Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, 5000f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
			Gizmos.DrawSphere(Vector3.zero, m_interiorRadius);
			Gizmos.matrix = Matrix4x4.identity;
		}
	}

	public float GetMaxRadius()
	{
		if (!m_hasInterior)
		{
			return m_exteriorRadius;
		}
		return Mathf.Max(m_exteriorRadius, m_interiorRadius);
	}

	public bool IsInside(Vector3 point, float radius, bool buildCheck = false)
	{
		float num = ((buildCheck && m_noBuildRadiusOverride > 0f) ? m_noBuildRadiusOverride : GetMaxRadius());
		return Utils.DistanceXZ(base.transform.position, point) < num + radius;
	}

	public static bool IsInsideLocation(Vector3 point, float distance)
	{
		foreach (Location allLocation in m_allLocations)
		{
			if (allLocation.IsInside(point, distance))
			{
				return true;
			}
		}
		return false;
	}

	public static Location GetLocation(Vector3 point, bool checkDungeons = true)
	{
		if (Character.InInterior(point))
		{
			return GetZoneLocation(point);
		}
		foreach (Location allLocation in m_allLocations)
		{
			if (allLocation.IsInside(point, 0f))
			{
				return allLocation;
			}
		}
		return null;
	}

	public static Location GetZoneLocation(Vector2i zone)
	{
		foreach (Location allLocation in m_allLocations)
		{
			if (zone == ZoneSystem.instance.GetZone(allLocation.transform.position))
			{
				return allLocation;
			}
		}
		return null;
	}

	public static Location GetZoneLocation(Vector3 point)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(point);
		foreach (Location allLocation in m_allLocations)
		{
			if (zone == ZoneSystem.instance.GetZone(allLocation.transform.position))
			{
				return allLocation;
			}
		}
		return null;
	}

	public static bool IsInsideNoBuildLocation(Vector3 point)
	{
		foreach (Location allLocation in m_allLocations)
		{
			if (allLocation.m_noBuild && allLocation.IsInside(point, 0f, buildCheck: true))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsInsideActiveBossDungeon(Vector3 point)
	{
		if (EnemyHud.instance != null)
		{
			Character activeBoss = EnemyHud.instance.GetActiveBoss();
			if ((object)activeBoss != null && activeBoss.m_bossEvent.Length > 0)
			{
				Vector2i zone = ZoneSystem.instance.GetZone(point);
				Vector2i zone2 = ZoneSystem.instance.GetZone(activeBoss.transform.position);
				if (zone == zone2)
				{
					return true;
				}
			}
		}
		return false;
	}
}

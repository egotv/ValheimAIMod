using System;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
	public class Group : HashSet<CreatureSpawner>
	{
		private readonly List<CreatureSpawner> m_activeSpawners = new List<CreatureSpawner>();

		public void CountSpawns(out int spawnedNow, out int spawnedEver)
		{
			spawnedNow = (spawnedEver = 0);
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				CreatureSpawner current = enumerator.Current;
				if (current.m_nview == null || current.m_nview.GetZDO() == null)
				{
					continue;
				}
				current.m_lastSpawnID = current.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
				if (current.m_nview.GetZDO().GetConnectionType() != 0)
				{
					spawnedEver++;
					current.m_lastSpawnID = current.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
					if (ZDOMan.instance.GetZDO(current.m_lastSpawnID) != null)
					{
						spawnedNow++;
					}
				}
			}
		}

		public void SpawnWeighted()
		{
			m_activeSpawners.Clear();
			float num = 0f;
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CreatureSpawner current = enumerator.Current;
					if (current.m_lastSpawnID.IsNone() || (current.CanRespawnNow(current.m_lastSpawnID) && !current.SpawnedCreatureStillExists(current.m_lastSpawnID)))
					{
						m_activeSpawners.Add(current);
						num += current.m_spawnerWeight;
					}
				}
			}
			float num2 = UnityEngine.Random.Range(0f, num);
			float num3 = 0f;
			foreach (CreatureSpawner activeSpawner in m_activeSpawners)
			{
				num3 += activeSpawner.m_spawnerWeight;
				if (num2 < num3)
				{
					activeSpawner.Spawn();
					return;
				}
			}
			ZLog.LogError("No active spawners for group but something is still calling it!");
		}
	}

	private const float m_radius = 0.75f;

	public GameObject m_creaturePrefab;

	[Header("Level")]
	public int m_maxLevel = 1;

	public int m_minLevel = 1;

	public float m_levelupChance = 10f;

	[Header("Spawn settings")]
	public float m_respawnTimeMinuts = 20f;

	public float m_triggerDistance = 60f;

	public float m_triggerNoise;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_requireSpawnArea;

	public bool m_spawnInPlayerBase;

	public bool m_wakeUpAnimation;

	public int m_spawnInterval = 5;

	public string m_requiredGlobalKey = "";

	public string m_blockingGlobalKey = "";

	public bool m_setPatrolSpawnPoint;

	[Header("Spawn group blocking")]
	[Tooltip("Spawners sharing the same ID within eachothers radiuses will be grouped together, and will never spawn more than the specified max group size. Weight will also be taken into account, prioritizing those with higher weight randomly.")]
	public int m_spawnGroupID;

	public int m_maxGroupSpawned = 1;

	public float m_spawnGroupRadius;

	public float m_spawnerWeight = 1f;

	[Space]
	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;

	private Group m_spawnGroup;

	private ZDOID m_lastSpawnID;

	private bool m_checkedLocation;

	private Location m_location;

	private static List<CreatureSpawner> m_creatureSpawners = new List<CreatureSpawner>();

	private static List<CreatureSpawner> m_groupUnchecked = new List<CreatureSpawner>();

	private static HashSet<CreatureSpawner> m_grouped = new HashSet<CreatureSpawner>();

	private static Stack<CreatureSpawner> m_groupNew = new Stack<CreatureSpawner>();

	private static List<Group> m_groups = new List<Group>();

	private void Awake()
	{
		m_creatureSpawners.Add(this);
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			InvokeRepeating("UpdateSpawner", UnityEngine.Random.Range(m_spawnInterval / 2, m_spawnInterval), m_spawnInterval);
		}
	}

	private void OnDestroy()
	{
		m_creatureSpawners.Remove(this);
	}

	private void UpdateSpawner()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (!m_checkedLocation)
		{
			m_location = Location.GetLocation(base.transform.position);
			m_checkedLocation = true;
			if ((bool)m_location && m_location.m_blockSpawnGroups.Contains(m_spawnGroupID))
			{
				m_nview.Destroy();
				return;
			}
		}
		ZDOConnection connection = m_nview.GetZDO().GetConnection();
		bool flag = connection != null && connection.m_type == ZDOExtraData.ConnectionType.Spawned;
		if (m_respawnTimeMinuts <= 0f && flag)
		{
			return;
		}
		ZDOID connectionZDOID = m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned);
		if (SpawnedCreatureStillExists(connectionZDOID) || !CanRespawnNow(connectionZDOID) || (!m_spawnAtDay && EnvMan.IsDay()) || (!m_spawnAtNight && EnvMan.IsNight()))
		{
			return;
		}
		_ = m_requireSpawnArea;
		if (!m_spawnInPlayerBase && (bool)EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase))
		{
			return;
		}
		if (m_triggerNoise > 0f)
		{
			if (!Player.IsPlayerInRange(base.transform.position, m_triggerDistance, m_triggerNoise))
			{
				return;
			}
		}
		else if (!Player.IsPlayerInRange(base.transform.position, m_triggerDistance))
		{
			return;
		}
		if (!CheckGlobalKeys() && !CheckGroupSpawnBlocked())
		{
			if (m_spawnGroup != null)
			{
				m_spawnGroup.SpawnWeighted();
			}
			else
			{
				Spawn();
			}
		}
	}

	private bool CheckGlobalKeys()
	{
		List<string> globalKeys = ZoneSystem.instance.GetGlobalKeys();
		if (m_blockingGlobalKey != "" && globalKeys.Contains(m_blockingGlobalKey))
		{
			return true;
		}
		if (m_requiredGlobalKey != "" && !globalKeys.Contains(m_requiredGlobalKey))
		{
			return true;
		}
		return false;
	}

	private bool SpawnedCreatureStillExists(ZDOID spawnID)
	{
		if (!spawnID.IsNone() && ZDOMan.instance.GetZDO(spawnID) != null)
		{
			m_nview.GetZDO().Set(ZDOVars.s_aliveTime, ZNet.instance.GetTime().Ticks);
			return true;
		}
		return false;
	}

	private bool CanRespawnNow(ZDOID spawnID)
	{
		if (m_respawnTimeMinuts > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_aliveTime, 0L));
			if ((time - dateTime).TotalMinutes < (double)m_respawnTimeMinuts)
			{
				return false;
			}
		}
		return true;
	}

	private bool HasSpawned()
	{
		if (m_nview == null || m_nview.GetZDO() == null)
		{
			return false;
		}
		ZDOConnection connection = m_nview.GetZDO().GetConnection();
		if (connection != null)
		{
			return connection.m_type == ZDOExtraData.ConnectionType.Spawned;
		}
		return false;
	}

	private ZNetView Spawn()
	{
		Vector3 position = base.transform.position;
		if (ZoneSystem.instance.FindFloor(position, out var height))
		{
			position.y = height;
		}
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate(m_creaturePrefab, position, rotation);
		ZNetView component = gameObject.GetComponent<ZNetView>();
		if (m_wakeUpAnimation)
		{
			gameObject.GetComponent<ZSyncAnimation>()?.SetBool("wakeup", value: true);
		}
		BaseAI component2 = gameObject.GetComponent<BaseAI>();
		if (component2 != null && m_setPatrolSpawnPoint)
		{
			component2.SetPatrolPoint();
		}
		int num = m_minLevel;
		int num2 = m_maxLevel;
		float num3 = m_levelupChance;
		if (m_location != null && !m_location.m_excludeEnemyLevelOverrideGroups.Contains(m_spawnGroupID))
		{
			if (m_location.m_enemyMinLevelOverride >= 0)
			{
				num = m_location.m_enemyMinLevelOverride;
			}
			if (m_location.m_enemyMaxLevelOverride >= 0)
			{
				num2 = m_location.m_enemyMaxLevelOverride;
			}
			if (m_location.m_enemyLevelUpOverride >= 0f)
			{
				num3 = m_location.m_enemyLevelUpOverride;
			}
		}
		if (num2 > 1)
		{
			Character component3 = gameObject.GetComponent<Character>();
			if ((bool)component3)
			{
				int i;
				for (i = num; i < num2; i++)
				{
					if (!(UnityEngine.Random.Range(0f, 100f) <= SpawnSystem.GetLevelUpChance(num3)))
					{
						break;
					}
				}
				if (i > 1)
				{
					component3.SetLevel(i);
				}
			}
			else
			{
				ItemDrop component4 = gameObject.GetComponent<ItemDrop>();
				if ((bool)component4)
				{
					int j;
					for (j = num; j < num2; j++)
					{
						if (!(UnityEngine.Random.Range(0f, 100f) <= num3))
						{
							break;
						}
					}
					if (j > 1)
					{
						component4.SetQuality(j);
					}
				}
			}
		}
		m_nview.GetZDO().SetConnection(ZDOExtraData.ConnectionType.Spawned, component.GetZDO().m_uid);
		m_nview.GetZDO().Set(ZDOVars.s_aliveTime, ZNet.instance.GetTime().Ticks);
		SpawnEffect(gameObject);
		return component;
	}

	private void SpawnEffect(GameObject spawnedObject)
	{
		Character component = spawnedObject.GetComponent<Character>();
		Vector3 basePos = (component ? component.GetCenterPoint() : (base.transform.position + Vector3.up * 0.75f));
		m_spawnEffects.Create(basePos, Quaternion.identity);
	}

	private float GetRadius()
	{
		return 0.75f;
	}

	private void OnDrawGizmos()
	{
	}

	private bool CheckGroupSpawnBlocked()
	{
		if (m_spawnGroupRadius <= 0f || m_maxGroupSpawned < 1)
		{
			return false;
		}
		if (m_spawnGroup == null)
		{
			m_groupNew.Clear();
			m_grouped.Clear();
			m_groupUnchecked.Clear();
			m_groupUnchecked.AddRange(m_creatureSpawners);
			m_groupUnchecked.Remove(this);
			m_groupNew.Push(this);
			m_grouped.Add(this);
			while (m_groupNew.Count > 0)
			{
				CreatureSpawner creatureSpawner = m_groupNew.Pop();
				for (int num = m_groupUnchecked.Count - 1; num >= 0; num--)
				{
					CreatureSpawner creatureSpawner2 = m_groupUnchecked[num];
					if (creatureSpawner.m_spawnGroupID == creatureSpawner2.m_spawnGroupID && Vector3.Distance(creatureSpawner.transform.position, creatureSpawner2.transform.position) <= creatureSpawner.m_spawnGroupRadius + creatureSpawner2.m_spawnGroupRadius)
					{
						m_groupNew.Push(creatureSpawner2);
						m_grouped.Add(creatureSpawner2);
						m_groupUnchecked.Remove(creatureSpawner2);
					}
				}
			}
			m_groups.Clear();
			foreach (CreatureSpawner item in m_grouped)
			{
				if (item.m_spawnGroup != null)
				{
					m_groups.Add(item.m_spawnGroup);
				}
			}
			Group group = null;
			if (m_groups.Count > 0)
			{
				if (m_groups.Count > 1)
				{
					ZLog.Log(string.Format("{0} {1} merged for {2} spawners.", m_groups.Count, "Group", m_grouped.Count));
				}
				group = m_groups[0];
			}
			else
			{
				group = new Group();
			}
			foreach (CreatureSpawner item2 in m_grouped)
			{
				group.Add(item2);
				item2.m_spawnGroup = group;
			}
			ZLog.Log(string.Format("{0} created for {1} spawners.", "Group", group.Count));
		}
		m_spawnGroup.CountSpawns(out var spawnedNow, out var spawnedEver);
		if (m_respawnTimeMinuts <= 0f)
		{
			if (spawnedEver < m_maxGroupSpawned && spawnedNow < m_maxGroupSpawned)
			{
				return false;
			}
			Terminal.Log($"Group spawnID #{m_spawnGroupID} blocked: I have not spawned, but someone else has made us reach the maximum, abort!");
			return true;
		}
		if (spawnedNow < m_maxGroupSpawned)
		{
			return false;
		}
		Terminal.Log($"Group spawnID #{m_spawnGroupID} blocked: I allow respawning, but we are currently at our maxium, abort!");
		return true;
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSpawner : MonoBehaviour
{
	private const float m_radius = 0.75f;

	public GameObject[] m_creaturePrefabs;

	[Header("Level")]
	public int m_maxLevel = 1;

	public int m_minLevel = 1;

	public float m_levelupChance = 10f;

	[Header("Spawn settings")]
	[Range(0f, 100f)]
	public float m_spawnChance = 100f;

	public float m_minSpawnInterval = 10f;

	public int m_maxSpawned = 10;

	public float m_maxExtraPerPlayer;

	public float m_maxSpawnedRange = 30f;

	public bool m_setHuntPlayer;

	public bool m_setPatrolSpawnPoint;

	public bool m_useSpawnerRotation;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;

	private static List<TriggerSpawner> m_allSpawners = new List<TriggerSpawner>();

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_nview.Register("Trigger", RPC_Trigger);
		m_allSpawners.Add(this);
	}

	private void OnDestroy()
	{
		m_allSpawners.Remove(this);
	}

	public static void TriggerAllInRange(Vector3 p, float range)
	{
		ZLog.Log("Trigging spawners in range");
		foreach (TriggerSpawner allSpawner in m_allSpawners)
		{
			if (Vector3.Distance(allSpawner.transform.position, p) < range)
			{
				allSpawner.Trigger();
			}
		}
	}

	private void Trigger()
	{
		m_nview.InvokeRPC("Trigger");
	}

	private void RPC_Trigger(long sender)
	{
		ZLog.Log("Trigging " + base.gameObject.name);
		TrySpawning();
	}

	private void TrySpawning()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (m_minSpawnInterval > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
			TimeSpan timeSpan = time - dateTime;
			if (timeSpan.TotalMinutes < (double)m_minSpawnInterval)
			{
				TimeSpan timeSpan2 = timeSpan;
				ZLog.Log("Not enough time passed " + timeSpan2.ToString());
				return;
			}
		}
		if (UnityEngine.Random.Range(0f, 100f) > m_spawnChance)
		{
			ZLog.Log("Spawn chance fail " + m_spawnChance);
		}
		else
		{
			Spawn();
		}
	}

	private bool Spawn()
	{
		Vector3 position = base.transform.position;
		if (ZoneSystem.instance.FindFloor(position, out var height))
		{
			position.y = height;
		}
		GameObject gameObject = m_creaturePrefabs[UnityEngine.Random.Range(0, m_creaturePrefabs.Length)];
		int num = m_maxSpawned + (int)(m_maxExtraPerPlayer * (float)Game.instance.GetPlayerDifficulty(base.transform.position));
		if (num > 0 && SpawnSystem.GetNrOfInstances(gameObject, base.transform.position, m_maxSpawnedRange) >= num)
		{
			return false;
		}
		Quaternion rotation = (m_useSpawnerRotation ? base.transform.rotation : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, position, rotation);
		gameObject2.GetComponent<ZNetView>();
		BaseAI component = gameObject2.GetComponent<BaseAI>();
		if (component != null)
		{
			if (m_setPatrolSpawnPoint)
			{
				component.SetPatrolPoint();
			}
			if (m_setHuntPlayer)
			{
				component.SetHuntPlayer(hunt: true);
			}
		}
		if (m_maxLevel > 1)
		{
			Character component2 = gameObject2.GetComponent<Character>();
			if ((bool)component2)
			{
				int i;
				for (i = m_minLevel; i < m_maxLevel; i++)
				{
					if (!(UnityEngine.Random.Range(0f, 100f) <= m_levelupChance))
					{
						break;
					}
				}
				if (i > 1)
				{
					component2.SetLevel(i);
				}
			}
		}
		m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		m_spawnEffects.Create(base.transform.position, base.transform.rotation);
		return true;
	}

	private float GetRadius()
	{
		return 0.75f;
	}

	private void OnDrawGizmos()
	{
	}
}

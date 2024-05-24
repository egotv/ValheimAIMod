using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
	[Serializable]
	public class SpawnData
	{
		public GameObject m_prefab;

		public float m_weight;

		[Header("Level")]
		public int m_maxLevel = 1;

		public int m_minLevel = 1;
	}

	private const float dt = 2f;

	public List<SpawnData> m_prefabs = new List<SpawnData>();

	public float m_levelupChance = 15f;

	public float m_spawnIntervalSec = 30f;

	public float m_triggerDistance = 256f;

	public bool m_setPatrolSpawnPoint = true;

	public float m_spawnRadius = 2f;

	public float m_nearRadius = 10f;

	public float m_farRadius = 1000f;

	public int m_maxNear = 3;

	public int m_maxTotal = 20;

	public bool m_onGroundOnly;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;

	private float m_spawnTimer;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		InvokeRepeating("UpdateSpawn", 2f, 2f);
	}

	private void UpdateSpawn()
	{
		if (m_nview.IsOwner() && !ZNetScene.instance.OutsideActiveArea(base.transform.position) && Player.IsPlayerInRange(base.transform.position, m_triggerDistance))
		{
			m_spawnTimer += 2f;
			if (m_spawnTimer > m_spawnIntervalSec)
			{
				m_spawnTimer = 0f;
				SpawnOne();
			}
		}
	}

	private bool SpawnOne()
	{
		GetInstances(out var near, out var total);
		if (near >= m_maxNear || total >= m_maxTotal)
		{
			return false;
		}
		SpawnData spawnData = SelectWeightedPrefab();
		if (spawnData == null)
		{
			return false;
		}
		if (!FindSpawnPoint(spawnData.m_prefab, out var point))
		{
			return false;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(spawnData.m_prefab, point, Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f));
		if (m_setPatrolSpawnPoint)
		{
			BaseAI component = gameObject.GetComponent<BaseAI>();
			if (component != null)
			{
				component.SetPatrolPoint();
			}
		}
		Character component2 = gameObject.GetComponent<Character>();
		if (spawnData.m_maxLevel > 1)
		{
			int i;
			for (i = spawnData.m_minLevel; i < spawnData.m_maxLevel; i++)
			{
				if (!(UnityEngine.Random.Range(0f, 100f) <= GetLevelUpChance()))
				{
					break;
				}
			}
			if (i > 1)
			{
				component2.SetLevel(i);
			}
		}
		Vector3 centerPoint = component2.GetCenterPoint();
		m_spawnEffects.Create(centerPoint, Quaternion.identity);
		return true;
	}

	private bool FindSpawnPoint(GameObject prefab, out Vector3 point)
	{
		prefab.GetComponent<BaseAI>();
		for (int i = 0; i < 10; i++)
		{
			Vector3 vector = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, m_spawnRadius);
			if (ZoneSystem.instance.FindFloor(vector, out var height) && (!m_onGroundOnly || !ZoneSystem.instance.IsBlocked(vector)))
			{
				vector.y = height + 0.1f;
				point = vector;
				return true;
			}
		}
		point = Vector3.zero;
		return false;
	}

	private SpawnData SelectWeightedPrefab()
	{
		if (m_prefabs.Count == 0)
		{
			return null;
		}
		float num = 0f;
		foreach (SpawnData prefab in m_prefabs)
		{
			num += prefab.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (SpawnData prefab2 in m_prefabs)
		{
			num3 += prefab2.m_weight;
			if (num2 <= num3)
			{
				return prefab2;
			}
		}
		return m_prefabs[m_prefabs.Count - 1];
	}

	private void GetInstances(out int near, out int total)
	{
		near = 0;
		total = 0;
		Vector3 position = base.transform.position;
		foreach (BaseAI baseAIInstance in BaseAI.BaseAIInstances)
		{
			if (IsSpawnPrefab(baseAIInstance.gameObject))
			{
				float num = Utils.DistanceXZ(baseAIInstance.transform.position, position);
				if (num < m_nearRadius)
				{
					near++;
				}
				if (num < m_farRadius)
				{
					total++;
				}
			}
		}
	}

	private bool IsSpawnPrefab(GameObject go)
	{
		string a = go.name;
		Character component = go.GetComponent<Character>();
		foreach (SpawnData prefab in m_prefabs)
		{
			if (a.CustomStartsWith(prefab.m_prefab.name) && (!component || !component.IsTamed()))
			{
				return true;
			}
		}
		return false;
	}

	public float GetLevelUpChance()
	{
		if (Game.m_worldLevel > 0 && Game.instance.m_worldLevelEnemyLevelUpExponent > 0f)
		{
			return Mathf.Min(70f, Mathf.Pow(m_levelupChance, (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyLevelUpExponent));
		}
		return m_levelupChance * Game.m_enemyLevelUpRate;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, m_spawnRadius);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, m_nearRadius);
	}
}

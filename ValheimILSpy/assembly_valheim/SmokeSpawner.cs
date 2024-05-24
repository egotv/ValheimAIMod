using System.Collections.Generic;
using UnityEngine;

public class SmokeSpawner : MonoBehaviour, IMonoUpdater
{
	private const float m_minPlayerDistance = 64f;

	private const int m_maxGlobalSmoke = 100;

	private const float m_blockedMinTime = 4f;

	public GameObject m_smokePrefab;

	public float m_interval = 0.5f;

	public LayerMask m_testMask;

	public float m_testRadius = 0.5f;

	public float m_spawnRadius;

	private float m_lastSpawnTime;

	private float m_time;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Start()
	{
		m_time = Random.Range(0f, m_interval);
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		m_time += deltaTime;
		if (m_time > m_interval)
		{
			m_time = 0f;
			Spawn(time);
		}
	}

	private void Spawn(float time)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || Vector3.Distance(localPlayer.transform.position, base.transform.position) > 64f)
		{
			m_lastSpawnTime = time;
		}
		else if (!TestBlocked())
		{
			if (Smoke.GetTotalSmoke() > 100)
			{
				Smoke.FadeOldest();
			}
			Vector3 position = base.transform.position;
			if (m_spawnRadius > 0f)
			{
				Vector2 vector = Random.insideUnitCircle.normalized * Random.Range(m_spawnRadius / 2f, m_spawnRadius);
				position += new Vector3(vector.x, 0f, vector.y);
			}
			Object.Instantiate(m_smokePrefab, position, Random.rotation);
			m_lastSpawnTime = time;
		}
	}

	private bool TestBlocked()
	{
		if (Physics.CheckSphere(base.transform.position, m_testRadius, m_testMask.value))
		{
			return true;
		}
		return false;
	}

	public bool IsBlocked()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return TestBlocked();
		}
		return Time.time - m_lastSpawnTime > 4f;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Utils.DrawGizmoCircle(base.transform.position, m_spawnRadius, 16);
	}
}

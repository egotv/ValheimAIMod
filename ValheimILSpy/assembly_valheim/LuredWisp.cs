using System.Collections.Generic;
using UnityEngine;

public class LuredWisp : MonoBehaviour
{
	public bool m_despawnInDaylight = true;

	public float m_maxLureDistance = 20f;

	public float m_acceleration = 6f;

	public float m_noiseDistance = 1.5f;

	public float m_noiseDistanceYScale = 0.2f;

	public float m_noiseSpeed = 0.5f;

	public float m_maxSpeed = 40f;

	public float m_friction = 0.03f;

	public EffectList m_despawnEffects = new EffectList();

	private static List<LuredWisp> m_wisps = new List<LuredWisp>();

	private Vector3 m_ballVel = Vector3.zero;

	private ZNetView m_nview;

	private Vector3 m_targetPoint;

	private float m_despawnTimer;

	private float m_time;

	private void Awake()
	{
		m_wisps.Add(this);
		m_nview = GetComponent<ZNetView>();
		m_targetPoint = base.transform.position;
		m_time = Random.Range(0, 1000);
		InvokeRepeating("UpdateTarget", Random.Range(0f, 2f), 2f);
	}

	private void OnDestroy()
	{
		m_wisps.Remove(this);
	}

	private void UpdateTarget()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !(m_despawnTimer > 0f))
		{
			WispSpawner bestSpawner = WispSpawner.GetBestSpawner(base.transform.position, m_maxLureDistance);
			if (bestSpawner == null || (m_despawnInDaylight && EnvMan.IsDaylight()))
			{
				m_despawnTimer = 3f;
				m_targetPoint = base.transform.position + Quaternion.Euler(-20f, Random.Range(0, 360), 0f) * Vector3.forward * 100f;
			}
			else
			{
				m_despawnTimer = 0f;
				m_targetPoint = bestSpawner.m_spawnPoint.position;
			}
		}
	}

	private void FixedUpdate()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			UpdateMovement(m_targetPoint, Time.fixedDeltaTime);
		}
	}

	private void UpdateMovement(Vector3 targetPos, float dt)
	{
		if (m_despawnTimer > 0f)
		{
			m_despawnTimer -= dt;
			if (m_despawnTimer <= 0f)
			{
				m_despawnEffects.Create(base.transform.position, base.transform.rotation);
				m_nview.Destroy();
				return;
			}
		}
		m_time += dt;
		float num = m_time * m_noiseSpeed;
		targetPos += new Vector3(Mathf.Sin(num * 4f), Mathf.Sin(num * 2f) * m_noiseDistanceYScale, Mathf.Cos(num * 5f)) * m_noiseDistance;
		Vector3 normalized = (targetPos - base.transform.position).normalized;
		m_ballVel += normalized * m_acceleration * dt;
		if (m_ballVel.magnitude > m_maxSpeed)
		{
			m_ballVel = m_ballVel.normalized * m_maxSpeed;
		}
		m_ballVel -= m_ballVel * m_friction;
		base.transform.position = base.transform.position + m_ballVel * dt;
	}

	public static int GetWispsInArea(Vector3 p, float r)
	{
		float num = r * r;
		int num2 = 0;
		foreach (LuredWisp wisp in m_wisps)
		{
			if (Utils.DistanceSqr(p, wisp.transform.position) < num)
			{
				num2++;
			}
		}
		return num2;
	}
}

using System;
using UnityEngine;

public class DistantFogEmitter : MonoBehaviour
{
	public float m_interval = 1f;

	public float m_minRadius = 100f;

	public float m_maxRadius = 500f;

	public float m_mountainSpawnChance = 1f;

	public float m_landSpawnChance = 0.5f;

	public float m_waterSpawnChance = 0.25f;

	public float m_mountainLimit = 120f;

	public float m_emitStep = 10f;

	public int m_emitPerStep = 10;

	public int m_particles = 100;

	public float m_placeOffset = 1f;

	public ParticleSystem[] m_psystems;

	public bool m_skipWater;

	private float m_placeTimer;

	private bool m_emit = true;

	private Vector3 m_lastPosition = Vector3.zero;

	public void SetEmit(bool emit)
	{
		m_emit = emit;
	}

	private void Update()
	{
		if (!m_emit || WorldGenerator.instance == null)
		{
			return;
		}
		m_placeTimer += Time.deltaTime;
		if (m_placeTimer > m_interval)
		{
			m_placeTimer = 0f;
			int num = Mathf.Max(0, m_particles - TotalNrOfParticles());
			num /= 4;
			for (int i = 0; i < num; i++)
			{
				PlaceOne();
			}
		}
	}

	private int TotalNrOfParticles()
	{
		int num = 0;
		ParticleSystem[] psystems = m_psystems;
		foreach (ParticleSystem particleSystem in psystems)
		{
			num += particleSystem.particleCount;
		}
		return num;
	}

	private void PlaceOne()
	{
		if (GetRandomPoint(base.transform.position, out var p))
		{
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = p + Vector3.up * m_placeOffset;
			m_psystems[UnityEngine.Random.Range(0, m_psystems.Length)].Emit(emitParams, 1);
		}
	}

	private bool GetRandomPoint(Vector3 center, out Vector3 p)
	{
		float f = UnityEngine.Random.value * (float)Math.PI * 2f;
		float num = Mathf.Sqrt(UnityEngine.Random.value) * (m_maxRadius - m_minRadius) + m_minRadius;
		p = center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
		p.y = WorldGenerator.instance.GetHeight(p.x, p.z);
		if (p.y < 30f)
		{
			if (m_skipWater)
			{
				return false;
			}
			if (UnityEngine.Random.value > m_waterSpawnChance)
			{
				return false;
			}
			p.y = 30f;
		}
		else if (p.y > m_mountainLimit)
		{
			if (UnityEngine.Random.value > m_mountainSpawnChance)
			{
				return false;
			}
		}
		else if (UnityEngine.Random.value > m_landSpawnChance)
		{
			return false;
		}
		return true;
	}
}

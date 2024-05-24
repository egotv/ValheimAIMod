using System;
using UnityEngine;

public class MistEmitter : MonoBehaviour
{
	public float m_interval = 1f;

	public float m_totalRadius = 30f;

	public float m_testRadius = 5f;

	public int m_rays = 10;

	public float m_placeOffset = 1f;

	public ParticleSystem m_psystem;

	private float m_placeTimer;

	private bool m_emit = true;

	public void SetEmit(bool emit)
	{
		m_emit = emit;
	}

	private void Update()
	{
		if (m_emit)
		{
			m_placeTimer += Time.deltaTime;
			if (m_placeTimer > m_interval)
			{
				m_placeTimer = 0f;
				PlaceOne();
			}
		}
	}

	private void PlaceOne()
	{
		if (!GetRandomPoint(base.transform.position, m_totalRadius, out var p))
		{
			return;
		}
		int num = 0;
		float num2 = (float)Math.PI * 2f / (float)m_rays;
		for (int i = 0; i < m_rays; i++)
		{
			float angle = (float)i * num2;
			if ((double)GetPointOnEdge(p, angle, m_testRadius).y < (double)p.y - 0.1)
			{
				num++;
			}
		}
		if (num <= m_rays / 4 && !EffectArea.IsPointInsideArea(p, EffectArea.Type.Fire, m_testRadius))
		{
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = p + Vector3.up * m_placeOffset;
			m_psystem.Emit(emitParams, 1);
		}
	}

	private static bool GetRandomPoint(Vector3 center, float radius, out Vector3 p)
	{
		float f = UnityEngine.Random.value * (float)Math.PI * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		p = center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
		if (ZoneSystem.instance.GetGroundHeight(p, out var height))
		{
			if (height < 30f)
			{
				return false;
			}
			float liquidLevel = Floating.GetLiquidLevel(p);
			if (height < liquidLevel)
			{
				return false;
			}
			p.y = height;
			return true;
		}
		return false;
	}

	private static Vector3 GetPointOnEdge(Vector3 center, float angle, float radius)
	{
		Vector3 vector = center + new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
		vector.y = ZoneSystem.instance.GetGroundHeight(vector);
		if (vector.y < 30f)
		{
			vector.y = 30f;
		}
		return vector;
	}
}

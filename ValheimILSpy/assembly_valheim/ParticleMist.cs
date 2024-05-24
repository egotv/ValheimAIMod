using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleMist : MonoBehaviour
{
	private List<Heightmap> tempHeightmaps = new List<Heightmap>();

	private List<Demister> fields = new List<Demister>();

	private List<KeyValuePair<Demister, float>> sortList = new List<KeyValuePair<Demister, float>>();

	private static ParticleMist m_instance;

	private ParticleSystem m_ps;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome = Heightmap.Biome.Mistlands;

	public float m_localRange = 10f;

	public int m_localEmission = 50;

	public int m_localEmissionPerUnit = 50;

	public float m_maxMistAltitude = 50f;

	[Header("Misters")]
	public float m_distantMaxRange = 100f;

	public float m_distantMinSize = 5f;

	public float m_distantMaxSize = 20f;

	public float m_distantEmissionMax = 0.1f;

	public float m_distantEmissionMaxVel = 1f;

	public float m_distantThickness = 4f;

	[Header("Demisters")]
	public float m_minDistance = 10f;

	public float m_maxDistance = 50f;

	public float m_emissionMax = 0.2f;

	public float m_emissionPerUnit = 20f;

	public float m_minSize = 2f;

	public float m_maxSize = 10f;

	private float m_inMistAreaTimer;

	private float m_accumulator;

	private float m_combinedMovement;

	private Vector3 m_lastUpdatePos;

	private bool m_haveActiveMist;

	public static ParticleMist instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_ps = GetComponent<ParticleSystem>();
		m_lastUpdatePos = base.transform.position;
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void Update()
	{
		if (!m_ps.emission.enabled)
		{
			return;
		}
		m_accumulator += Time.fixedDeltaTime;
		if (m_accumulator < 0.1f)
		{
			return;
		}
		m_accumulator -= 0.1f;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		List<Mister> demistersSorted = Mister.GetDemistersSorted(localPlayer.transform.position);
		if (demistersSorted.Count == 0)
		{
			return;
		}
		m_haveActiveMist = demistersSorted.Count > 0;
		GetAllForcefields(fields);
		m_inMistAreaTimer += 0.1f;
		float value = Vector3.Distance(base.transform.position, m_lastUpdatePos);
		m_combinedMovement += Mathf.Clamp(value, 0f, 10f);
		m_lastUpdatePos = base.transform.position;
		FindMaxMistAlltitude(50f, out var minMistHeight, out var _);
		int num = (int)(m_combinedMovement * (float)m_localEmissionPerUnit);
		if (num > 0)
		{
			m_combinedMovement = Mathf.Max(0f, m_combinedMovement - (float)num / (float)m_localEmissionPerUnit);
		}
		int toEmit = (int)((float)m_localEmission * 0.1f) + num;
		Emit(base.transform.position, 0f, m_localRange, toEmit, fields, null, minMistHeight);
		foreach (Demister field in fields)
		{
			float endRange = field.m_forceField.endRange;
			float num2 = Mathf.Max(0f, Vector3.Distance(field.transform.position, base.transform.position) - endRange);
			if (!(num2 > m_maxDistance))
			{
				float num3 = (float)Math.PI * 4f * (endRange * endRange);
				float num4 = Mathf.Lerp(m_emissionMax, 0f, Utils.LerpStep(m_minDistance, m_maxDistance, num2));
				int num5 = (int)(num3 * num4 * 0.1f);
				float movedDistance = field.GetMovedDistance();
				num5 += (int)(movedDistance * m_emissionPerUnit);
				Emit(field.transform.position, endRange, 0f, num5, fields, field, minMistHeight);
			}
		}
		foreach (Mister item in demistersSorted)
		{
			if (!item.Inside(base.transform.position, 0f))
			{
				MisterEmit(item, demistersSorted, fields, minMistHeight, 0.1f);
			}
		}
	}

	private void Emit(Vector3 center, float radius, float thickness, int toEmit, List<Demister> fields, Demister pf, float minAlt)
	{
		if (!Mister.InsideMister(center, radius + thickness) || IsInsideOtherDemister(fields, center, radius + thickness, pf))
		{
			return;
		}
		ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
		for (int i = 0; i < toEmit; i++)
		{
			Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
			Vector3 vector = center + onUnitSphere * (radius + 0.1f + UnityEngine.Random.Range(0f, thickness));
			if (!(vector.y < minAlt) && !IsInsideOtherDemister(fields, vector, 0f, pf) && Mister.InsideMister(vector))
			{
				float num = Vector3.Distance(base.transform.position, vector);
				if (!(num > m_maxDistance))
				{
					emitParams.startSize = Mathf.Lerp(m_minSize, m_maxSize, Utils.LerpStep(m_minDistance, m_maxDistance, num));
					emitParams.position = vector;
					m_ps.Emit(emitParams, 1);
				}
			}
		}
	}

	private void MisterEmit(Mister mister, List<Mister> allMisters, List<Demister> fields, float minAlt, float dt)
	{
		Vector3 position = mister.transform.position;
		float radius = mister.m_radius;
		float num = Mathf.Max(0f, Vector3.Distance(mister.transform.position, base.transform.position) - radius);
		if (num > m_distantMaxRange || mister.IsCompletelyInsideOtherMister(m_distantThickness))
		{
			return;
		}
		float num2 = (float)Math.PI * 4f * (radius * radius);
		float num3 = Mathf.Lerp(m_distantEmissionMax, 0f, Utils.LerpStep(0f, m_distantMaxRange, num));
		int num4 = (int)(num2 * num3 * dt);
		float num5 = mister.transform.position.y + mister.m_height;
		ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
		for (int i = 0; i < num4; i++)
		{
			Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
			Vector3 vector = position + onUnitSphere * (radius + 0.1f + UnityEngine.Random.Range(0f, m_distantThickness));
			if (vector.y < minAlt)
			{
				continue;
			}
			if (vector.y > num5)
			{
				vector.y = num5;
			}
			if (!Mister.IsInsideOtherMister(vector, mister) && !IsInsideOtherDemister(fields, vector, 0f, null))
			{
				float num6 = Vector3.Distance(base.transform.position, vector);
				if (!(num6 > m_distantMaxRange))
				{
					emitParams.startSize = Mathf.Lerp(m_distantMinSize, m_distantMaxSize, Utils.LerpStep(0f, m_distantMaxRange, num6));
					emitParams.position = vector;
					Vector3 velocity = onUnitSphere * UnityEngine.Random.Range(0f, m_distantEmissionMaxVel);
					velocity.y = 0f;
					emitParams.velocity = velocity;
					m_ps.Emit(emitParams, 1);
				}
			}
		}
	}

	private bool IsInsideOtherDemister(List<Demister> fields, Vector3 p, float radius, Demister ignore)
	{
		foreach (Demister field in fields)
		{
			if (!(field == ignore) && Vector3.Distance(field.transform.position, p) + radius < field.m_forceField.endRange)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsInMist(Vector3 p0)
	{
		if (m_instance == null)
		{
			return false;
		}
		if (!m_instance.m_haveActiveMist)
		{
			return false;
		}
		if (Mister.InsideMister(p0))
		{
			return !m_instance.InsideDemister(p0);
		}
		return false;
	}

	public static bool IsMistBlocked(Vector3 p0, Vector3 p1)
	{
		if (m_instance == null)
		{
			return false;
		}
		return m_instance.IsMistBlocked_internal(p0, p1);
	}

	private bool IsMistBlocked_internal(Vector3 p0, Vector3 p1)
	{
		if (!m_haveActiveMist)
		{
			return false;
		}
		if (Vector3.Distance(p0, p1) < 10f)
		{
			return false;
		}
		Vector3 p2 = (p0 + p1) * 0.5f;
		if (Mister.InsideMister(p0) && !InsideDemister(p0))
		{
			return true;
		}
		if (Mister.InsideMister(p1) && !InsideDemister(p1))
		{
			return true;
		}
		if (Mister.InsideMister(p2) && !InsideDemister(p2))
		{
			return true;
		}
		return false;
	}

	private bool InsideDemister(Vector3 p)
	{
		foreach (Demister demister in Demister.GetDemisters())
		{
			if (Vector3.Distance(demister.transform.position, p) < demister.m_forceField.endRange)
			{
				return true;
			}
		}
		return false;
	}

	private void GetAllForcefields(List<Demister> fields)
	{
		List<Demister> demisters = Demister.GetDemisters();
		sortList.Clear();
		foreach (Demister item in demisters)
		{
			sortList.Add(new KeyValuePair<Demister, float>(item, Vector3.Distance(base.transform.position, item.transform.position)));
		}
		sortList.Sort((KeyValuePair<Demister, float> a, KeyValuePair<Demister, float> b) => a.Value.CompareTo(b.Value));
		fields.Clear();
		foreach (KeyValuePair<Demister, float> sort in sortList)
		{
			fields.Add(sort.Key);
		}
	}

	private void FindMaxMistAlltitude(float testRange, out float minMistHeight, out float maxMistHeight)
	{
		Vector3 position = base.transform.position;
		float num = 0f;
		int num2 = 20;
		minMistHeight = 99999f;
		for (int i = 0; i < num2; i++)
		{
			Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
			Vector3 p = position + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * testRange;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(p);
			num += groundHeight;
			if (groundHeight < minMistHeight)
			{
				minMistHeight = groundHeight;
			}
		}
		float num3 = num / (float)num2;
		maxMistHeight = num3 + m_maxMistAltitude;
	}
}

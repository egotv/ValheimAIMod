using System;
using UnityEngine;

public class Thunder : MonoBehaviour
{
	public float m_strikeIntervalMin = 3f;

	public float m_strikeIntervalMax = 10f;

	public float m_thunderDelayMin = 3f;

	public float m_thunderDelayMax = 5f;

	public float m_flashDistanceMin = 50f;

	public float m_flashDistanceMax = 200f;

	public float m_flashAltitude = 100f;

	public EffectList m_flashEffect = new EffectList();

	public EffectList m_thunderEffect = new EffectList();

	[Header("Thor")]
	public bool m_spawnThor;

	public string m_requiredGlobalKey = "";

	public GameObject m_thorPrefab;

	public float m_thorSpawnDistance = 300f;

	public float m_thorSpawnAltitudeMax = 100f;

	public float m_thorSpawnAltitudeMin = 100f;

	public float m_thorInterval = 10f;

	public float m_thorChance = 1f;

	private Vector3 m_flashPos = Vector3.zero;

	private float m_strikeTimer = -1f;

	private float m_thunderTimer = -1f;

	private float m_thorTimer;

	private void Start()
	{
		m_strikeTimer = UnityEngine.Random.Range(m_strikeIntervalMin, m_strikeIntervalMax);
	}

	private void Update()
	{
		if (m_strikeTimer > 0f)
		{
			m_strikeTimer -= Time.deltaTime;
			if (m_strikeTimer <= 0f)
			{
				DoFlash();
			}
		}
		if (m_thunderTimer > 0f)
		{
			m_thunderTimer -= Time.deltaTime;
			if (m_thunderTimer <= 0f)
			{
				DoThunder();
				m_strikeTimer = UnityEngine.Random.Range(m_strikeIntervalMin, m_strikeIntervalMax);
			}
		}
		if (!m_spawnThor)
		{
			return;
		}
		m_thorTimer += Time.deltaTime;
		if (m_thorTimer > m_thorInterval)
		{
			m_thorTimer = 0f;
			if (UnityEngine.Random.value <= m_thorChance && (m_requiredGlobalKey == "" || ZoneSystem.instance.GetGlobalKey(m_requiredGlobalKey)))
			{
				SpawnThor();
			}
		}
	}

	private void SpawnThor()
	{
		float num = UnityEngine.Random.value * ((float)Math.PI * 2f);
		Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(num), 0f, Mathf.Cos(num)) * m_thorSpawnDistance;
		vector.y += UnityEngine.Random.Range(m_thorSpawnAltitudeMin, m_thorSpawnAltitudeMax);
		float groundHeight = ZoneSystem.instance.GetGroundHeight(vector);
		if (vector.y < groundHeight)
		{
			vector.y = groundHeight + 50f;
		}
		float f = num + 180f + (float)UnityEngine.Random.Range(-45, 45);
		Vector3 vector2 = base.transform.position + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * m_thorSpawnDistance;
		vector2.y += UnityEngine.Random.Range(m_thorSpawnAltitudeMin, m_thorSpawnAltitudeMax);
		float groundHeight2 = ZoneSystem.instance.GetGroundHeight(vector2);
		if (vector.y < groundHeight2)
		{
			vector.y = groundHeight2 + 50f;
		}
		Vector3 normalized = (vector2 - vector).normalized;
		UnityEngine.Object.Instantiate(m_thorPrefab, vector, Quaternion.LookRotation(normalized));
	}

	private void DoFlash()
	{
		float f = UnityEngine.Random.value * ((float)Math.PI * 2f);
		float num = UnityEngine.Random.Range(m_flashDistanceMin, m_flashDistanceMax);
		m_flashPos = base.transform.position + new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * num;
		m_flashPos.y += m_flashAltitude;
		Quaternion rotation = Quaternion.LookRotation((base.transform.position - m_flashPos).normalized);
		GameObject[] array = m_flashEffect.Create(m_flashPos, Quaternion.identity);
		for (int i = 0; i < array.Length; i++)
		{
			Light[] componentsInChildren = array[i].GetComponentsInChildren<Light>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].transform.rotation = rotation;
			}
		}
		m_thunderTimer = UnityEngine.Random.Range(m_thunderDelayMin, m_thunderDelayMax);
	}

	private void DoThunder()
	{
		m_thunderEffect.Create(m_flashPos, Quaternion.identity);
	}
}

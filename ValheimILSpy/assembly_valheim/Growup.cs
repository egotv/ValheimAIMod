using System;
using System.Collections.Generic;
using UnityEngine;

public class Growup : MonoBehaviour
{
	[Serializable]
	public class GrownEntry
	{
		public GameObject m_prefab;

		public float m_weight = 1f;
	}

	public float m_growTime = 60f;

	public bool m_inheritTame = true;

	public GameObject m_grownPrefab;

	public List<GrownEntry> m_altGrownPrefabs;

	private BaseAI m_baseAI;

	private ZNetView m_nview;

	private void Start()
	{
		m_baseAI = GetComponent<BaseAI>();
		m_nview = GetComponent<ZNetView>();
		InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(10f, 15f), 10f);
	}

	private void GrowUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || !(m_baseAI.GetTimeSinceSpawned().TotalSeconds > (double)m_growTime))
		{
			return;
		}
		Character component = GetComponent<Character>();
		Character component2 = UnityEngine.Object.Instantiate(GetPrefab(), base.transform.position, base.transform.rotation).GetComponent<Character>();
		if ((bool)component && (bool)component2)
		{
			if (m_inheritTame)
			{
				component2.SetTamed(component.IsTamed());
			}
			component2.SetLevel(component.GetLevel());
		}
		m_nview.Destroy();
	}

	private GameObject GetPrefab()
	{
		if (m_altGrownPrefabs == null || m_altGrownPrefabs.Count == 0)
		{
			return m_grownPrefab;
		}
		float num = 0f;
		foreach (GrownEntry altGrownPrefab in m_altGrownPrefabs)
		{
			num += altGrownPrefab.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int i = 0; i < m_altGrownPrefabs.Count; i++)
		{
			num3 += m_altGrownPrefabs[i].m_weight;
			if (num2 <= num3)
			{
				return m_altGrownPrefabs[i].m_prefab;
			}
		}
		return m_altGrownPrefabs[0].m_prefab;
	}
}

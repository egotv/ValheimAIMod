using System.Collections.Generic;
using UnityEngine;

public class RandomSpawn : MonoBehaviour
{
	public GameObject m_OffObject;

	[Range(0f, 100f)]
	public float m_chanceToSpawn = 50f;

	public Room.Theme m_dungeonRequireTheme;

	public Heightmap.Biome m_requireBiome;

	public bool m_notInLava;

	[Header("Elevation span (water is 30)")]
	public int m_minElevation = -10000;

	public int m_maxElevation = 10000;

	private List<ZNetView> m_childNetViews;

	private ZNetView m_nview;

	public void Randomize(Vector3 pos, Location loc = null, DungeonGenerator dg = null)
	{
		bool spawned = Random.Range(0f, 100f) <= m_chanceToSpawn;
		if (dg != null && m_dungeonRequireTheme != 0 && !dg.m_themes.HasFlag(m_dungeonRequireTheme))
		{
			spawned = false;
		}
		if (loc != null && m_requireBiome != 0)
		{
			if (loc.m_biome == Heightmap.Biome.None)
			{
				loc.m_biome = WorldGenerator.instance.GetBiome(pos);
			}
			if (!m_requireBiome.HasFlag(loc.m_biome))
			{
				spawned = false;
			}
		}
		if (m_notInLava && (bool)ZoneSystem.instance && ZoneSystem.IsLavaPreHeightmap(pos))
		{
			spawned = false;
		}
		if (pos.y < (float)m_minElevation || pos.y > (float)m_maxElevation)
		{
			spawned = false;
		}
		SetSpawned(spawned);
	}

	public void Reset()
	{
		SetSpawned(doSpawn: true);
	}

	private void SetSpawned(bool doSpawn)
	{
		if (!doSpawn)
		{
			base.gameObject.SetActive(value: false);
			foreach (ZNetView childNetView in m_childNetViews)
			{
				childNetView.gameObject.SetActive(value: false);
			}
		}
		else if (m_nview == null)
		{
			base.gameObject.SetActive(value: true);
		}
		if (m_OffObject != null)
		{
			m_OffObject.SetActive(!doSpawn);
		}
	}

	public void Prepare()
	{
		m_nview = GetComponent<ZNetView>();
		m_childNetViews = new List<ZNetView>();
		ZNetView[] componentsInChildren = base.gameObject.GetComponentsInChildren<ZNetView>(includeInactive: true);
		foreach (ZNetView zNetView in componentsInChildren)
		{
			if (Utils.IsEnabledInheirarcy(zNetView.gameObject, base.gameObject))
			{
				m_childNetViews.Add(zNetView);
			}
		}
	}
}

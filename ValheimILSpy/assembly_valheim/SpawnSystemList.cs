using System.Collections.Generic;
using UnityEngine;

public class SpawnSystemList : MonoBehaviour
{
	public List<SpawnSystem.SpawnData> m_spawners = new List<SpawnSystem.SpawnData>();

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	public void GetSpawners(Heightmap.Biome biome, List<SpawnSystem.SpawnData> spawners)
	{
		foreach (SpawnSystem.SpawnData spawner in m_spawners)
		{
			if ((spawner.m_biome & biome) != 0 || spawner.m_biome == biome)
			{
				spawners.Add(spawner);
			}
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

public class MaterialVariationWorld : MonoBehaviour
{
	public List<MaterialVariationSettings> m_variations = new List<MaterialVariationSettings>();

	private static List<MeshRenderer> mrs = new List<MeshRenderer>();

	private void Update()
	{
		Location zoneLocation = Location.GetZoneLocation(base.transform.position);
		if (!zoneLocation)
		{
			return;
		}
		GetComponentsInChildren(mrs);
		DungeonGenerator generator = zoneLocation.m_generator;
		foreach (MaterialVariationSettings variation in m_variations)
		{
			if ((bool)generator && variation.m_dungeonThemeCondition != 0 && generator.m_themes.HasFlag(variation.m_dungeonThemeCondition))
			{
				change(variation);
			}
			if ((bool)zoneLocation && variation.m_biomeCondition != 0)
			{
				if (zoneLocation.m_biome == Heightmap.Biome.None)
				{
					zoneLocation.m_biome = WorldGenerator.instance.GetBiome(zoneLocation.transform.position);
				}
				if (variation.m_biomeCondition == zoneLocation.m_biome)
				{
					change(variation);
				}
			}
		}
		base.enabled = false;
		void change(MaterialVariationSettings mvs)
		{
			foreach (MeshRenderer mr in mrs)
			{
				mr.materials = mvs.m_materials;
				Terminal.Log($"Replaced material on {base.gameObject.name} for dungeon {mvs.m_dungeonThemeCondition} or {mvs.m_biomeCondition}");
			}
		}
	}
}

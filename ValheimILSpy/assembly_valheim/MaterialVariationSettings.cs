using System;
using UnityEngine;

[Serializable]
public struct MaterialVariationSettings
{
	public Material[] m_materials;

	public Room.Theme m_dungeonThemeCondition;

	public Heightmap.Biome m_biomeCondition;
}

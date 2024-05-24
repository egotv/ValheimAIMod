using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelEffects : MonoBehaviour
{
	[Serializable]
	public class LevelSetup
	{
		public float m_scale = 1f;

		public float m_hue;

		public float m_saturation;

		public float m_value;

		public bool m_setEmissiveColor;

		[ColorUsage(false, true)]
		public Color m_emissiveColor = Color.white;

		public GameObject m_enableObject;
	}

	public Renderer m_mainRender;

	public GameObject m_baseEnableObject;

	public List<LevelSetup> m_levelSetups = new List<LevelSetup>();

	private static Dictionary<string, Material> m_materials = new Dictionary<string, Material>();

	private Character m_character;

	private void Start()
	{
		m_character = GetComponentInParent<Character>();
		Character character = m_character;
		character.m_onLevelSet = (Action<int>)Delegate.Combine(character.m_onLevelSet, new Action<int>(OnLevelSet));
		SetupLevelVisualization(m_character.GetLevel());
	}

	private void OnLevelSet(int level)
	{
		SetupLevelVisualization(level);
	}

	private void SetupLevelVisualization(int level)
	{
		if (level <= 1 || m_levelSetups.Count < level - 1)
		{
			return;
		}
		LevelSetup levelSetup = m_levelSetups[level - 2];
		base.transform.localScale = new Vector3(levelSetup.m_scale, levelSetup.m_scale, levelSetup.m_scale);
		if ((bool)m_mainRender)
		{
			string key = Utils.GetPrefabName(m_character.gameObject) + level;
			if (m_materials.TryGetValue(key, out var value))
			{
				Material[] sharedMaterials = m_mainRender.sharedMaterials;
				sharedMaterials[0] = value;
				m_mainRender.sharedMaterials = sharedMaterials;
			}
			else
			{
				Material[] sharedMaterials2 = m_mainRender.sharedMaterials;
				sharedMaterials2[0] = new Material(sharedMaterials2[0]);
				sharedMaterials2[0].SetFloat("_Hue", levelSetup.m_hue);
				sharedMaterials2[0].SetFloat("_Saturation", levelSetup.m_saturation);
				sharedMaterials2[0].SetFloat("_Value", levelSetup.m_value);
				if (levelSetup.m_setEmissiveColor)
				{
					sharedMaterials2[0].SetColor("_EmissionColor", levelSetup.m_emissiveColor);
				}
				m_mainRender.sharedMaterials = sharedMaterials2;
				m_materials[key] = sharedMaterials2[0];
			}
		}
		if ((bool)m_baseEnableObject)
		{
			m_baseEnableObject.SetActive(value: false);
		}
		if ((bool)levelSetup.m_enableObject)
		{
			levelSetup.m_enableObject.SetActive(value: true);
		}
	}

	public void GetColorChanges(out float hue, out float saturation, out float value)
	{
		int level = m_character.GetLevel();
		if (level > 1 && m_levelSetups.Count >= level - 1)
		{
			LevelSetup levelSetup = m_levelSetups[level - 2];
			hue = levelSetup.m_hue;
			saturation = levelSetup.m_saturation;
			value = levelSetup.m_value;
		}
		else
		{
			hue = 0f;
			saturation = 0f;
			value = 0f;
		}
	}
}

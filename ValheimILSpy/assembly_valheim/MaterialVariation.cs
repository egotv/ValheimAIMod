using System;
using System.Collections.Generic;
using UnityEngine;

public class MaterialVariation : MonoBehaviour
{
	[Serializable]
	public class MaterialEntry
	{
		public Material m_material;

		public float m_weight = 1f;
	}

	public int m_materialIndex;

	public List<MaterialEntry> m_materials = new List<MaterialEntry>();

	private ZNetView m_nview;

	private Renderer m_renderer;

	private int m_variation = -1;

	private string m_matName;

	private bool m_isSet;

	private Piece m_piece;

	private int m_checks;

	private void Start()
	{
		m_nview = GetComponentInParent<ZNetView>();
		m_piece = GetComponentInParent<Piece>();
		m_renderer = GetComponent<SkinnedMeshRenderer>();
		if (!m_renderer)
		{
			m_renderer = GetComponent<MeshRenderer>();
		}
		if (!m_nview || !m_renderer)
		{
			ZLog.LogError("Missing nview or renderer on '" + base.transform.gameObject.name + "'");
		}
		InvokeRepeating("CheckMaterial", 0f, 0.2f);
	}

	private void CheckMaterial()
	{
		if (((!m_isSet && m_variation < 0) || (m_isSet && m_renderer.materials[m_materialIndex].name != m_matName && (!m_piece || !Player.IsPlacementGhost(m_piece.gameObject)))) && (bool)m_nview && m_nview.GetZDO() != null && (bool)m_renderer)
		{
			m_variation = m_nview.GetZDO().GetInt("MatVar" + m_materialIndex, -1);
			if (m_variation < 0 && m_nview.IsOwner())
			{
				m_variation = GetWeightedVariation();
				m_nview.GetZDO().Set("MatVar" + m_materialIndex, m_variation);
			}
			if (m_variation >= 0)
			{
				Material[] materials = m_renderer.materials;
				materials[m_materialIndex] = m_materials[m_variation].m_material;
				m_renderer.materials = materials;
				m_matName = m_renderer.materials[m_materialIndex].name;
				m_isSet = true;
			}
		}
		m_checks++;
		if (m_checks >= 5)
		{
			CancelInvoke("CheckMaterial");
		}
	}

	private int GetWeightedVariation()
	{
		float num = 0f;
		foreach (MaterialEntry material in m_materials)
		{
			num += material.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int i = 0; i < m_materials.Count; i++)
		{
			num3 += m_materials[i].m_weight;
			if (num2 <= num3)
			{
				return i;
			}
		}
		return 0;
	}
}

using System;
using UnityEngine;

public class ResourceRoot : MonoBehaviour, Hoverable
{
	public string m_name = "$item_ancientroot";

	public string m_statusHigh = "$item_ancientroot_full";

	public string m_statusLow = "$item_ancientroot_half";

	public string m_statusEmpty = "$item_ancientroot_empty";

	public float m_maxLevel = 100f;

	public float m_highThreshold = 50f;

	public float m_emptyTreshold = 10f;

	public float m_regenPerSec = 1f;

	public Color m_fullColor = Color.white;

	public Color m_emptyColor = Color.black;

	public MeshRenderer[] m_meshes;

	private ZNetView m_nview;

	private bool m_wasModified;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_nview.Register<float>("RPC_Drain", RPC_Drain);
			InvokeRepeating("UpdateTick", UnityEngine.Random.Range(0f, 10f), 10f);
		}
	}

	public string GetHoverText()
	{
		float level = GetLevel();
		string text = ((level > m_highThreshold) ? m_statusHigh : ((!(level > m_emptyTreshold)) ? m_statusEmpty : m_statusLow));
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool CanDrain(float amount)
	{
		return GetLevel() > amount;
	}

	public bool Drain(float amount)
	{
		if (!CanDrain(amount))
		{
			return false;
		}
		m_nview.InvokeRPC("RPC_Drain", amount);
		return true;
	}

	private void RPC_Drain(long caller, float amount)
	{
		if (GetLevel() > amount)
		{
			ModifyLevel(0f - amount);
		}
	}

	private double GetTimeSinceLastUpdate()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, time.Ticks));
		TimeSpan timeSpan = time - dateTime;
		m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return num;
	}

	private void ModifyLevel(float mod)
	{
		float level = GetLevel();
		level += mod;
		level = Mathf.Clamp(level, 0f, m_maxLevel);
		m_nview.GetZDO().Set(ZDOVars.s_level, level);
	}

	public float GetLevel()
	{
		return m_nview.GetZDO().GetFloat(ZDOVars.s_level, m_maxLevel);
	}

	private void UpdateTick()
	{
		if (m_nview.IsOwner())
		{
			double timeSinceLastUpdate = GetTimeSinceLastUpdate();
			float mod = (float)((double)m_regenPerSec * timeSinceLastUpdate);
			ModifyLevel(mod);
		}
		float level = GetLevel();
		if (!(level < m_emptyTreshold) && !m_wasModified)
		{
			return;
		}
		m_wasModified = true;
		float t = Utils.LerpStep(m_emptyTreshold, m_highThreshold, level);
		Color value = Color.Lerp(m_emptyColor, m_fullColor, t);
		MeshRenderer[] meshes = m_meshes;
		for (int i = 0; i < meshes.Length; i++)
		{
			Material[] materials = meshes[i].materials;
			for (int j = 0; j < materials.Length; j++)
			{
				materials[j].SetColor("_EmissiveColor", value);
			}
		}
	}

	public bool IsLevelLow()
	{
		return GetLevel() < m_emptyTreshold;
	}
}

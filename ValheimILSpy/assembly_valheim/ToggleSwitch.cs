using System;
using UnityEngine;

public class ToggleSwitch : MonoBehaviour, Interactable, Hoverable
{
	public MeshRenderer m_renderer;

	public Material m_enableMaterial;

	public Material m_disableMaterial;

	public Action<ToggleSwitch, Humanoid> m_onUse;

	public string m_hoverText = "";

	public string m_name = "";

	private bool m_state;

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (m_onUse != null)
		{
			m_onUse(this, character);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string GetHoverText()
	{
		return m_hoverText;
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public void SetState(bool enabled)
	{
		m_state = enabled;
		m_renderer.material = (m_state ? m_enableMaterial : m_disableMaterial);
	}
}

using UnityEngine;

public class Switch : MonoBehaviour, Interactable, Hoverable
{
	public delegate bool Callback(Switch caller, Humanoid user, ItemDrop.ItemData item);

	public delegate string TooltipCallback();

	public Callback m_onUse;

	public TooltipCallback m_onHover;

	[TextArea(3, 20)]
	public string m_hoverText = "";

	public string m_name = "";

	public float m_holdRepeatInterval = -1f;

	private float m_lastUseTime;

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			if (m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - m_lastUseTime < m_holdRepeatInterval)
			{
				return false;
			}
		}
		m_lastUseTime = Time.time;
		if (m_onUse != null)
		{
			return m_onUse(this, character, null);
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_onUse != null)
		{
			return m_onUse(this, user, item);
		}
		return false;
	}

	public string GetHoverText()
	{
		if (m_onHover != null)
		{
			return m_onHover();
		}
		return Localization.instance.Localize(m_hoverText);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_name);
	}
}

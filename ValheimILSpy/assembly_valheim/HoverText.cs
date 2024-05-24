using UnityEngine;

public class HoverText : MonoBehaviour, Hoverable
{
	public string m_text = "";

	public string GetHoverText()
	{
		return Localization.instance.Localize(m_text);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_text);
	}
}

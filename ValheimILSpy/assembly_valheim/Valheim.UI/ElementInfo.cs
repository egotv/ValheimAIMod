using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class ElementInfo : MonoBehaviour
{
	[SerializeField]
	protected TextMeshProUGUI m_selectedName;

	[SerializeField]
	protected Image m_icon;

	[SerializeField]
	protected TextMeshProUGUI m_selectedDescription;

	public void Clear()
	{
		m_selectedName.text = "";
		m_selectedDescription.text = "";
		m_icon.gameObject.SetActive(value: false);
	}

	public void Set(RadialMenuElement element)
	{
		if (element == null)
		{
			Clear();
			return;
		}
		m_selectedName.text = element.Name;
		m_selectedDescription.text = element.Description;
		m_icon.gameObject.SetActive(element is ItemElement);
	}

	public void Set(IRadialConfig config)
	{
		if (config == null)
		{
			Clear();
			return;
		}
		m_selectedName.text = config.LocalizedName;
		m_selectedDescription.text = "";
		m_icon.gameObject.SetActive(value: false);
	}
}

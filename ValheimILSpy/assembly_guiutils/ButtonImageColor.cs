using UnityEngine;
using UnityEngine.UI;

public class ButtonImageColor : MonoBehaviour
{
	public Image m_image;

	private Color m_defaultColor = Color.white;

	public Color m_disabledColor = Color.grey;

	private Button m_button;

	private void Awake()
	{
		m_button = GetComponent<Button>();
		m_defaultColor = m_image.color;
	}

	private void Update()
	{
		if (m_button.IsInteractable())
		{
			m_image.color = m_defaultColor;
		}
		else
		{
			m_image.color = m_disabledColor;
		}
	}
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTextColor : MonoBehaviour
{
	private Color m_defaultColor = Color.white;

	private Color m_defaultMeshColor = Color.white;

	public Color m_disabledColor = Color.grey;

	private Button m_button;

	private TMP_Text m_text;

	private TextMeshProUGUI m_textMesh;

	private Sprite m_sprite;

	private void Awake()
	{
		m_button = GetComponent<Button>();
		m_text = GetComponentInChildren<TMP_Text>();
		if (m_text != null)
		{
			m_defaultColor = m_text.color;
		}
		m_textMesh = GetComponentInChildren<TextMeshProUGUI>();
		if (m_textMesh != null)
		{
			m_defaultMeshColor = m_textMesh.color;
		}
	}

	private void Update()
	{
		bool flag = m_button.IsInteractable();
		if (m_text != null)
		{
			m_text.color = (flag ? m_defaultColor : m_disabledColor);
		}
		if (m_textMesh != null)
		{
			m_textMesh.color = (flag ? m_defaultMeshColor : m_disabledColor);
		}
	}
}

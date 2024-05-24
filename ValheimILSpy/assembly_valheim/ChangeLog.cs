using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeLog : MonoBehaviour
{
	private bool m_hasSetScroll;

	public TMP_Text m_textField;

	public TextAsset m_changeLog;

	public TextAsset m_xboxChangeLog;

	public Scrollbar m_scrollbar;

	public GameObject m_showPlayerLog;

	private void Start()
	{
		string text = m_changeLog.text;
		m_textField.text = text;
	}

	private void LateUpdate()
	{
		if (!m_hasSetScroll)
		{
			m_hasSetScroll = true;
			if (m_scrollbar != null)
			{
				m_scrollbar.value = 1f;
			}
		}
	}
}

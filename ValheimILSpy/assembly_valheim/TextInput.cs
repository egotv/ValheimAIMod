using GUIFramework;
using TMPro;
using UnityEngine;

public class TextInput : MonoBehaviour
{
	private static TextInput m_instance;

	public GameObject m_panel;

	public TMP_Text m_topic;

	public GuiInputField m_inputField;

	private TextReceiver m_queuedSign;

	private bool m_visibleFrame;

	private bool m_bShouldHideNextFrame;

	public static TextInput instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_panel.SetActive(value: false);
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_visibleFrame;
		}
		return false;
	}

	private void Update()
	{
		if (m_bShouldHideNextFrame)
		{
			m_bShouldHideNextFrame = false;
			Hide();
			return;
		}
		m_visibleFrame = m_instance.m_panel.gameObject.activeSelf;
		if (m_visibleFrame && !Console.IsVisible() && !Chat.instance.HasFocus() && ZInput.GetKeyDown(KeyCode.Escape))
		{
			Hide();
		}
	}

	public void OnInput()
	{
		setText(m_inputField.text.Replace("\\n", "\n").Replace("\\t", "\t"));
		m_bShouldHideNextFrame = true;
	}

	public void OnCancel()
	{
		Hide();
	}

	public void OnEnter()
	{
		setText(m_inputField.text.Replace("\\n", "\n").Replace("\\t", "\t"));
		Hide();
	}

	private void setText(string text)
	{
		if (m_queuedSign != null)
		{
			m_queuedSign.SetText(text);
			m_queuedSign = null;
		}
	}

	public void RequestText(TextReceiver sign, string topic, int charLimit)
	{
		m_queuedSign = sign;
		Show(topic, sign.GetText(), charLimit);
	}

	private void Show(string topic, string text, int charLimit)
	{
		m_panel.SetActive(value: true);
		m_topic.text = Localization.instance.Localize(topic);
		m_inputField.characterLimit = charLimit;
		m_inputField.text = text;
		m_inputField.ActivateInputField();
	}

	public void Hide()
	{
		m_panel.SetActive(value: false);
	}
}

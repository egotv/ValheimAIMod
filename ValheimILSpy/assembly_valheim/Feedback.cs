using GUIFramework;
using UnityEngine;
using UnityEngine.UI;

public class Feedback : MonoBehaviour
{
	private static Feedback m_instance;

	public GuiInputField m_subject;

	public GuiInputField m_text;

	public Button m_sendButton;

	public Toggle m_catBug;

	public Toggle m_catFeedback;

	public Toggle m_catIdea;

	private void Awake()
	{
		m_instance = this;
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public static bool IsVisible()
	{
		return m_instance != null;
	}

	private void LateUpdate()
	{
		m_sendButton.interactable = IsValid();
		if (IsVisible() && (ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")))))
		{
			OnBack();
		}
	}

	private bool IsValid()
	{
		if (m_subject.text.Length == 0)
		{
			return false;
		}
		if (m_text.text.Length == 0)
		{
			return false;
		}
		return true;
	}

	public void OnBack()
	{
		Object.Destroy(base.gameObject);
	}

	public void OnSend()
	{
		if (IsValid())
		{
			string category = GetCategory();
			Gogan.LogEvent("Feedback_" + category, m_subject.text, m_text.text, 0L);
			Object.Destroy(base.gameObject);
		}
	}

	private string GetCategory()
	{
		if (m_catBug.isOn)
		{
			return "Bug";
		}
		if (m_catFeedback.isOn)
		{
			return "Feedback";
		}
		if (m_catIdea.isOn)
		{
			return "Idea";
		}
		return "";
	}
}

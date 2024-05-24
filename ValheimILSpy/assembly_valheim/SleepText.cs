using TMPro;
using UnityEngine;

public class SleepText : MonoBehaviour
{
	public TMP_Text m_textField;

	public TMP_Text m_dreamField;

	public DreamTexts m_dreamTexts;

	private void OnEnable()
	{
		m_textField.CrossFadeAlpha(0f, 0f, ignoreTimeScale: true);
		m_textField.CrossFadeAlpha(1f, 1f, ignoreTimeScale: true);
		m_dreamField.enabled = false;
		Invoke("CollectResources", 5f);
		Invoke("HideZZZ", 2f);
		Invoke("ShowDreamText", 4f);
	}

	private void HideZZZ()
	{
		m_textField.CrossFadeAlpha(0f, 2f, ignoreTimeScale: true);
	}

	private void CollectResources()
	{
		Game.instance.CollectResourcesCheck();
	}

	private void ShowDreamText()
	{
		DreamTexts.DreamText randomDreamText = m_dreamTexts.GetRandomDreamText();
		if (randomDreamText != null)
		{
			m_dreamField.text = Localization.instance.Localize(randomDreamText.m_text);
			m_dreamField.enabled = true;
			Invoke("DelayedCrossFadeStart", 0.1f);
			Invoke("HideDreamText", 6.5f);
		}
	}

	private void DelayedCrossFadeStart()
	{
		m_dreamField.CrossFadeAlpha(0f, 0f, ignoreTimeScale: true);
		m_dreamField.CrossFadeAlpha(1f, 1.5f, ignoreTimeScale: true);
	}

	private void HideDreamText()
	{
		m_dreamField.CrossFadeAlpha(0f, 1.5f, ignoreTimeScale: true);
	}
}

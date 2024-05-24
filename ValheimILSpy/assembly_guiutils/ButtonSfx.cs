using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSfx : MonoBehaviour, ISelectHandler, IEventSystemHandler
{
	public GameObject m_sfxPrefab;

	public GameObject m_selectSfxPrefab;

	private Selectable m_selectable;

	private static int m_lastTriggerFrame;

	private const int m_minDeltaFrames = 2;

	private void Start()
	{
		m_selectable = GetComponent<Selectable>();
		Button button = m_selectable as Button;
		if ((bool)button)
		{
			button.onClick.AddListener(OnClick);
			return;
		}
		Toggle toggle = m_selectable as Toggle;
		if ((bool)toggle)
		{
			toggle.onValueChanged.AddListener(OnChange);
		}
	}

	private void OnClick()
	{
		if ((bool)m_sfxPrefab && Time.frameCount - m_lastTriggerFrame > 2)
		{
			Object.Instantiate(m_sfxPrefab);
			m_lastTriggerFrame = Time.frameCount;
		}
	}

	private void OnChange(bool v)
	{
		if ((bool)m_sfxPrefab && Time.frameCount - m_lastTriggerFrame > 2)
		{
			Object.Instantiate(m_sfxPrefab);
			m_lastTriggerFrame = Time.frameCount;
		}
	}

	public void OnSelect(BaseEventData eventData)
	{
	}
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class SettingsTooltip : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public enum TooltipAlignment
	{
		Left,
		Bottom,
		Right,
		Top
	}

	[SerializeField]
	private GameObject m_tooltip;

	[SerializeField]
	private float m_showDelay = 0.2f;

	[SerializeField]
	private int m_space = 15;

	[SerializeField]
	private string m_textId;

	[SerializeField]
	private string m_topicId;

	[SerializeField]
	private TooltipAlignment m_tooltipAlignment = TooltipAlignment.Right;

	private static Selectable s_current;

	private Selectable m_selectable;

	private Image m_background;

	private TMP_Text m_text;

	private TMP_Text m_topic;

	private bool m_shown;

	private void Start()
	{
		if (m_tooltip == null)
		{
			Debug.LogWarning("No tooltip object set, removing tooltip component from " + base.gameObject.name);
			Object.Destroy(this);
			return;
		}
		m_selectable = GetComponent<Selectable>();
		if (m_selectable == null)
		{
			Debug.LogWarning("No selectable found, removing tooltip component from " + base.gameObject.name);
			Object.Destroy(this);
			return;
		}
		m_topic = Utils.FindChild(m_tooltip.transform, "Topic")?.GetComponent<TMP_Text>();
		m_text = Utils.FindChild(m_tooltip.transform, "Text")?.GetComponent<TMP_Text>();
		m_background = Utils.FindChild(m_tooltip.transform, "Background")?.GetComponent<Image>();
		if (s_current == null)
		{
			m_tooltip.gameObject.SetActive(value: false);
		}
		ZInput.OnInputLayoutChanged += OnInputLayoutChanged;
	}

	public void OnInputLayoutChanged()
	{
		if (m_shown)
		{
			Hide();
		}
	}

	private void Update()
	{
		if (ZInput.IsGamepadActive())
		{
			if (s_current == m_selectable && EventSystem.current.currentSelectedGameObject != m_selectable.gameObject)
			{
				Hide();
			}
			else if (!(EventSystem.current.currentSelectedGameObject != m_selectable.gameObject) && (!(s_current == m_selectable) || !m_shown))
			{
				Show();
			}
		}
	}

	private void OnDestroy()
	{
		ZInput.OnInputLayoutChanged -= OnInputLayoutChanged;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!(s_current == m_selectable) || !m_shown)
		{
			Show();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!ZInput.GamepadActive)
		{
			Hide();
		}
	}

	private void Show()
	{
		s_current = m_selectable;
		m_shown = true;
		StartCoroutine(DelayedShow());
	}

	private void Hide()
	{
		m_tooltip.gameObject.SetActive(value: false);
		m_shown = false;
		if (s_current == m_selectable)
		{
			s_current = null;
		}
	}

	private IEnumerator DelayedShow()
	{
		yield return new WaitForSeconds(m_showDelay);
		if (s_current != m_selectable || !m_shown)
		{
			yield break;
		}
		if (m_topic != null)
		{
			m_topic.text = Localization.instance.Localize(m_topicId);
		}
		RectTransform component;
		if (m_text != null)
		{
			m_text.text = Localization.instance.Localize(m_textId);
			if (m_background != null)
			{
				m_tooltip.gameObject.SetActive(value: true);
				m_topic.ForceMeshUpdate();
				m_text.ForceMeshUpdate();
				m_tooltip.gameObject.SetActive(value: false);
				yield return 0;
				component = m_background.gameObject.GetComponent<RectTransform>();
				component.sizeDelta = new Vector2(component.sizeDelta.x, m_topic.textBounds.size.y + m_text.textBounds.size.y + 15f);
				component = m_text.gameObject.GetComponent<RectTransform>();
				Vector2 anchoredPosition = component.anchoredPosition;
				anchoredPosition.y = 0f - m_topic.textBounds.size.y - 10f;
				component.anchoredPosition = anchoredPosition;
			}
		}
		Vector3[] array = new Vector3[4];
		component = m_selectable.gameObject.GetComponent<RectTransform>();
		component.GetWorldCorners(array);
		Vector2 vector = new Vector2(array[3].x - array[0].x, array[1].y - array[0].y);
		Vector3[] array2 = new Vector3[4];
		component = m_background.gameObject.GetComponent<RectTransform>();
		component.GetWorldCorners(array2);
		Vector2 vector2 = new Vector2(array2[3].x - array2[0].x, array2[1].y - array2[0].y);
		float num = vector2.x / component.rect.width;
		switch (m_tooltipAlignment)
		{
		case TooltipAlignment.Right:
			m_tooltip.transform.position = new Vector2(array[2].x + vector2.x / 2f + (float)m_space * num, array[2].y - vector.y / 2f);
			break;
		case TooltipAlignment.Bottom:
			m_tooltip.transform.position = new Vector2(array[0].x + vector.x / 2f, array[0].y - vector2.y / 2f - (float)m_space * num);
			break;
		case TooltipAlignment.Left:
			m_tooltip.transform.position = new Vector2(array[0].x - vector2.x / 2f - (float)m_space * num, array[2].y - vector.y / 2f);
			break;
		case TooltipAlignment.Top:
			m_tooltip.transform.position = new Vector2(array[1].x + vector.x / 2f, array[1].y + vector2.y / 2f + (float)m_space * num);
			break;
		}
		m_tooltip.gameObject.SetActive(value: true);
	}
}

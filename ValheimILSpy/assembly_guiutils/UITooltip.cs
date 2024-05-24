using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private Selectable m_selectable;

	public GameObject m_tooltipPrefab;

	private RectTransform m_anchor;

	private Vector2 m_fixedPosition;

	public string m_text = "";

	public string m_topic = "";

	public GameObject m_gamepadFocusObject;

	private static UITooltip m_current;

	private static GameObject m_tooltip;

	private static GameObject m_hovered;

	private const float m_showDelay = 0.5f;

	private float m_showTimer;

	private void Awake()
	{
		m_selectable = GetComponent<Selectable>();
	}

	private void LateUpdate()
	{
		if (m_current == this && !m_tooltip.activeSelf)
		{
			m_showTimer += Time.deltaTime;
			if (m_showTimer > 0.5f || (ZInput.IsGamepadActive() && !ZInput.IsMouseActive()))
			{
				m_tooltip.SetActive(value: true);
			}
		}
		if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
		{
			if (m_gamepadFocusObject != null)
			{
				if (m_gamepadFocusObject.activeSelf && m_current != this)
				{
					OnHoverStart(m_gamepadFocusObject);
				}
				else if (!m_gamepadFocusObject.activeSelf && m_current == this)
				{
					HideTooltip();
				}
			}
			else if ((bool)m_selectable)
			{
				if (EventSystem.current.currentSelectedGameObject == m_selectable.gameObject && m_current != this)
				{
					OnHoverStart(m_selectable.gameObject);
				}
				else if (EventSystem.current.currentSelectedGameObject != m_selectable.gameObject && m_current == this)
				{
					HideTooltip();
				}
			}
			if (m_current == this && m_tooltip != null)
			{
				if (m_anchor != null)
				{
					m_tooltip.transform.SetParent(m_anchor);
					m_tooltip.transform.localPosition = m_fixedPosition;
					return;
				}
				if (m_fixedPosition != Vector2.zero)
				{
					m_tooltip.transform.position = m_fixedPosition;
					return;
				}
				RectTransform obj = base.gameObject.transform as RectTransform;
				Vector3[] array = new Vector3[4];
				obj.GetWorldCorners(array);
				m_tooltip.transform.position = (array[1] + array[2]) / 2f;
				Utils.ClampUIToScreen(m_tooltip.transform.GetChild(0).transform as RectTransform);
			}
		}
		else if (m_current == this)
		{
			if (m_hovered == null)
			{
				HideTooltip();
				return;
			}
			if (m_tooltip.activeSelf && !RectTransformUtility.RectangleContainsScreenPoint(m_hovered.transform as RectTransform, ZInput.mousePosition))
			{
				HideTooltip();
				return;
			}
			m_tooltip.transform.position = ZInput.mousePosition;
			Utils.ClampUIToScreen(m_tooltip.transform.GetChild(0).transform as RectTransform);
		}
	}

	private void OnDisable()
	{
		if (m_current == this)
		{
			HideTooltip();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		OnHoverStart(eventData.pointerEnter);
	}

	public void OnHoverStart(GameObject go)
	{
		if ((bool)m_current)
		{
			HideTooltip();
		}
		if (m_tooltip == null && (m_text != "" || m_topic != ""))
		{
			m_tooltip = Object.Instantiate(m_tooltipPrefab, base.transform.GetComponentInParent<Canvas>().transform);
			UpdateTextElements();
			Utils.ClampUIToScreen(m_tooltip.transform.GetChild(0).transform as RectTransform);
			m_hovered = go;
			m_current = this;
			m_tooltip.SetActive(value: false);
			m_showTimer = 0f;
		}
	}

	private void UpdateTextElements()
	{
		if (m_tooltip != null)
		{
			Transform transform = Utils.FindChild(m_tooltip.transform, "Text");
			if (transform != null)
			{
				transform.GetComponent<TMP_Text>().text = Localization.instance.Localize(m_text);
			}
			Transform transform2 = Utils.FindChild(m_tooltip.transform, "Topic");
			if (transform2 != null)
			{
				transform2.GetComponent<TMP_Text>().text = Localization.instance.Localize(m_topic);
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (m_current == this)
		{
			HideTooltip();
		}
	}

	public static void HideTooltip()
	{
		if ((bool)m_tooltip)
		{
			Object.Destroy(m_tooltip);
			m_current = null;
			m_tooltip = null;
			m_hovered = null;
		}
	}

	public void Set(string topic, string text, RectTransform anchor = null, Vector2 fixedPosition = default(Vector2))
	{
		m_anchor = anchor;
		m_fixedPosition = fixedPosition;
		if (topic == m_topic && text == m_text)
		{
			return;
		}
		m_topic = topic;
		m_text = text;
		if (m_current == this && m_tooltip != null)
		{
			UpdateTextElements();
		}
		else
		{
			if (!(m_selectable != null) || ZInput.instance == null)
			{
				return;
			}
			RectTransform obj = m_selectable.transform as RectTransform;
			Vector3 point = obj.InverseTransformPoint(ZInput.mousePosition);
			if (obj.rect.Contains(point))
			{
				List<RaycastResult> list = new List<RaycastResult>();
				PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
				pointerEventData.position = ZInput.mousePosition;
				EventSystem.current.RaycastAll(pointerEventData, list);
				if (list.Count > 0 && list[0].gameObject == m_selectable.gameObject)
				{
					OnHoverStart(m_selectable.gameObject);
				}
			}
		}
	}
}

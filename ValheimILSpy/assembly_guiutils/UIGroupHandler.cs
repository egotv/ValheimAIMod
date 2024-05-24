using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIGroupHandler : MonoBehaviour
{
	public GameObject m_defaultElement;

	[SerializeField]
	private GameObject m_rightStickSelectable;

	public GameObject m_enableWhenActiveAndGamepad;

	public int m_groupPriority;

	public bool m_setDefaultOnKBM;

	public bool m_resetActiveElementOnStateChange = true;

	private CanvasGroup m_canvasGroup;

	private bool m_userActive = true;

	private bool m_active = true;

	private static List<UIGroupHandler> m_groups = new List<UIGroupHandler>();

	public bool IsActive
	{
		get
		{
			return m_active;
		}
		private set
		{
			m_active = value;
			this.OnActiveStateChanged?.Invoke();
		}
	}

	public event Action OnActiveStateChanged;

	private void Awake()
	{
		m_groups.Add(this);
		m_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void OnDestroy()
	{
		m_groups.Remove(this);
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		if (IsActive)
		{
			IsActive = false;
			ResetActiveElement();
		}
	}

	private Selectable FindSelectable(GameObject root)
	{
		return root.GetComponentInChildren<Selectable>(includeInactive: false);
	}

	private bool IsHighestPriority()
	{
		if (!m_userActive)
		{
			return false;
		}
		foreach (UIGroupHandler group in m_groups)
		{
			if (!(group == this) && group.gameObject.activeInHierarchy && group.m_groupPriority > m_groupPriority)
			{
				return false;
			}
		}
		return true;
	}

	private void ResetActiveElement()
	{
		if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
		{
			return;
		}
		Selectable[] componentsInChildren = base.gameObject.GetComponentsInChildren<Selectable>(includeInactive: false);
		foreach (Selectable selectable in componentsInChildren)
		{
			if (EventSystem.current.currentSelectedGameObject == selectable.gameObject)
			{
				EventSystem.current.SetSelectedGameObject(null);
				break;
			}
		}
	}

	private void Update()
	{
		bool flag = IsHighestPriority();
		if (flag != IsActive)
		{
			if (m_resetActiveElementOnStateChange)
			{
				ResetActiveElement();
			}
			IsActive = flag;
		}
		if ((bool)m_canvasGroup)
		{
			m_canvasGroup.interactable = flag;
		}
		if ((bool)m_enableWhenActiveAndGamepad)
		{
			m_enableWhenActiveAndGamepad.SetActive(IsActive && (ZInput.IsGamepadActive() || m_setDefaultOnKBM));
		}
		if (!IsActive || !(m_defaultElement != null) || (!ZInput.IsGamepadActive() && !m_setDefaultOnKBM))
		{
			return;
		}
		float joyRightStickY = ZInput.GetJoyRightStickY();
		float joyLeftStickY = ZInput.GetJoyLeftStickY();
		bool flag2 = m_rightStickSelectable != null && joyLeftStickY > -0.1f && joyLeftStickY < 0.1f && (joyRightStickY > 0.1f || joyRightStickY < -0.1f);
		Selectable selectable = ((m_rightStickSelectable != null) ? FindSelectable(m_rightStickSelectable) : null);
		if (flag2)
		{
			if ((bool)selectable)
			{
				ZLog.Log("Activating right stick element " + m_rightStickSelectable.name);
				EventSystem.current.SetSelectedGameObject(selectable.gameObject);
				selectable.OnSelect(null);
			}
			return;
		}
		if (selectable != null && EventSystem.current.currentSelectedGameObject == selectable.gameObject)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
		if (!HaveSelectedObject() && !flag2)
		{
			Selectable selectable2 = FindSelectable(m_defaultElement);
			if ((bool)selectable2)
			{
				ZLog.Log("Activating default element " + m_defaultElement.name);
				EventSystem.current.SetSelectedGameObject(selectable2.gameObject);
				selectable2.OnSelect(null);
			}
		}
	}

	private bool HaveSelectedObject()
	{
		if (EventSystem.current.currentSelectedGameObject == null)
		{
			return false;
		}
		if (!EventSystem.current.currentSelectedGameObject.activeInHierarchy)
		{
			return false;
		}
		Selectable component = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
		if ((bool)component && !component.IsInteractable())
		{
			return false;
		}
		return true;
	}

	public void SetActive(bool active)
	{
		m_userActive = active;
		if (!m_userActive && (bool)m_enableWhenActiveAndGamepad)
		{
			m_enableWhenActiveAndGamepad.SetActive(value: false);
		}
	}
}

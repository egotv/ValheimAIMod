using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInputHint : MonoBehaviour
{
	[Serializable]
	public class InputLayoutElement
	{
		public GameObject m_hintObject;

		public List<InputLayout> m_enableForLayout = new List<InputLayout>();
	}

	public GameObject m_gamepadHint;

	public GameObject m_mouseKeyboardHint;

	private Button m_button;

	private UIGroupHandler m_group;

	[SerializeField]
	private UIGroupHandler alternativeGroupHandler;

	private Localize m_localize;

	public List<InputLayoutElement> m_inputLayoutSettings = new List<InputLayoutElement>();

	private void Start()
	{
		m_group = GetComponentInParent<UIGroupHandler>();
		m_button = GetComponent<Button>();
		m_localize = GetComponentInParent<Localize>();
		if ((bool)m_gamepadHint)
		{
			m_gamepadHint.gameObject.SetActive(value: false);
		}
		if ((bool)m_mouseKeyboardHint)
		{
			m_mouseKeyboardHint.gameObject.SetActive(value: false);
		}
		if ((bool)m_group)
		{
			m_group.OnActiveStateChanged += UpdateInputHints;
		}
		if ((bool)alternativeGroupHandler)
		{
			alternativeGroupHandler.OnActiveStateChanged += UpdateInputHints;
		}
		ZInput.OnInputLayoutChanged += HandleInputChanged;
		UpdateInputHints();
	}

	private void OnDestroy()
	{
		ZInput.OnInputLayoutChanged -= HandleInputChanged;
		if ((bool)m_group)
		{
			m_group.OnActiveStateChanged -= UpdateInputHints;
		}
		if ((bool)alternativeGroupHandler)
		{
			alternativeGroupHandler.OnActiveStateChanged -= UpdateInputHints;
		}
	}

	private void HandleInputChanged()
	{
		UpdateInputHints();
	}

	private void OnEnable()
	{
		UpdateInputHints();
	}

	private void UpdateInputHints()
	{
		bool flag = ((m_button == null || m_button.IsInteractable()) && (m_group == null || m_group.IsActive)) || (alternativeGroupHandler != null && alternativeGroupHandler.IsActive);
		UpdateVisiblityAndLayout(m_gamepadHint, flag && ZInput.IsGamepadActive());
		UpdateVisiblityAndLayout(m_mouseKeyboardHint, flag && ZInput.IsMouseActive());
		foreach (InputLayoutElement inputLayoutSetting in m_inputLayoutSettings)
		{
			if (inputLayoutSetting.m_hintObject.transform.parent.gameObject.activeInHierarchy)
			{
				inputLayoutSetting.m_hintObject.SetActive(inputLayoutSetting.m_enableForLayout.Contains(ZInput.InputLayout));
			}
		}
	}

	private void UpdateVisiblityAndLayout(GameObject go, bool condition)
	{
		if (go != null)
		{
			go.SetActive(condition);
			if ((bool)m_localize)
			{
				m_localize.RefreshLocalization();
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(go.transform as RectTransform);
		}
	}
}

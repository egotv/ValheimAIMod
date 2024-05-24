using System.Collections;
using System.Collections.Generic;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class KeyboardMouseSettings : SettingsBase
{
	[SerializeField]
	private UIGroupHandler m_groupHandler;

	[Header("Controls")]
	[SerializeField]
	private Slider m_mouseSensitivitySlider;

	[SerializeField]
	private TMP_Text m_mouseSensitivityText;

	[SerializeField]
	private Toggle m_invertMouse;

	[SerializeField]
	private Toggle m_quickPieceSelect;

	[SerializeField]
	private GameObject m_bindDialog;

	[SerializeField]
	private List<KeySetting> m_keys = new List<KeySetting>();

	[SerializeField]
	private Dictionary<string, ZInput.ButtonDef> m_oldBtnDefs = new Dictionary<string, ZInput.ButtonDef>();

	[SerializeField]
	private int m_keyRows = 13;

	[SerializeField]
	private int m_keyCols = 2;

	private float m_blockInputDelay;

	private KeySetting m_selectedKey;

	public override void FixBackButtonNavigation(Button backButton)
	{
		Button componentInChildren = m_keys[12].m_keyTransform.GetComponentInChildren<Button>();
		SetNavigation(componentInChildren, NavigationDirection.OnDown, backButton);
		SetNavigation(backButton, NavigationDirection.OnUp, componentInChildren);
	}

	public override void FixOkButtonNavigation(Button okButton)
	{
		Button componentInChildren = m_keys[25].m_keyTransform.GetComponentInChildren<Button>();
		SetNavigation(componentInChildren, NavigationDirection.OnDown, okButton);
		SetNavigation(okButton, NavigationDirection.OnUp, componentInChildren);
	}

	public override void LoadSettings()
	{
		m_oldBtnDefs.Clear();
		foreach (KeySetting key in m_keys)
		{
			ZInput.ButtonDef buttonDef = ZInput.instance.GetButtonDef(key.m_keyName);
			if (buttonDef != null)
			{
				m_oldBtnDefs[key.m_keyName] = new ZInput.ButtonDef(buttonDef);
			}
		}
		PlayerController.m_mouseSens = PlatformPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_invertMouse = PlatformPrefs.GetInt("InvertMouse") == 1;
		m_mouseSensitivitySlider.value = PlayerController.m_mouseSens;
		m_invertMouse.isOn = PlayerController.m_invertMouse;
		m_quickPieceSelect.isOn = PlatformPrefs.GetInt("QuickPieceSelect") == 1;
		OnMouseSensitivityChanged();
		m_bindDialog.SetActive(value: false);
		SetupKeys();
	}

	public override void ResetSettings()
	{
		foreach (KeyValuePair<string, ZInput.ButtonDef> oldBtnDef in m_oldBtnDefs)
		{
			ZInput.instance.ResetTo(oldBtnDef.Value);
		}
	}

	public override void SaveSettings()
	{
		PlatformPrefs.SetFloat("MouseSensitivity", m_mouseSensitivitySlider.value);
		PlatformPrefs.SetInt("InvertMouse", m_invertMouse.isOn ? 1 : 0);
		PlatformPrefs.SetInt("QuickPieceSelect", m_quickPieceSelect.isOn ? 1 : 0);
		PlayerController.m_mouseSens = m_mouseSensitivitySlider.value;
		PlayerController.m_invertMouse = m_invertMouse.isOn;
		Saved?.Invoke();
	}

	private void Update()
	{
		if (!m_bindDialog.activeSelf)
		{
			return;
		}
		m_blockInputDelay -= Time.unscaledDeltaTime;
		if (!(m_blockInputDelay >= 0f))
		{
			if (InvalidKeyBind())
			{
				m_bindDialog.SetActive(value: false);
				InvalidKeybindPopup();
			}
			else if (ZInput.instance.EndBindKey())
			{
				m_bindDialog.SetActive(value: false);
				UpdateBindings();
				StartCoroutine(DelayedKeyEnable());
			}
		}
	}

	private bool InvalidKeyBind()
	{
		KeyCode[] blockedButtons = m_selectedKey.m_blockedButtons;
		for (int i = 0; i < blockedButtons.Length; i++)
		{
			if (ZInput.GetKeyDown(blockedButtons[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void InvalidKeybindPopup()
	{
		string text = "$invalid_keybind_text";
		UnifiedPopup.Push(new WarningPopup("$invalid_keybind_header", text, delegate
		{
			UnifiedPopup.Pop();
			StartCoroutine(DelayedKeyEnable());
		}));
	}

	private IEnumerator DelayedKeyEnable()
	{
		if (!(base.gameObject == null))
		{
			yield return null;
			EnableKeys(enable: true);
			m_groupHandler.m_defaultElement = m_mouseSensitivitySlider.gameObject;
			Settings.instance.BlockNavigation(block: false);
		}
	}

	private void OnDestroy()
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<GuiButton>().onClick.RemoveAllListeners();
		}
		m_keys.Clear();
	}

	public void OnMouseSensitivityChanged()
	{
		m_mouseSensitivityText.text = Mathf.Round(m_mouseSensitivitySlider.value * 100f) + "%";
	}

	private void SetupKeys()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (KeySetting key in m_keys)
		{
			GuiButton componentInChildren = key.m_keyTransform.GetComponentInChildren<GuiButton>();
			componentInChildren.onClick.AddListener(delegate
			{
				OpenBindDialog(key);
			});
			if (num < m_keyRows - 1)
			{
				num3 = num2 * m_keyRows + num + 1;
				if (num3 < m_keys.Count)
				{
					GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
					SetNavigation(componentInChildren, NavigationDirection.OnDown, componentInChildren2);
				}
			}
			if (num > 0)
			{
				num3 = num2 * m_keyRows + num - 1;
				GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
				SetNavigation(componentInChildren, NavigationDirection.OnUp, componentInChildren2);
			}
			if (num2 > 0)
			{
				num3 = (num2 - 1) * m_keyRows + num;
				GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
				SetNavigation(componentInChildren, NavigationDirection.OnLeft, componentInChildren2);
			}
			if (num2 < m_keyCols - 1)
			{
				num3 = (num2 + 1) * m_keyRows + num;
				if (num3 < m_keys.Count)
				{
					GuiButton componentInChildren2 = m_keys[num3].m_keyTransform.GetComponentInChildren<GuiButton>();
					SetNavigation(componentInChildren, NavigationDirection.OnRight, componentInChildren2);
				}
			}
			num++;
			if (num % m_keyRows == 0)
			{
				num = 0;
				num2++;
			}
		}
		UpdateBindings();
	}

	private void EnableKeys(bool enable)
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<GuiButton>().interactable = enable;
		}
	}

	private void OpenBindDialog(KeySetting key)
	{
		ZLog.Log("Binding key " + key.m_keyName);
		m_selectedKey = key;
		Settings.instance.BlockNavigation(block: true);
		m_bindDialog.SetActive(value: true);
		m_blockInputDelay = 0.2f;
		m_groupHandler.m_defaultElement = EventSystem.current.currentSelectedGameObject;
		EventSystem.current.SetSelectedGameObject(m_bindDialog.gameObject);
		EnableKeys(enable: false);
		ZInput.instance.StartBindKey(key.m_keyName);
	}

	private void UpdateBindings()
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<TMP_Text>().text = Localization.instance.GetBoundKeyString(key.m_keyName, emptyStringOnMissing: true);
		}
	}

	public void ResetBindings()
	{
		ZInput.instance.Reset();
		UpdateBindings();
	}
}

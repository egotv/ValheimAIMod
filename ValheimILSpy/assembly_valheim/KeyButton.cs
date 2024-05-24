using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	private Button m_button;

	public TMP_Text m_toolTipLabel;

	public string m_toolTip;

	public WorldPresets m_preset;

	public List<string> m_keys;

	public void Awake()
	{
		m_button = GetComponentInParent<Button>();
		m_button.onClick.AddListener(OnClick);
	}

	private void Update()
	{
		if (ZInput.IsGamepadActive() && EventSystem.current.currentSelectedGameObject == m_button.gameObject)
		{
			UpdateTooltip();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		UpdateTooltip();
	}

	private void UpdateTooltip()
	{
		KeyUI.m_lastKeyUI = null;
		if ((bool)m_toolTipLabel)
		{
			m_toolTipLabel.text = Localization.instance.Localize(m_toolTip);
		}
	}

	public void SetKeys(World world)
	{
		foreach (string key in m_keys)
		{
			string text = key.ToLower();
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.SetGlobalKey(text);
			}
			else
			{
				if (world.m_startingGlobalKeys.Contains(text))
				{
					continue;
				}
				string text2 = text.Split(' ', StringSplitOptions.None)[0].ToLower();
				for (int num = world.m_startingGlobalKeys.Count - 1; num >= 0; num--)
				{
					if (world.m_startingGlobalKeys[num].Split(' ', StringSplitOptions.None)[0].ToLower() == text2)
					{
						world.m_startingGlobalKeys.RemoveAt(num);
					}
				}
				world.m_startingGlobalKeys.Add(text);
			}
		}
		ServerOptionsGUI.m_instance.OnPresetButton(this);
	}

	public bool TryMatch(List<string> keys)
	{
		for (int i = 0; i < keys.Count; i++)
		{
			keys[i] = keys[i].ToLower();
		}
		for (int j = 0; j < m_keys.Count; j++)
		{
			if (!keys.Contains(m_keys[j].ToLower()))
			{
				return false;
			}
		}
		return true;
	}

	public string GetName()
	{
		return base.gameObject.GetComponentInChildren<TMP_Text>().text;
	}

	private void OnClick()
	{
		ServerOptionsGUI.m_instance.OnPresetButton(this);
	}
}

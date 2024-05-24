using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeySlider : KeyUI
{
	[Serializable]
	public class SliderSetting
	{
		public string m_name;

		public string m_toolTip;

		public WorldModifierOption m_modifierValue;

		public List<string> m_keys = new List<string>();
	}

	private Slider m_slider;

	public TMP_Text m_nameLabel;

	public TMP_Text m_toolTipLabel;

	public string m_toolTip;

	public int m_defaultIndex;

	public WorldModifiers m_modifier;

	public List<SliderSetting> m_settings;

	[HideInInspector]
	public bool m_manualSet;

	private float m_lastToolTipUpdateValue = -1f;

	public static KeySlider m_lastActiveSlider;

	public void Awake()
	{
		m_slider = GetComponentInParent<Slider>();
		m_slider.maxValue = m_settings.Count - 1;
		m_slider.value = m_defaultIndex;
		m_slider.wholeNumbers = true;
		foreach (SliderSetting setting in m_settings)
		{
			for (int i = 0; i < setting.m_keys.Count; i++)
			{
				setting.m_keys[i] = setting.m_keys[i].ToLower();
			}
		}
		m_slider.onValueChanged.AddListener(OnValueChanged);
	}

	public override void Update()
	{
		if ((bool)m_nameLabel)
		{
			m_nameLabel.text = Localization.instance.Localize(Selected().m_name);
		}
		if (m_lastActiveSlider == this && KeyUI.m_lastKeyUI == this)
		{
			SetToolTip();
		}
		if (ZInput.IsGamepadActive() && EventSystem.current.currentSelectedGameObject != base.gameObject && m_lastActiveSlider == this)
		{
			m_lastActiveSlider = null;
		}
		base.Update();
	}

	public void OnValueChanged(float f)
	{
		OnValueChanged();
		m_manualSet = true;
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (m_lastActiveSlider != this || KeyUI.m_lastKeyUI != this)
		{
			m_lastActiveSlider = this;
			m_lastToolTipUpdateValue = -1f;
		}
		base.OnPointerEnter(eventData);
	}

	public void SetValue(WorldModifierOption value)
	{
		for (int i = 0; i < m_settings.Count; i++)
		{
			if (m_settings[i].m_modifierValue == value)
			{
				m_slider.value = i;
				OnValueChanged();
				return;
			}
		}
		Terminal.LogError($"Slider {m_modifier} missing value to set: {value}");
	}

	public WorldModifierOption GetValue()
	{
		return m_settings[(int)m_slider.value].m_modifierValue;
	}

	protected override void SetToolTip()
	{
		if ((bool)m_toolTipLabel && !(m_lastActiveSlider != this) && m_slider.value != m_lastToolTipUpdateValue)
		{
			m_lastToolTipUpdateValue = m_slider.value;
			string text = "";
			if (m_toolTip.Length > 0)
			{
				text = text + m_toolTip + "\n\n";
			}
			if (Selected().m_name.Length > 0 && Selected().m_toolTip.Length > 0)
			{
				text = text + "<color=orange>" + Selected().m_name + "</color>\n";
			}
			text += Selected().m_toolTip;
			if (text.Length > 0)
			{
				m_toolTipLabel.text = Localization.instance.Localize(text);
				m_toolTipLabel.gameObject.SetActive(value: true);
			}
		}
	}

	private SliderSetting Selected()
	{
		return m_settings[(int)m_slider.value];
	}

	public override void SetKeys(World world)
	{
		foreach (string key in m_settings[(int)m_slider.value].m_keys)
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
	}

	public override bool TryMatch(World world, bool checkAllKeys = false)
	{
		int num = 0;
		for (int i = 0; i < m_settings.Count; i++)
		{
			bool flag = false;
			SliderSetting sliderSetting = m_settings[i];
			if (sliderSetting.m_keys.Count == 0)
			{
				m_slider.value = (num = i);
				flag = true;
				if (world.m_startingGlobalKeys.Count == 0)
				{
					return true;
				}
				continue;
			}
			GlobalKeys gk;
			foreach (string key in sliderSetting.m_keys)
			{
				ZoneSystem.GetKeyValue(key, out var _, out gk);
				if (!world.m_startingGlobalKeys.Contains(key.ToLower()))
				{
					flag = true;
					break;
				}
			}
			if (checkAllKeys)
			{
				foreach (string startingGlobalKey in world.m_startingGlobalKeys)
				{
					if (Enum.TryParse<GlobalKeys>(ZoneSystem.GetKeyValue(startingGlobalKey, out var _, out gk), ignoreCase: true, out var result) && result < GlobalKeys.NonServerOption && !sliderSetting.m_keys.Contains(startingGlobalKey.ToLower()))
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				m_slider.value = i;
				return true;
			}
		}
		return false;
	}

	public override bool TryMatch(List<string> keys, out string label, bool setSlider = true)
	{
		for (int i = 0; i < keys.Count; i++)
		{
			keys[i] = keys[i].ToLower();
		}
		int num = 0;
		int num2 = -1;
		int num3 = 0;
		for (int j = 0; j < m_settings.Count; j++)
		{
			bool flag = false;
			SliderSetting sliderSetting = m_settings[j];
			if (sliderSetting.m_keys.Count == 0)
			{
				num = j;
				flag = true;
			}
			foreach (string key in sliderSetting.m_keys)
			{
				ZoneSystem.GetKeyValue(key, out var _, out var _);
				if (!keys.Contains(key.ToLower()))
				{
					flag = true;
					break;
				}
			}
			if (!flag && sliderSetting.m_keys.Count >= num3)
			{
				num2 = j;
				num3 = sliderSetting.m_keys.Count;
			}
		}
		if (num2 >= 0)
		{
			if (setSlider)
			{
				m_slider.value = num2;
			}
			label = m_modifier.GetDisplayString() + ": " + m_settings[num2].m_name;
			return true;
		}
		if (setSlider)
		{
			m_slider.value = num;
		}
		label = null;
		return false;
	}
}

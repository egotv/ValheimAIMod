using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyToggle : KeyUI
{
	private Toggle m_toggle;

	public TMP_Text m_toolTipLabel;

	public string m_toolTip;

	public bool m_defaultOn;

	public string m_enabledKey;

	public void Awake()
	{
		m_toggle = GetComponentInParent<Toggle>();
		m_toggle.isOn = m_defaultOn;
		m_toggle.onValueChanged.AddListener(delegate
		{
			OnValueChanged();
		});
	}

	public override void Update()
	{
		if (ZInput.IsGamepadActive() && EventSystem.current.currentSelectedGameObject == m_toggle.gameObject)
		{
			SetToolTip();
		}
		base.Update();
	}

	protected override void SetToolTip()
	{
		if ((bool)m_toolTipLabel)
		{
			m_toolTipLabel.text = Localization.instance.Localize(m_toolTip);
		}
	}

	public override void SetKeys(World world)
	{
		if (m_toggle.isOn)
		{
			string text = m_enabledKey.ToLower();
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.SetGlobalKey(text);
			}
			else if (!world.m_startingGlobalKeys.Contains(text))
			{
				world.m_startingGlobalKeys.Add(m_enabledKey.ToLower());
			}
		}
	}

	public override bool TryMatch(World world, bool checkAllKeys = false)
	{
		return m_toggle.isOn = world.m_startingGlobalKeys.Contains(m_enabledKey.ToLower());
	}

	public override bool TryMatch(List<string> keys, out string label, bool setToggle = true)
	{
		m_toggle.isOn = false;
		foreach (string key in keys)
		{
			if (key.ToLower() == m_enabledKey.ToLower())
			{
				if (setToggle)
				{
					m_toggle.isOn = true;
				}
				TMP_Text componentInChildren = GetComponentInChildren<TMP_Text>();
				label = (((object)componentInChildren != null) ? componentInChildren.text : m_enabledKey);
				return true;
			}
		}
		label = null;
		return false;
	}
}

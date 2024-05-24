using System;
using UnityEngine;

namespace Valheim.SettingsGui;

[Serializable]
public class KeySetting
{
	public string m_keyName = "";

	public RectTransform m_keyTransform;

	public KeyCode[] m_blockedButtons;
}

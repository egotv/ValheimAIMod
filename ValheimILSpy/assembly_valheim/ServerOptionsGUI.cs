using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ServerOptionsGUI : MonoBehaviour
{
	private static List<string> m_tempKeys = new List<string>();

	public static ServerOptionsGUI m_instance;

	public RectTransform m_toolTipPanel;

	public TMP_Text m_toolTipText;

	public GameObject m_modifiersRoot;

	public GameObject m_presetsRoot;

	public GameObject m_doneButton;

	private WorldPresets m_preset;

	private static KeyUI[] m_modifiers;

	private static KeyButton[] m_presets;

	private static Dictionary<WorldModifiers, List<WorldModifierOption>> m_modifierSetups = new Dictionary<WorldModifiers, List<WorldModifierOption>>();

	public void Awake()
	{
		m_instance = this;
		m_modifiers = m_modifiersRoot.transform.GetComponentsInChildren<KeyUI>();
		m_presets = m_presetsRoot.transform.GetComponentsInChildren<KeyButton>();
	}

	private void Update()
	{
		if (ZNet.instance != null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		m_toolTipPanel.gameObject.SetActive(m_toolTipText.text.Length > 0);
		if (EventSystem.current.currentSelectedGameObject == null)
		{
			EventSystem.current.SetSelectedGameObject(m_doneButton);
		}
	}

	public void ReadKeys(World world)
	{
		if (world != null)
		{
			KeyUI[] modifiers = m_modifiers;
			for (int i = 0; i < modifiers.Length; i++)
			{
				modifiers[i].TryMatch(world);
			}
		}
	}

	public void SetKeys(World world)
	{
		if (world == null)
		{
			return;
		}
		string text = "";
		bool flag = false;
		KeyUI[] modifiers = m_modifiers;
		foreach (KeyUI obj in modifiers)
		{
			obj.SetKeys(world);
			if (obj is KeySlider { m_manualSet: not false })
			{
				flag = true;
			}
		}
		if (flag)
		{
			modifiers = m_modifiers;
			for (int i = 0; i < modifiers.Length; i++)
			{
				if (modifiers[i] is KeySlider keySlider2)
				{
					if (text.Length > 0)
					{
						text += ":";
					}
					text = text + keySlider2.m_modifier.ToString().ToLower() + "_" + keySlider2.GetValue().ToString().ToLower();
				}
			}
		}
		else if (text.Length == 0 && m_preset > WorldPresets.Custom)
		{
			text = m_preset.ToString().ToLower();
		}
		if (text.Length > 0)
		{
			string text2 = GlobalKeys.Preset.ToString().ToLower() + " " + text;
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.SetGlobalKey(text2);
			}
			else
			{
				world.m_startingGlobalKeys.Add(text2);
			}
			Terminal.Log("Saving modifier preset: " + text);
		}
	}

	public void OnPresetButton(KeyButton button)
	{
		KeyUI[] modifiers = m_modifiers;
		foreach (KeyUI obj in modifiers)
		{
			obj.TryMatch(button.m_keys, out var _);
			if (obj is KeySlider keySlider)
			{
				keySlider.m_manualSet = false;
			}
		}
		m_preset = button.m_preset;
	}

	public void SetPreset(World world, string combinedString)
	{
		if (Enum.TryParse<WorldPresets>(combinedString, ignoreCase: true, out var result))
		{
			SetPreset(world, result);
			return;
		}
		string[] array = combinedString.Split(':', StringSplitOptions.None);
		foreach (string text in array)
		{
			string[] array2 = text.Split('_', StringSplitOptions.None);
			if (array2.Length == 2 && Enum.TryParse<WorldModifiers>(array2[0], ignoreCase: true, out var result2) && Enum.TryParse<WorldModifierOption>(array2[1], ignoreCase: true, out var result3))
			{
				SetPreset(world, result2, result3);
			}
			else
			{
				Terminal.LogError("Invalid preset string data '" + text + "'");
			}
		}
	}

	public void SetPreset(World world, WorldPresets preset)
	{
		Terminal.Log($"Setting World preset: {preset}");
		KeyButton[] presets = m_presets;
		foreach (KeyButton keyButton in presets)
		{
			if (keyButton.m_preset == preset)
			{
				keyButton.SetKeys(world);
				ZoneSystem.instance?.UpdateWorldRates();
				m_preset = preset;
				return;
			}
		}
		Terminal.LogError($"Missing settings for preset: {preset}");
	}

	public void SetPreset(World world, WorldModifiers preset, WorldModifierOption value)
	{
		Terminal.Log($"Setting WorldModifiers preset: '{preset}' to '{value}'");
		KeyUI[] modifiers = m_modifiers;
		for (int i = 0; i < modifiers.Length; i++)
		{
			if (modifiers[i] is KeySlider keySlider && keySlider.m_modifier == preset)
			{
				keySlider.SetValue(value);
				keySlider.SetKeys(world);
				ZoneSystem.instance?.UpdateWorldRates();
				return;
			}
		}
		Terminal.LogError($"Missing settings for preset: '{preset}' to '{value}'");
	}

	public void OnCustomValueChanged(KeyUI element)
	{
		m_preset = WorldPresets.Custom;
	}

	public static void Initizalize()
	{
		KeyUI[] modifiers = m_modifiers;
		for (int i = 0; i < modifiers.Length; i++)
		{
			if (!(modifiers[i] is KeySlider keySlider))
			{
				continue;
			}
			if (keySlider.m_modifier == WorldModifiers.Default)
			{
				ZLog.LogError($"Modifier {keySlider.m_nameLabel} is setup without a defined modifier");
			}
			List<WorldModifierOption> list = new List<WorldModifierOption>();
			m_modifierSetups[keySlider.m_modifier] = list;
			foreach (KeySlider.SliderSetting setting in keySlider.m_settings)
			{
				if (setting.m_modifierValue == WorldModifierOption.Default)
				{
					ZLog.LogError($"Modifier setting {setting.m_name} in {keySlider.m_nameLabel} is setup without a modifier option");
				}
				list.Add(setting.m_modifierValue);
			}
		}
	}

	public static string GetWorldModifierSummary(IEnumerable<string> keys, bool alwaysShort = false, string divider = ", ")
	{
		string text = "";
		string text2 = "";
		m_tempKeys.Clear();
		m_tempKeys.AddRange(keys);
		if (m_tempKeys.Count == 0)
		{
			return "";
		}
		if (m_presets == null)
		{
			ZLog.LogWarning("Can't get world modifier summary until prefab has been initiated.");
			return "Error!";
		}
		KeyButton keyButton = null;
		int num = 0;
		KeyButton[] presets = m_presets;
		foreach (KeyButton keyButton2 in presets)
		{
			if (keyButton2.m_preset != 0 && keyButton2.TryMatch(m_tempKeys) && keyButton2.m_keys.Count > num)
			{
				keyButton = keyButton2;
				num = keyButton2.m_keys.Count;
			}
		}
		if (keyButton != null)
		{
			text2 = keyButton.m_preset.GetDisplayString();
			foreach (string key in keyButton.m_keys)
			{
				m_tempKeys.Remove(key);
			}
		}
		KeyUI[] modifiers = m_modifiers;
		for (int i = 0; i < modifiers.Length; i++)
		{
			if (modifiers[i].TryMatch(m_tempKeys, out var label, setElement: false))
			{
				if (text.Length > 0)
				{
					text += divider;
				}
				text += label;
			}
		}
		if (alwaysShort)
		{
			if (text.Length > 0)
			{
				text2 = ((text2.Length <= 0) ? "$menu_modifier_custom" : (text2 + "+"));
			}
			return text2;
		}
		if (text.Length > 0)
		{
			if (text2.Length <= 0)
			{
				return text;
			}
			text2 = text2 + divider + text;
		}
		return text2;
	}

	public static bool TryConvertModifierKeysToCompactKVP<T>(ICollection<string> keys, out T result) where T : IDictionary<string, string>, new()
	{
		result = new T();
		foreach (string key in keys)
		{
			int num = key.IndexOf(' ');
			string text;
			string text2;
			if (num >= 0)
			{
				text = key.Substring(0, num);
				text2 = key.Substring(num + 1);
			}
			else
			{
				text = key;
				text2 = null;
			}
			if (!Enum.TryParse<GlobalKeys>(text, ignoreCase: true, out var result2) || result2.ToString().ToLower() != text.ToLower())
			{
				ZLog.LogError("Failed to parse key " + key + " as GlobalKeys!");
				return false;
			}
			int num5;
			if (result2 == GlobalKeys.Preset)
			{
				string text3 = "";
				int[] array = text2.AllIndicesOf(':');
				for (int i = 0; i < array.Length + 1; i++)
				{
					int num2 = 0;
					if (i > 0)
					{
						text3 += ":";
						num2 = array[i - 1] + 1;
					}
					else
					{
						num2 = 0;
					}
					int num3 = text2.IndexOf('_', num2);
					int num4 = ((i >= array.Length) ? text2.Length : array[i]);
					if (num3 >= num4)
					{
						ZLog.LogError("Failed to parse value " + text2 + "'s subkey as WorldModifiers and WorldModifierOption: separator index in wrong location!");
					}
					if (num3 < 0)
					{
						string text4 = text2.Substring(num2, num4 - num2);
						if (!Enum.TryParse<WorldPresets>(text4, ignoreCase: true, out var result3) || result3.ToString().ToLower() != text4.ToLower())
						{
							ZLog.LogError("Failed to parse value " + text2 + "'s subvalue " + text4 + " as WorldPresets: Value enum couldn't be parsed!");
							return false;
						}
						string text5 = text3;
						num5 = (int)result3;
						text3 = text5 + num5;
						continue;
					}
					string text6 = text2.Substring(num2, num3 - num2);
					string text7 = text2.Substring(num3 + 1, num4 - (num3 + 1));
					if (!Enum.TryParse<WorldModifiers>(text6, ignoreCase: true, out var result4) || result4.ToString().ToLower() != text6.ToLower())
					{
						ZLog.LogError("Failed to parse value " + text2 + "'s subkey " + text6 + " as WorldModifiers: Key enum couldn't be parsed!");
						return false;
					}
					if (!Enum.TryParse<WorldModifierOption>(text7, ignoreCase: true, out var result5) || result5.ToString().ToLower() != text7.ToLower())
					{
						ZLog.LogError("Failed to parse value " + text2 + "'s subvalue " + text7 + " as WorldModifierOption: Value enum couldn't be parsed!");
						return false;
					}
					string text8 = text3;
					num5 = (int)result4;
					string text9 = num5.ToString();
					num5 = (int)result5;
					text3 = text8 + text9 + "_" + num5;
				}
				text2 = text3;
			}
			num5 = (int)result2;
			result[num5.ToString()] = text2;
		}
		return true;
	}

	public static bool TryConvertCompactKVPToModifierKeys<T>(IDictionary<string, string> kvps, out T result) where T : ICollection<string>, new()
	{
		GlobalKeys[] array = new GlobalKeys[kvps.Count];
		string[] array2 = new string[kvps.Count];
		int num = 0;
		result = new T();
		foreach (KeyValuePair<string, string> kvp in kvps)
		{
			if (!int.TryParse(kvp.Key, out var result2))
			{
				ZLog.LogError("Failed to parse key " + kvp.Key + " as GlobalKeys: " + kvp.Key + " could not be parsed as an integer!");
				return false;
			}
			if (!Enum.IsDefined(typeof(GlobalKeys), result2))
			{
				ZLog.LogError(string.Format("Failed to parse key {0} as {1}: {2} is out of range!", kvp.Key, "GlobalKeys", result2));
			}
			array[num] = (GlobalKeys)result2;
			array2[num] = kvp.Value;
			num++;
		}
		for (int i = 0; i < array.Length; i++)
		{
			GlobalKeys globalKeys = array[i];
			string text = array2[i];
			if (string.IsNullOrEmpty(text))
			{
				result.Add(globalKeys.ToString());
				continue;
			}
			if (globalKeys == GlobalKeys.Preset)
			{
				string text2 = "";
				int[] array3 = array2[i].AllIndicesOf(':');
				for (int j = 0; j < array3.Length + 1; j++)
				{
					int num2 = 0;
					if (j > 0)
					{
						text2 += ":";
						num2 = array3[j - 1] + 1;
					}
					else
					{
						num2 = 0;
					}
					int num3 = text.IndexOf('_', num2);
					int num4 = ((j >= array3.Length) ? text.Length : array3[j]);
					if (num3 >= num4)
					{
						ZLog.LogError("Failed to parse value " + text + "'s subkey as WorldModifiers and WorldModifierOption: separator index in wrong location!");
					}
					if (num3 < 0)
					{
						string text3 = text.Substring(num2, num4 - num2);
						if (!int.TryParse(text3, out var result3))
						{
							ZLog.LogError("Failed to parse value " + text3 + " as WorldPresets: " + text3 + " could not be parsed as an integer!");
							return false;
						}
						if (!Enum.IsDefined(typeof(WorldPresets), result3))
						{
							ZLog.LogError(string.Format("Failed to parse value {0} as {1}: {2} is out of range!", text3, "WorldPresets", result3));
						}
						string text4 = text2;
						WorldPresets worldPresets = (WorldPresets)result3;
						text2 = text4 + worldPresets;
						continue;
					}
					string text5 = text.Substring(num2, num3 - num2);
					string text6 = text.Substring(num3 + 1, num4 - (num3 + 1));
					if (!int.TryParse(text5, out var result4))
					{
						ZLog.LogError("Failed to parse value " + text5 + " as WorldModifiers: " + text5 + " could not be parsed as an integer!");
						return false;
					}
					if (!Enum.IsDefined(typeof(WorldModifiers), result4))
					{
						ZLog.LogError(string.Format("Failed to parse value {0} as {1}: {2} is out of range!", text5, "WorldModifiers", result4));
					}
					if (!int.TryParse(text6, out var result5))
					{
						ZLog.LogError("Failed to parse value " + text6 + " as WorldModifierOption: " + text6 + " could not be parsed as an integer!");
						return false;
					}
					if (!Enum.IsDefined(typeof(WorldModifierOption), result5))
					{
						ZLog.LogError(string.Format("Failed to parse value {0} as {1}: {2} is out of range!", text6, "WorldModifierOption", result5));
					}
					string text7 = text2;
					WorldModifiers worldModifiers = (WorldModifiers)result4;
					string text8 = worldModifiers.ToString();
					WorldModifierOption worldModifierOption = (WorldModifierOption)result5;
					text2 = text7 + text8 + "_" + worldModifierOption;
				}
				text = text2;
			}
			result.Add(array[i].ToString() + " " + text);
		}
		return true;
	}

	private static bool TryMatch(List<string> keys, List<string> others)
	{
		if (others.Count != keys.Count)
		{
			return false;
		}
		for (int i = 0; i < keys.Count; i++)
		{
			keys[i] = keys[i].ToLower();
		}
		for (int j = 0; j < others.Count; j++)
		{
			if (!keys.Contains(others[j].ToLower()))
			{
				return false;
			}
		}
		return true;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Localization
{
	private Dictionary<Text, string> textStrings = new Dictionary<Text, string>();

	private Dictionary<TMP_Text, string> textMeshStrings = new Dictionary<TMP_Text, string>();

	private readonly StringBuilder m_stringBuilder = new StringBuilder();

	private static Localization m_instance;

	private static LocalizationSettings m_localizationSettings;

	public static Action OnLanguageChange;

	private char[] m_endChars = " (){}[]+-!?/\\\\&%,.:-=<>\n".ToCharArray();

	private Dictionary<string, string> m_translations = new Dictionary<string, string>();

	private List<string> m_languages = new List<string>();

	private readonly LRUCache<string> m_cache = new LRUCache<string>(100);

	public static Localization instance
	{
		get
		{
			if (m_instance == null)
			{
				Initialize();
			}
			return m_instance;
		}
	}

	private static void Initialize()
	{
		m_localizationSettings = Resources.Load<LocalizationSettings>("LocalizationSettings");
		if (m_instance == null)
		{
			m_instance = new Localization();
		}
	}

	private Localization()
	{
		m_languages = LoadLanguages();
		SetupLanguage("English");
		string @string = PlayerPrefs.GetString("language", "");
		if (@string != "")
		{
			SetupLanguage(@string);
		}
		GuiInputField.Localize = Localize;
	}

	public void SetLanguage(string language)
	{
		if (PlayerPrefs.GetString("language") != language)
		{
			PlayerPrefs.SetString("language", language);
			Clear();
			SetupLanguage(language);
			OnLanguageChange?.Invoke();
		}
	}

	public string GetSelectedLanguage()
	{
		return PlayerPrefs.GetString("language", "English");
	}

	public string GetNextLanguage(string lang)
	{
		for (int i = 0; i < m_languages.Count; i++)
		{
			if (m_languages[i] == lang)
			{
				if (i + 1 < m_languages.Count)
				{
					return m_languages[i + 1];
				}
				return m_languages[0];
			}
		}
		return m_languages[0];
	}

	public string GetPrevLanguage(string lang)
	{
		for (int i = 0; i < m_languages.Count; i++)
		{
			if (m_languages[i] == lang)
			{
				if (i - 1 >= 0)
				{
					return m_languages[i - 1];
				}
				return m_languages[m_languages.Count - 1];
			}
		}
		return m_languages[0];
	}

	public void Localize(Transform root)
	{
		Text[] componentsInChildren = root.gameObject.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren)
		{
			string text2 = text.text;
			text.text = Localize(text.text);
			if (text2 != text.text)
			{
				textStrings[text] = text2;
			}
		}
		TMP_Text[] componentsInChildren2 = root.gameObject.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren2)
		{
			string text3 = tMP_Text.text;
			tMP_Text.text = Localize(tMP_Text.text);
			if (text3 != tMP_Text.text)
			{
				textMeshStrings[tMP_Text] = text3;
			}
		}
	}

	public void RemoveTextFromCache(Text text)
	{
		if (textStrings.ContainsKey(text))
		{
			textStrings.Remove(text);
		}
	}

	public void RemoveTextFromCache(TMP_Text text)
	{
		if (textMeshStrings.ContainsKey(text))
		{
			textMeshStrings.Remove(text);
		}
	}

	public void ReLocalizeVisible(Transform root)
	{
		Text[] componentsInChildren = root.gameObject.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren)
		{
			if (text.gameObject.activeInHierarchy && textStrings.TryGetValue(text, out var value))
			{
				text.text = Localize(value);
			}
		}
		TMP_Text[] componentsInChildren2 = root.gameObject.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren2)
		{
			if (tMP_Text.gameObject.activeInHierarchy && textMeshStrings.TryGetValue(tMP_Text, out var value2))
			{
				tMP_Text.text = Localize(value2);
			}
		}
	}

	public void ReLocalizeAll(Transform root)
	{
		Text[] componentsInChildren = root.gameObject.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren)
		{
			if (textStrings.TryGetValue(text, out var value))
			{
				text.text = Localize(value);
			}
		}
		TMP_Text[] componentsInChildren2 = root.gameObject.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren2)
		{
			if (textMeshStrings.TryGetValue(tMP_Text, out var value2))
			{
				tMP_Text.text = Localize(value2);
			}
		}
	}

	public string Localize(string text, params string[] words)
	{
		string text2 = Localize(text);
		return InsertWords(text2, words);
	}

	private string InsertWords(string text, string[] words)
	{
		for (int i = 0; i < words.Length; i++)
		{
			string newValue = words[i];
			text = text.Replace("$" + (i + 1), newValue);
		}
		return text;
	}

	public string Localize(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (m_cache.TryGet(text, out var translated))
		{
			return translated;
		}
		m_stringBuilder.Clear();
		int num = 0;
		string word;
		int wordStart;
		int wordEnd;
		while (FindNextWord(text, num, out word, out wordStart, out wordEnd))
		{
			m_stringBuilder.Append(text.Substring(num, wordStart - num));
			m_stringBuilder.Append(Translate(word));
			num = wordEnd;
		}
		m_stringBuilder.Append(text.Substring(num));
		translated = m_stringBuilder.ToString();
		m_cache.Put(text, translated);
		return translated;
	}

	private bool FindNextWord(string text, int startIndex, out string word, out int wordStart, out int wordEnd)
	{
		if (startIndex >= text.Length - 1)
		{
			word = null;
			wordStart = -1;
			wordEnd = -1;
			return false;
		}
		wordStart = text.IndexOf('$', startIndex);
		if (wordStart != -1)
		{
			int num = text.IndexOfAny(m_endChars, wordStart);
			if (num != -1)
			{
				word = text.Substring(wordStart + 1, num - wordStart - 1);
				wordEnd = num;
			}
			else
			{
				word = text.Substring(wordStart + 1);
				wordEnd = text.Length;
			}
			return true;
		}
		word = null;
		wordEnd = -1;
		return false;
	}

	private string Translate(string word)
	{
		if (word.CustomStartsWith("KEY_"))
		{
			string text = word.Substring(4);
			if (ZInput.IsGamepadActive())
			{
				string boundKeyString = GetBoundKeyString("Joy" + text, emptyStringOnMissing: true);
				if (boundKeyString.Length > 0)
				{
					return boundKeyString;
				}
			}
			return GetBoundKeyString(text);
		}
		if (m_translations.TryGetValue(word, out var value))
		{
			return value;
		}
		return "[" + word + "]";
	}

	public string GetBoundKeyString(string bindingName, bool emptyStringOnMissing = false)
	{
		string boundKeyString = ZInput.instance.GetBoundKeyString(bindingName, emptyStringOnMissing);
		if (boundKeyString.Length > 0 && boundKeyString[0] == '$' && m_translations.TryGetValue(boundKeyString.Substring(1), out var value))
		{
			return value;
		}
		return boundKeyString;
	}

	private void AddWord(string key, string text)
	{
		m_translations.Remove(key);
		m_translations.Add(key, text);
	}

	private void Clear()
	{
		m_translations.Clear();
	}

	private string StripCitations(string s)
	{
		if (s.CustomStartsWith("\""))
		{
			s = s.Remove(0, 1);
			if (s.CustomEndsWith("\""))
			{
				s = s.Remove(s.Length - 1, 1);
			}
		}
		return s;
	}

	public bool SetupLanguage(string language)
	{
		bool flag = true;
		for (int i = 0; i < m_localizationSettings.Localizations.Count; i++)
		{
			TextAsset textAsset = m_localizationSettings.Localizations[i];
			flag &= LoadCSV(textAsset, language);
			if (textAsset == null || !flag)
			{
				ZLog.Log(string.Format("Failed to load language file #{0} {1}", i, (textAsset != null) ? (" - " + textAsset.name) : ""));
			}
			else
			{
				ZLog.Log($"Loaded localization file #{i} - '{textAsset.name}' language: '{language}'");
			}
		}
		return flag;
	}

	public bool LoadCSV(TextAsset file, string language)
	{
		if (file == null)
		{
			return false;
		}
		StringReader stringReader = new StringReader(file.text);
		string[] array = stringReader.ReadLine().Split(',', StringSplitOptions.None);
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (StripCitations(array[i]) == language)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			ZLog.LogWarning("Failed to find language '" + language + "' in file '" + file.name + "'");
			return false;
		}
		foreach (List<string> item in DoQuoteLineSplit(stringReader))
		{
			if (item.Count == 0)
			{
				continue;
			}
			string text = item[0];
			if (!text.CustomStartsWith("//") && text.Length != 0 && item.Count > num)
			{
				string text2 = item[num];
				if (string.IsNullOrEmpty(text2) || text2[0] == '\r')
				{
					text2 = item[1];
				}
				AddWord(text, text2);
			}
		}
		return true;
	}

	private List<List<string>> DoQuoteLineSplit(StringReader reader)
	{
		List<List<string>> list = new List<List<string>>();
		List<string> list2 = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		while (true)
		{
			int num = reader.Read();
			switch (num)
			{
			case -1:
				list2.Add(stringBuilder.ToString());
				list.Add(list2);
				return list;
			case 34:
				flag = !flag;
				continue;
			case 44:
				if (!flag)
				{
					list2.Add(stringBuilder.ToString());
					stringBuilder.Length = 0;
					continue;
				}
				break;
			}
			if (num == 10 && !flag)
			{
				list2.Add(stringBuilder.ToString());
				stringBuilder.Length = 0;
				list.Add(list2);
				list2 = new List<string>();
			}
			else
			{
				stringBuilder.Append((char)num);
			}
		}
	}

	public string TranslateSingleId(string locaId, string language)
	{
		locaId = locaId.Replace("$", "");
		string text = "";
		for (int i = 0; i < m_localizationSettings.Localizations.Count; i++)
		{
			StringReader stringReader = new StringReader(m_localizationSettings.Localizations[i].text);
			string[] array = stringReader.ReadLine().Split(',', StringSplitOptions.None);
			int num = -1;
			for (int j = 0; j < array.Length; j++)
			{
				if (StripCitations(array[j]) == language)
				{
					num = j;
					break;
				}
			}
			Debug.Log($"Column: {num}");
			List<List<string>> list = DoQuoteLineSplit(stringReader);
			Debug.Log($"Lines: {list.Count}");
			foreach (List<string> item in list)
			{
				if (item.Count != 0 && !(item[0] != locaId))
				{
					text = item[num];
					Debug.Log("Translation1: " + text);
					if (string.IsNullOrEmpty(text) || text[0] == '\r')
					{
						text = item[1];
					}
					Debug.Log("Translation2: " + text);
					break;
				}
			}
		}
		Debug.Log("Translation3: " + text);
		return text;
	}

	public List<string> GetLanguages()
	{
		return m_languages;
	}

	private List<string> LoadLanguages()
	{
		string[] array = new StringReader((Resources.Load("localization", typeof(TextAsset)) as TextAsset).text).ReadLine().Split(',', StringSplitOptions.None);
		List<string> list = new List<string>();
		for (int i = 1; i < array.Length; i++)
		{
			string item = StripCitations(array[i]);
			list.Add(item);
		}
		return list;
	}
}

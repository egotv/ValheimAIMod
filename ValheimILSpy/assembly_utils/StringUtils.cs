using System;
using System.Collections.Generic;

public static class StringUtils
{
	public static string EncodeStringListAsString(IReadOnlyList<string> stringsToEncode, bool encloseInQuotes = true)
	{
		int num = 0;
		for (int i = 0; i < stringsToEncode.Count; i++)
		{
			num += stringsToEncode[i].Length;
			num += ((!encloseInQuotes) ? 1 : 3);
		}
		List<char> list = new List<char>(num);
		for (int j = 0; j < stringsToEncode.Count; j++)
		{
			string text = stringsToEncode[j];
			if (j != 0)
			{
				list.Add(',');
			}
			if (encloseInQuotes)
			{
				list.Add('"');
			}
			for (int k = 0; k < text.Length; k++)
			{
				if (text[k] == '"' || text[k] == '\\' || (!encloseInQuotes && text[k] == ','))
				{
					list.Add('\\');
				}
				list.Add(text[k]);
			}
			if (encloseInQuotes)
			{
				list.Add('"');
			}
		}
		return new string(list.ToArray());
	}

	public static bool TryDecodeStringAsICollection<T>(string encodedString, out T decodedCollection) where T : ICollection<string>, new()
	{
		decodedCollection = new T();
		if (encodedString == null || encodedString.Length == 0)
		{
			return true;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		char[] array = new char[Math.Max(encodedString.Length, 0)];
		int num = 0;
		for (int i = 0; i < encodedString.Length; i++)
		{
			if (flag)
			{
				if (flag3)
				{
					array[num++] = encodedString[i];
					flag3 = false;
					continue;
				}
				switch (encodedString[i])
				{
				case '\\':
					flag3 = true;
					break;
				case '"':
					if (flag2)
					{
						array[num++] = encodedString[i];
					}
					else
					{
						flag = false;
					}
					break;
				case ',':
					if (flag2)
					{
						flag = false;
						flag2 = false;
						i--;
					}
					else
					{
						array[num++] = encodedString[i];
					}
					break;
				default:
					array[num++] = encodedString[i];
					break;
				}
			}
			else
			{
				if (char.IsWhiteSpace(encodedString[i]))
				{
					continue;
				}
				switch (encodedString[i])
				{
				case '"':
					if (num != 0)
					{
						return false;
					}
					flag = true;
					break;
				case ',':
					decodedCollection.Add((num > 0) ? new string(array, 0, num) : "");
					num = 0;
					break;
				default:
					flag = true;
					flag2 = true;
					i--;
					break;
				}
			}
		}
		if (num > 0)
		{
			decodedCollection.Add(new string(array, 0, num));
		}
		return true;
	}

	public static string EncodeDictionaryAsString(IDictionary<string, string> dictionaryToEncode, bool encloseInQuotes = true)
	{
		KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[dictionaryToEncode.Count];
		uint num = 0u;
		foreach (KeyValuePair<string, string> item in dictionaryToEncode)
		{
			array[num] = item;
			num++;
		}
		return EncodeKVPArrayAsString(array, encloseInQuotes);
	}

	public static string EncodeKVPArrayAsString(IReadOnlyList<KeyValuePair<string, string>> keyValuePairsToEncode, bool encloseInQuotes = true)
	{
		int num = 0;
		for (int i = 0; i < keyValuePairsToEncode.Count; i++)
		{
			num += keyValuePairsToEncode[i].Key.Length;
			if (keyValuePairsToEncode[i].Value != null)
			{
				num += keyValuePairsToEncode[i].Value.Length;
				num += (encloseInQuotes ? 6 : 2);
			}
			else
			{
				num += ((!encloseInQuotes) ? 1 : 3);
			}
		}
		List<char> list = new List<char>(num);
		for (int j = 0; j < keyValuePairsToEncode.Count; j++)
		{
			string key = keyValuePairsToEncode[j].Key;
			string value = keyValuePairsToEncode[j].Value;
			if (j != 0)
			{
				list.Add(',');
			}
			if (encloseInQuotes)
			{
				list.Add('"');
			}
			for (int k = 0; k < key.Length; k++)
			{
				if (key[k] == '"' || key[k] == '\\' || (!encloseInQuotes && (key[k] == ',' || key[k] == '=')))
				{
					list.Add('\\');
				}
				list.Add(key[k]);
			}
			if (encloseInQuotes)
			{
				list.Add('"');
			}
			if (value == null)
			{
				continue;
			}
			list.Add('=');
			if (encloseInQuotes)
			{
				list.Add('"');
			}
			for (int l = 0; l < value.Length; l++)
			{
				if (value[l] == '"' || value[l] == '\\' || (!encloseInQuotes && (value[l] == ',' || value[l] == '=')))
				{
					list.Add('\\');
				}
				list.Add(value[l]);
			}
			if (encloseInQuotes)
			{
				list.Add('"');
			}
		}
		return new string(list.ToArray());
	}

	public static bool TryDecodeStringAsIDictionary<T>(string encodedString, out T decodedDictionary) where T : IDictionary<string, string>, new()
	{
		decodedDictionary = new T();
		if (encodedString == null || encodedString.Length == 0)
		{
			return true;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		char[] array = new char[Math.Max(encodedString.Length, 0)];
		int num = 0;
		string text = null;
		string text2 = null;
		for (int i = 0; i < encodedString.Length; i++)
		{
			if (flag)
			{
				if (flag3)
				{
					array[num++] = encodedString[i];
					flag3 = false;
					continue;
				}
				switch (encodedString[i])
				{
				case '\\':
					flag3 = true;
					break;
				case '"':
					if (flag2)
					{
						array[num++] = encodedString[i];
					}
					else
					{
						flag = false;
					}
					break;
				case ',':
				case '=':
					if (flag2)
					{
						flag = false;
						flag2 = false;
						i--;
					}
					else
					{
						array[num++] = encodedString[i];
					}
					break;
				default:
					array[num++] = encodedString[i];
					break;
				}
			}
			else
			{
				if (char.IsWhiteSpace(encodedString[i]))
				{
					continue;
				}
				switch (encodedString[i])
				{
				case '"':
					if (num != 0)
					{
						return false;
					}
					flag = true;
					break;
				case '=':
					if (text != null || text2 != null)
					{
						return false;
					}
					text = ((num > 0) ? new string(array, 0, num) : "");
					num = 0;
					break;
				case ',':
				{
					if (text2 != null)
					{
						return false;
					}
					string text3 = ((num > 0) ? new string(array, 0, num) : "");
					if (text == null)
					{
						text = text3;
					}
					else
					{
						text2 = text3;
					}
					num = 0;
					decodedDictionary.Add(text, text2);
					text = null;
					text2 = null;
					break;
				}
				default:
					flag = true;
					flag2 = true;
					i--;
					break;
				}
			}
		}
		if (flag2 && flag)
		{
			string text4 = ((num > 0) ? new string(array, 0, num) : "");
			if (text == null)
			{
				text = text4;
			}
			else
			{
				text2 = text4;
			}
			num = 0;
		}
		if (text != null && text2 == null)
		{
			string text5;
			if (num == 0)
			{
				text5 = "";
			}
			else
			{
				text5 = new string(array, 0, num);
				num = 0;
			}
			text2 = text5;
			decodedDictionary.Add(text, text2);
			text = null;
			text2 = null;
		}
		return true;
	}
}

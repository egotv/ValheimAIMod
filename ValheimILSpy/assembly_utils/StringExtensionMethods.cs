using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class StringExtensionMethods
{
	private static List<string> s_staticIntToStrings;

	private const int c_NumFastStrings = 256;

	private static readonly bool s_stringInitDone = InitIntToStrings();

	private static bool InitIntToStrings()
	{
		s_staticIntToStrings = new List<string>(257);
		for (int i = 0; i <= 256; i++)
		{
			s_staticIntToStrings.Add(i.ToString());
		}
		return true;
	}

	public static string ToFastString(this int i)
	{
		if (i >= 0 && i <= 256)
		{
			return s_staticIntToStrings[i];
		}
		return i.ToString();
	}

	public static int GetStableHashCode(this string str)
	{
		int num = 5381;
		int num2 = num;
		for (int i = 0; i < str.Length && str[i] != 0; i += 2)
		{
			num = ((num << 5) + num) ^ str[i];
			if (i == str.Length - 1 || str[i + 1] == '\0')
			{
				break;
			}
			num2 = ((num2 << 5) + num2) ^ str[i + 1];
		}
		return num + num2 * 1566083941;
	}

	public static int[] AllIndicesOf(this string thisString, string substring)
	{
		List<int> list = new List<int>();
		if (string.IsNullOrEmpty(substring))
		{
			return list.ToArray();
		}
		for (int i = 0; i < thisString.Length - substring.Length + 1; i++)
		{
			bool flag = true;
			for (int j = 0; j < substring.Length; j++)
			{
				if (thisString[i + j] != substring[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				list.Add(i);
			}
		}
		return list.ToArray();
	}

	public static int[] AllIndicesOf(this string thisString, char target)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < thisString.Length; i++)
		{
			if (thisString[i] == target)
			{
				list.Add(i);
			}
		}
		return list.ToArray();
	}

	public static string RemoveRichTextTags(this string text)
	{
		return Regex.Replace(text, "<[\\/a-zA-Z0-9= \"\\\"''#;:()$_-]*?>", string.Empty);
	}
}

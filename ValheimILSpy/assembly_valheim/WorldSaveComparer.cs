using System.Collections.Generic;

public class WorldSaveComparer : IComparer<string>
{
	public int Compare(string x, string y)
	{
		bool flag = true;
		int num = 0;
		if (!SaveSystem.GetSaveInfo(x, out var saveName, out var saveFileType, out var actualFileEnding, out var timestamp))
		{
			num++;
			flag = false;
		}
		if (!SaveSystem.GetSaveInfo(y, out saveName, out saveFileType, out var actualFileEnding2, out timestamp))
		{
			num--;
			flag = false;
		}
		if (!flag)
		{
			return num;
		}
		if (actualFileEnding == ".fwl")
		{
			num--;
		}
		else if (actualFileEnding != ".db")
		{
			num++;
		}
		if (actualFileEnding2 == ".fwl")
		{
			num++;
		}
		else if (actualFileEnding2 != ".db")
		{
			num--;
		}
		return num;
	}
}

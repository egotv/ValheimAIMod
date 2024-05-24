using System;
using System.Collections.Generic;

public class SaveFileComparer : IComparer<SaveFile>
{
	public int Compare(SaveFile x, SaveFile y)
	{
		return DateTime.Compare(y.LastModified, x.LastModified);
	}
}

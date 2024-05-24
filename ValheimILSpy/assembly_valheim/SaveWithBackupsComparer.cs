using System;
using System.Collections.Generic;

public class SaveWithBackupsComparer : IComparer<SaveWithBackups>
{
	public int Compare(SaveWithBackups x, SaveWithBackups y)
	{
		if (x.IsDeleted || y.IsDeleted)
		{
			return 0 + (x.IsDeleted ? (-1) : 0) + (y.IsDeleted ? 1 : 0);
		}
		return DateTime.Compare(y.PrimaryFile.LastModified, x.PrimaryFile.LastModified);
	}
}

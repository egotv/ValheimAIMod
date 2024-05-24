using System;
using System.Collections.Generic;

public class SaveFile
{
	private List<string> m_paths;

	public readonly FileHelpers.FileSource m_source;

	private Action m_modifiedCallback;

	private bool m_isDirty;

	private string m_fileName;

	public string PathPrimary
	{
		get
		{
			EnsureSorted();
			return m_paths[0];
		}
	}

	public string[] PathsAssociated
	{
		get
		{
			EnsureSorted();
			string[] array = new string[m_paths.Count - 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = m_paths[i + 1];
			}
			return array;
		}
	}

	public string[] AllPaths
	{
		get
		{
			EnsureSorted();
			return m_paths.ToArray();
		}
	}

	public string FileName
	{
		get
		{
			if (m_fileName == null)
			{
				string pathPrimary = PathPrimary;
				if (!SaveSystem.GetSaveInfo(pathPrimary, out var _, out var _, out var actualFileEnding, out var _))
				{
					m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
					return m_fileName;
				}
				switch (ParentSaveWithBackups.ParentSaveCollection.m_dataType)
				{
				case SaveDataType.World:
					if (actualFileEnding != ".fwl" && actualFileEnding != ".db")
					{
						m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
						return m_fileName;
					}
					break;
				case SaveDataType.Character:
					if (actualFileEnding != ".fch")
					{
						m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
						return m_fileName;
					}
					break;
				}
				string text = SaveSystem.RemoveDirectoryPart(pathPrimary);
				int num = text.LastIndexOf(actualFileEnding);
				if (num < 0)
				{
					m_fileName = text;
				}
				else
				{
					m_fileName = text.Remove(num, actualFileEnding.Length);
				}
			}
			return m_fileName;
		}
	}

	public DateTime LastModified { get; private set; } = DateTime.MinValue;


	public ulong Size { get; private set; }

	public SaveWithBackups ParentSaveWithBackups { get; private set; }

	public SaveFile(string path, FileHelpers.FileSource source, SaveWithBackups parentSaveWithBackups, Action modifiedCallback)
	{
		m_paths = new List<string>();
		m_source = source;
		ParentSaveWithBackups = parentSaveWithBackups;
		m_modifiedCallback = modifiedCallback;
		AddAssociatedFile(path);
	}

	public SaveFile(string[] paths, FileHelpers.FileSource source, SaveWithBackups parentSaveWithBackups, Action modifiedCallback)
	{
		m_paths = new List<string>();
		m_source = source;
		ParentSaveWithBackups = parentSaveWithBackups;
		m_modifiedCallback = modifiedCallback;
		AddAssociatedFiles(paths);
	}

	public SaveFile(FilePathAndSource pathAndSource, SaveWithBackups inSaveFile, Action modifiedCallback)
	{
		m_paths = new List<string>();
		m_source = pathAndSource.source;
		Size = 0uL;
		ParentSaveWithBackups = inSaveFile;
		m_modifiedCallback = modifiedCallback;
		AddAssociatedFile(pathAndSource.path);
	}

	public void AddAssociatedFile(string path)
	{
		m_paths.Add(path);
		Size += FileHelpers.GetFileSize(path, m_source);
		if (!SaveSystem.GetSaveInfo(path, out var _, out var _, out var _, out var timestamp) || !timestamp.HasValue)
		{
			timestamp = FileHelpers.GetLastWriteTime(path, m_source);
		}
		if (timestamp.Value > LastModified)
		{
			LastModified = timestamp.Value;
		}
		OnModified();
	}

	public void AddAssociatedFiles(string[] paths)
	{
		m_paths.AddRange(paths);
		for (int i = 0; i < paths.Length; i++)
		{
			Size += FileHelpers.GetFileSize(paths[i], m_source);
			if (!SaveSystem.GetSaveInfo(paths[i], out var _, out var _, out var _, out var timestamp) || !timestamp.HasValue)
			{
				timestamp = FileHelpers.GetLastWriteTime(paths[i], m_source);
			}
			if (timestamp.Value > LastModified)
			{
				LastModified = timestamp.Value;
			}
		}
		OnModified();
	}

	public override bool Equals(object obj)
	{
		if (!(obj is SaveFile saveFile))
		{
			return false;
		}
		if (m_source != saveFile.m_source)
		{
			return false;
		}
		string[] allPaths = AllPaths;
		string[] allPaths2 = saveFile.AllPaths;
		if (allPaths.Length != allPaths2.Length)
		{
			return false;
		}
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (allPaths[i] != allPaths2[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		string[] allPaths = AllPaths;
		int num = 878520832;
		num = num * -1521134295 + allPaths.Length.GetHashCode();
		for (int i = 0; i < allPaths.Length; i++)
		{
			num = num * -1521134295 + EqualityComparer<string>.Default.GetHashCode(allPaths[i]);
		}
		return num * -1521134295 + m_source.GetHashCode();
	}

	private void EnsureSorted()
	{
		if (m_isDirty)
		{
			m_paths.Sort(SaveSystem.GetComparerByDataType(ParentSaveWithBackups.ParentSaveCollection.m_dataType));
			m_isDirty = false;
		}
	}

	private void OnModified()
	{
		SetDirty();
		m_modifiedCallback?.Invoke();
	}

	private void SetDirty()
	{
		m_isDirty = m_paths.Count > 1;
		m_fileName = null;
	}
}

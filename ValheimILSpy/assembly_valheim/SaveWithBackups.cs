using System;
using System.Collections.Generic;

public class SaveWithBackups
{
	private List<SaveFile> m_saveFiles = new List<SaveFile>();

	private Action m_modifiedCallback;

	private bool m_isDirty;

	private SaveFile m_primaryFile;

	private List<SaveFile> m_backupFiles = new List<SaveFile>();

	private Dictionary<string, SaveFile> m_saveFilesByNameAndSource = new Dictionary<string, SaveFile>();

	public SaveFile PrimaryFile
	{
		get
		{
			EnsureSortedAndPrimaryFileDetermined();
			return m_primaryFile;
		}
	}

	public SaveFile[] BackupFiles
	{
		get
		{
			EnsureSortedAndPrimaryFileDetermined();
			return m_backupFiles.ToArray();
		}
	}

	public SaveFile[] AllFiles => m_saveFiles.ToArray();

	public ulong SizeWithBackups
	{
		get
		{
			ulong num = 0uL;
			for (int i = 0; i < m_saveFiles.Count; i++)
			{
				num += m_saveFiles[i].Size;
			}
			return num;
		}
	}

	public bool IsDeleted => PrimaryFile == null;

	public string m_name { get; private set; }

	public SaveCollection ParentSaveCollection { get; private set; }

	public SaveWithBackups(string name, SaveCollection parentSaveCollection, Action modifiedCallback)
	{
		m_name = name;
		ParentSaveCollection = parentSaveCollection;
		m_modifiedCallback = modifiedCallback;
	}

	public SaveFile AddSaveFile(string filePath, FileHelpers.FileSource fileSource)
	{
		SaveFile saveFile = new SaveFile(filePath, fileSource, this, OnModified);
		string key = saveFile.FileName + "_" + saveFile.m_source;
		if (m_saveFiles.Count > 0 && m_saveFilesByNameAndSource.TryGetValue(key, out var value))
		{
			value.AddAssociatedFiles(saveFile.AllPaths);
		}
		else
		{
			m_saveFiles.Add(saveFile);
			m_saveFilesByNameAndSource.Add(key, saveFile);
		}
		OnModified();
		return saveFile;
	}

	public SaveFile AddSaveFile(string[] filePaths, FileHelpers.FileSource fileSource)
	{
		SaveFile saveFile = new SaveFile(filePaths, fileSource, this, OnModified);
		string key = saveFile.FileName + "_" + saveFile.m_source;
		if (m_saveFiles.Count > 0 && m_saveFilesByNameAndSource.TryGetValue(key, out var value))
		{
			value.AddAssociatedFiles(saveFile.AllPaths);
		}
		else
		{
			m_saveFiles.Add(saveFile);
			m_saveFilesByNameAndSource.Add(key, saveFile);
		}
		OnModified();
		return saveFile;
	}

	public void RemoveSaveFile(SaveFile saveFile)
	{
		m_saveFiles.Remove(saveFile);
		string key = saveFile.FileName + "_" + saveFile.m_source;
		m_saveFilesByNameAndSource.Remove(key);
		OnModified();
	}

	private void EnsureSortedAndPrimaryFileDetermined()
	{
		if (!m_isDirty)
		{
			return;
		}
		m_saveFiles.Sort(new SaveFileComparer());
		m_primaryFile = null;
		for (int i = 0; i < m_saveFiles.Count; i++)
		{
			if (SaveSystem.GetSaveInfo(m_saveFiles[i].PathPrimary, out var _, out var saveFileType, out var _, out var _) && saveFileType == SaveFileType.Single && (m_primaryFile == null || m_saveFiles[i].m_source == FileHelpers.FileSource.Cloud || (m_saveFiles[i].m_source == FileHelpers.FileSource.Local && m_primaryFile.m_source == FileHelpers.FileSource.Legacy)))
			{
				m_primaryFile = m_saveFiles[i];
			}
		}
		if (m_primaryFile != null)
		{
			m_name = m_primaryFile.FileName;
		}
		m_backupFiles.Clear();
		if (m_primaryFile == null)
		{
			m_backupFiles.AddRange(m_saveFiles);
		}
		else
		{
			for (int j = 0; j < m_saveFiles.Count; j++)
			{
				if (m_saveFiles[j] != m_primaryFile)
				{
					m_backupFiles.Add(m_saveFiles[j]);
				}
			}
		}
		m_isDirty = false;
	}

	private void OnModified()
	{
		SetDirty();
		m_modifiedCallback?.Invoke();
	}

	private void SetDirty()
	{
		m_isDirty = true;
	}
}

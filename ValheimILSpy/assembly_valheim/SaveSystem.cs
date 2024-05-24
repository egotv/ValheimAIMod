using System;
using System.Collections.Generic;
using System.IO;

public class SaveSystem
{
	public enum RestoreBackupResult
	{
		Success,
		UnknownError,
		NoBackup,
		RenameFailed,
		CopyFailed,
		AlreadyHasMeta
	}

	public const string newNaming = ".new";

	public const string oldNaming = ".old";

	public const char fileNameSplitChar = '_';

	public const string backupNaming = "_backup_";

	public const string backupAutoNaming = "_backup_auto-";

	public const string backupRestoreNaming = "_backup_restore-";

	public const string backupCloudNaming = "_backup_cloud-";

	public const string characterFileEnding = ".fch";

	public const string worldMetaFileEnding = ".fwl";

	public const string worldDbFileEnding = ".db";

	private const double maximumBackupTimestampDifference = 10.0;

	private static Dictionary<SaveDataType, SaveCollection> s_saveCollections = null;

	private const bool useWorldListCache = true;

	private static Dictionary<FilePathAndSource, World> m_cachedWorlds = new Dictionary<FilePathAndSource, World>();

	private static void EnsureCollectionsAreCreated()
	{
		if (s_saveCollections == null)
		{
			s_saveCollections = new Dictionary<SaveDataType, SaveCollection>();
			SaveDataType[] array = new SaveDataType[2]
			{
				SaveDataType.World,
				SaveDataType.Character
			};
			for (int i = 0; i < array.Length; i++)
			{
				SaveCollection saveCollection = new SaveCollection(array[i]);
				s_saveCollections.Add(saveCollection.m_dataType, saveCollection);
			}
		}
	}

	public static SaveWithBackups[] GetSavesByType(SaveDataType dataType)
	{
		EnsureCollectionsAreCreated();
		if (!s_saveCollections.TryGetValue(dataType, out var value))
		{
			return null;
		}
		return value.Saves;
	}

	public static bool TryGetSaveByName(string name, SaveDataType dataType, out SaveWithBackups save)
	{
		EnsureCollectionsAreCreated();
		if (!s_saveCollections.TryGetValue(dataType, out var value))
		{
			ZLog.LogError($"Failed to retrieve collection of type {dataType}!");
			save = null;
			return false;
		}
		return value.TryGetSaveByName(name, out save);
	}

	public static void ForceRefreshCache()
	{
		foreach (KeyValuePair<SaveDataType, SaveCollection> s_saveCollection in s_saveCollections)
		{
			s_saveCollection.Value.EnsureLoadedAndSorted();
		}
	}

	public static void InvalidateCache()
	{
		EnsureCollectionsAreCreated();
		foreach (KeyValuePair<SaveDataType, SaveCollection> s_saveCollection in s_saveCollections)
		{
			s_saveCollection.Value.InvalidateCache();
		}
	}

	public static IComparer<string> GetComparerByDataType(SaveDataType dataType)
	{
		return dataType switch
		{
			SaveDataType.World => new WorldSaveComparer(), 
			SaveDataType.Character => new CharacterSaveComparer(), 
			_ => null, 
		};
	}

	public static string GetSavePath(SaveDataType dataType, FileHelpers.FileSource source)
	{
		switch (dataType)
		{
		case SaveDataType.World:
			return World.GetWorldSavePath(source);
		case SaveDataType.Character:
			return PlayerProfile.GetCharacterFolderPath(source);
		default:
			ZLog.LogError($"Reload not implemented for save data type {dataType}!");
			return null;
		}
	}

	public static bool Delete(SaveFile file)
	{
		int num = 0;
		for (int i = 0; i < file.AllPaths.Length; i++)
		{
			if (!FileHelpers.Delete(file.AllPaths[i], file.m_source))
			{
				num++;
			}
		}
		Minimap.DeleteMapTextureData(file.FileName);
		if (num > 0)
		{
			InvalidateCache();
			return false;
		}
		SaveWithBackups parentSaveWithBackups = file.ParentSaveWithBackups;
		parentSaveWithBackups.RemoveSaveFile(file);
		if (parentSaveWithBackups.AllFiles.Length == 0)
		{
			parentSaveWithBackups.ParentSaveCollection.Remove(parentSaveWithBackups);
		}
		return true;
	}

	public static bool Copy(SaveFile file, string newName, FileHelpers.FileSource destinationLocation = FileHelpers.FileSource.Auto)
	{
		if (destinationLocation == FileHelpers.FileSource.Auto)
		{
			destinationLocation = file.m_source;
		}
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (!GetSaveInfo(allPaths[i], out var _, out var _, out var actualFileEnding, out var _))
			{
				ZLog.LogError("Failed to get save info for file " + allPaths[i]);
				return false;
			}
			if (!TryConvertSource(allPaths[i], file.m_source, destinationLocation, out var destinationPath))
			{
				ZLog.LogError($"Failed to convert source from {file.m_source} to {destinationLocation} for file {allPaths[i]}");
				return false;
			}
			int num = destinationPath.LastIndexOfAny(new char[2] { '/', '\\' });
			string text = ((num >= 0) ? destinationPath.Substring(0, num + 1) : destinationPath);
			array[i] = text + newName + actualFileEnding;
		}
		bool flag = false;
		for (int j = 0; j < allPaths.Length; j++)
		{
			if (!FileHelpers.Copy(allPaths[j], file.m_source, array[j], destinationLocation))
			{
				flag = true;
			}
		}
		if (flag)
		{
			InvalidateCache();
		}
		else
		{
			file.ParentSaveWithBackups.AddSaveFile(array, destinationLocation);
		}
		return true;
	}

	public static bool Rename(SaveFile file, string newName)
	{
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (!GetSaveInfo(allPaths[i], out var _, out var _, out var actualFileEnding, out var _))
			{
				return false;
			}
			int num = allPaths[i].LastIndexOfAny(new char[2] { '/', '\\' });
			string text = ((num >= 0) ? allPaths[i].Substring(0, num + 1) : allPaths[i]);
			array[i] = text + newName + actualFileEnding;
		}
		if (file.m_source == FileHelpers.FileSource.Cloud)
		{
			int num2 = -1;
			for (int j = 0; j < allPaths.Length; j++)
			{
				if (!FileHelpers.CloudMove(allPaths[j], array[j]))
				{
					num2 = j;
					break;
				}
			}
			if (num2 >= 0)
			{
				for (int k = 0; k < num2; k++)
				{
					FileHelpers.CloudMove(allPaths[k], array[k]);
				}
				InvalidateCache();
				return false;
			}
		}
		else
		{
			for (int l = 0; l < allPaths.Length; l++)
			{
				File.Move(allPaths[l], array[l]);
			}
		}
		SaveWithBackups parentSaveWithBackups = file.ParentSaveWithBackups;
		parentSaveWithBackups.RemoveSaveFile(file);
		parentSaveWithBackups.AddSaveFile(array, file.m_source);
		return true;
	}

	public static bool MoveSource(SaveFile file, bool isBackup, FileHelpers.FileSource destinationSource, out bool cloudQuotaExceeded)
	{
		cloudQuotaExceeded = false;
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (!TryConvertSource(allPaths[i], file.m_source, destinationSource, out array[i]))
			{
				ZLog.LogError($"Failed to convert source from {file.m_source} to {destinationSource} for file {allPaths[i]}");
				return false;
			}
		}
		if (destinationSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(file.Size))
		{
			ZLog.LogWarning("This operation would exceed the cloud save quota and has therefore been aborted!");
			cloudQuotaExceeded = true;
			return false;
		}
		bool flag = false;
		int num = 0;
		for (int j = 0; j < allPaths.Length; j++)
		{
			if (!FileHelpers.Copy(allPaths[j], file.m_source, array[j], destinationSource))
			{
				flag = true;
				break;
			}
			num = j;
		}
		if (flag)
		{
			ZLog.LogError("Copying world into cloud failed, aborting move to cloud.");
			for (int k = 0; k < num; k++)
			{
				FileHelpers.Delete(array[k], FileHelpers.FileSource.Cloud);
			}
			InvalidateCache();
			return false;
		}
		file.ParentSaveWithBackups.AddSaveFile(array, destinationSource);
		if (file.m_source != FileHelpers.FileSource.Cloud && !isBackup)
		{
			MoveToBackup(file, DateTime.Now);
		}
		else
		{
			Delete(file);
		}
		return true;
	}

	public static RestoreBackupResult RestoreMetaFromMostRecentBackup(SaveFile saveFile)
	{
		if (!saveFile.PathPrimary.EndsWith(".db"))
		{
			return RestoreBackupResult.UnknownError;
		}
		for (int i = 0; i < saveFile.AllPaths.Length; i++)
		{
			if (saveFile.AllPaths[i].EndsWith(".fwl"))
			{
				return RestoreBackupResult.AlreadyHasMeta;
			}
		}
		SaveFile saveFile2 = GetMostRecentBackupWithMeta(saveFile.ParentSaveWithBackups);
		if (saveFile2 == null)
		{
			return RestoreBackupResult.NoBackup;
		}
		string text = World.GetWorldSavePath(saveFile.m_source) + "/" + saveFile.ParentSaveWithBackups.m_name + ".fwl";
		try
		{
			if (!FileHelpers.Copy(saveFile2.PathPrimary, saveFile2.m_source, text, saveFile.m_source))
			{
				InvalidateCache();
				return RestoreBackupResult.CopyFailed;
			}
		}
		catch (Exception ex)
		{
			ZLog.LogError("Caught exception while restoring meta from backup: " + ex.ToString());
			InvalidateCache();
			return RestoreBackupResult.UnknownError;
		}
		saveFile.AddAssociatedFile(text);
		return RestoreBackupResult.Success;
		static SaveFile GetMostRecentBackupWithMeta(SaveWithBackups save)
		{
			int num = -1;
			for (int j = 0; j < save.BackupFiles.Length; j++)
			{
				if (IsRestorableMeta(save.BackupFiles[j]) && (num < 0 || !(save.BackupFiles[j].LastModified <= save.BackupFiles[num].LastModified)))
				{
					num = j;
				}
			}
			if (num < 0)
			{
				return null;
			}
			return save.BackupFiles[num];
		}
	}

	public static RestoreBackupResult RestoreBackup(SaveFile backup)
	{
		if (!GetSaveInfo(backup.PathPrimary, out var _, out var saveFileType, out var _, out var _))
		{
			return RestoreBackupResult.UnknownError;
		}
		SaveWithBackups parentSaveWithBackups = backup.ParentSaveWithBackups;
		Minimap.DeleteMapTextureData(parentSaveWithBackups.m_name);
		if (!parentSaveWithBackups.IsDeleted && !Rename(parentSaveWithBackups.PrimaryFile, parentSaveWithBackups.m_name + "_backup_restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")))
		{
			return RestoreBackupResult.RenameFailed;
		}
		string newName;
		bool flag;
		if (saveFileType == SaveFileType.Single)
		{
			newName = parentSaveWithBackups.m_name + "_backup_" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			flag = false;
		}
		else
		{
			newName = parentSaveWithBackups.m_name;
			flag = backup.m_source == FileHelpers.FileSource.Local && saveFileType == SaveFileType.CloudBackup;
		}
		if (!Copy(backup, newName, flag ? FileHelpers.FileSource.Cloud : backup.m_source))
		{
			return RestoreBackupResult.CopyFailed;
		}
		return RestoreBackupResult.Success;
	}

	public static RestoreBackupResult RestoreMostRecentBackup(SaveWithBackups save)
	{
		SaveFile saveFile = GetMostRecentBackup(save);
		if (saveFile == null)
		{
			return RestoreBackupResult.NoBackup;
		}
		return RestoreBackup(saveFile);
		static SaveFile GetMostRecentBackup(SaveWithBackups save)
		{
			int num = -1;
			for (int i = 0; i < save.BackupFiles.Length; i++)
			{
				if (IsRestorableBackup(save.BackupFiles[i]) && (num < 0 || !(save.BackupFiles[i].LastModified <= save.BackupFiles[num].LastModified)))
				{
					num = i;
				}
			}
			if (num < 0)
			{
				return null;
			}
			return save.BackupFiles[num];
		}
	}

	public static bool CheckMove(string saveName, SaveDataType dataType, ref FileHelpers.FileSource source, DateTime now, ulong opUsage = 0uL, bool copyToNewLocation = false)
	{
		SaveFile saveFile = null;
		if (TryGetSaveByName(saveName, dataType, out var save) && !save.IsDeleted && save.PrimaryFile.m_source == source)
		{
			saveFile = save.PrimaryFile;
		}
		if (source == FileHelpers.FileSource.Legacy)
		{
			if (FileHelpers.m_cloudEnabled && !FileHelpers.OperationExceedsCloudCapacity(opUsage))
			{
				source = FileHelpers.FileSource.Cloud;
			}
			else
			{
				source = FileHelpers.FileSource.Local;
			}
			if (saveFile != null)
			{
				if (copyToNewLocation)
				{
					Copy(saveFile, saveName, source);
				}
				MoveToBackup(saveFile, now);
			}
			return true;
		}
		if (source == FileHelpers.FileSource.Local && FileHelpers.m_cloudEnabled && FileHelpers.m_cloudOnly && !FileHelpers.OperationExceedsCloudCapacity(opUsage))
		{
			source = FileHelpers.FileSource.Cloud;
			if (saveFile != null)
			{
				if (copyToNewLocation)
				{
					Copy(saveFile, saveName, source);
				}
				MoveToBackup(saveFile, now);
			}
			return true;
		}
		return false;
	}

	private static bool MoveToBackup(SaveFile saveFile, DateTime now)
	{
		return Rename(saveFile, saveFile.ParentSaveWithBackups.m_name + "_backup_" + now.ToString("yyyyMMdd-HHmmss"));
	}

	public static bool CreateBackup(SaveFile saveFile, DateTime now, FileHelpers.FileSource source = FileHelpers.FileSource.Auto)
	{
		return Copy(saveFile, saveFile.ParentSaveWithBackups.m_name + "_backup_" + now.ToString("yyyyMMdd-HHmmss"), source);
	}

	public static bool ConsiderBackup(string saveName, SaveDataType dataType, DateTime now, int backupCount, int backupShort, int backupLong, int waitFirstBackup, float worldTime = 0f)
	{
		ZLog.Log($"Considering autobackup. World time: {worldTime}, short time: {backupShort}, long time: {backupLong}, backup count: {backupCount}");
		if (worldTime > 0f && worldTime < (float)waitFirstBackup)
		{
			ZLog.Log("Skipping backup. World session not long enough.");
			return false;
		}
		if (backupCount == 1)
		{
			backupCount = 2;
		}
		if (!TryGetSaveByName(saveName, dataType, out var save))
		{
			ZLog.LogError("Failed to retrieve save with name " + saveName + "!");
			return false;
		}
		if (save.IsDeleted)
		{
			ZLog.LogError("Save with name " + saveName + " is deleted, can't manage auto-backups!");
			return false;
		}
		List<SaveFile> list = new List<SaveFile>();
		SaveFile[] backupFiles = save.BackupFiles;
		foreach (SaveFile saveFile in backupFiles)
		{
			if (GetSaveInfo(saveFile.PathPrimary, out var _, out var saveFileType, out var _, out var _) && saveFileType == SaveFileType.AutoBackup)
			{
				list.Add(saveFile);
			}
		}
		list.Sort((SaveFile a, SaveFile b) => b.LastModified.CompareTo(a.LastModified));
		while (list.Count > backupCount)
		{
			list.RemoveAt(list.Count - 1);
		}
		SaveFile saveFile2 = null;
		if (list.Count == 0)
		{
			ZLog.Log("Creating first autobackup");
		}
		else
		{
			if (!(now - TimeSpan.FromSeconds(backupShort) > list[0].LastModified))
			{
				ZLog.Log("No autobackup needed yet...");
				return false;
			}
			if (list.Count == 1)
			{
				ZLog.Log("Creating second autobackup for reference");
			}
			else if (now - TimeSpan.FromSeconds(backupLong) > list[1].LastModified)
			{
				if (list.Count < backupCount)
				{
					ZLog.Log("Creating new backup since we haven't reached our desired amount");
				}
				else
				{
					saveFile2 = list[list.Count - 1];
					ZLog.Log("Time to overwrite our last autobackup");
				}
			}
			else
			{
				saveFile2 = list[0];
				ZLog.Log("Overwrite our newest autobackup since the second one isn't so old");
			}
		}
		if (saveFile2 != null)
		{
			ZLog.Log("Replacing backup file: " + saveFile2.FileName);
			if (!Delete(saveFile2))
			{
				ZLog.LogError("Failed to delete backup " + saveFile2.FileName + "!");
				return false;
			}
		}
		string text = saveName + "_backup_auto-" + now.ToString("yyyyMMddHHmmss");
		ZLog.Log("Saving backup at: " + text);
		if (!Copy(save.PrimaryFile, text, save.PrimaryFile.m_source))
		{
			ZLog.LogError("Failed to copy save with name " + saveName + " to auto-backup!");
			return false;
		}
		return true;
	}

	public static bool HasBackupWithMeta(SaveWithBackups save)
	{
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (IsRestorableMeta(save.BackupFiles[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasRestorableBackup(SaveWithBackups save)
	{
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (IsRestorableBackup(save.BackupFiles[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsRestorableBackup(SaveFile backup)
	{
		switch (backup.ParentSaveWithBackups.ParentSaveCollection.m_dataType)
		{
		case SaveDataType.World:
			if (!backup.PathPrimary.EndsWith(".fwl"))
			{
				return false;
			}
			if (backup.PathsAssociated.Length < 1 || !backup.PathsAssociated[0].EndsWith(".db"))
			{
				return false;
			}
			break;
		case SaveDataType.Character:
			if (!backup.PathPrimary.EndsWith(".fch"))
			{
				return false;
			}
			break;
		default:
			ZLog.LogError($"Not implemented for {backup.ParentSaveWithBackups.ParentSaveCollection.m_dataType}!");
			return false;
		}
		for (int i = 0; i < backup.AllPaths.Length; i++)
		{
			if (FileHelpers.IsFileCorrupt(backup.AllPaths[i], backup.m_source))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsRestorableMeta(SaveFile backup)
	{
		if (!backup.PathPrimary.EndsWith(".fwl"))
		{
			return false;
		}
		if (FileHelpers.IsFileCorrupt(backup.PathPrimary, backup.m_source))
		{
			return false;
		}
		return true;
	}

	public static bool IsCorrupt(SaveFile file)
	{
		for (int i = 0; i < file.AllPaths.Length; i++)
		{
			if (FileHelpers.IsFileCorrupt(file.AllPaths[i], file.m_source))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsWorldWithMissingMetaFile(SaveFile file)
	{
		if (file.ParentSaveWithBackups.ParentSaveCollection.m_dataType != 0)
		{
			return false;
		}
		if (GetSaveInfo(file.PathPrimary, out var _, out var _, out var actualFileEnding, out var _))
		{
			return actualFileEnding != ".fwl";
		}
		return false;
	}

	public static bool GetSaveInfo(string path, out string saveName, out SaveFileType saveFileType, out string actualFileEnding, out DateTime? timestamp)
	{
		string text = RemoveDirectoryPart(path);
		int[] array = text.AllIndicesOf('.');
		if (array.Length == 0)
		{
			saveName = "";
			actualFileEnding = "";
			saveFileType = SaveFileType.Single;
			timestamp = null;
			return false;
		}
		if (text.EndsWith(".old"))
		{
			saveName = text.Substring(0, array[0]);
			saveFileType = SaveFileType.OldBackup;
			actualFileEnding = ((array.Length >= 2) ? text.Substring(array[0], array[array.Length - 1] - array[0]) : "");
			timestamp = null;
			return true;
		}
		string text2 = text.Substring(0, array[array.Length - 1]);
		timestamp = null;
		if (text2.Length >= 14)
		{
			char[] array2 = new char[14];
			int num = array2.Length;
			int num2 = text2.Length - 1;
			while (num2 >= 0 && num > 0)
			{
				if (text2[num2] != '-')
				{
					num--;
					array2[num] = text2[num2];
				}
				num2--;
			}
			if (num == 0)
			{
				string text3 = new string(array2);
				if (text3.Length >= 14 && int.TryParse(text3.Substring(0, 4), out var result) && int.TryParse(text3.Substring(4, 2), out var result2) && int.TryParse(text3.Substring(6, 2), out var result3) && int.TryParse(text3.Substring(8, 2), out var result4) && int.TryParse(text3.Substring(10, 2), out var result5) && int.TryParse(text3.Substring(12, 2), out var result6))
				{
					try
					{
						timestamp = new DateTime(result, result2, result3, result4, result5, result6);
					}
					catch (ArgumentOutOfRangeException)
					{
						timestamp = null;
					}
				}
			}
		}
		actualFileEnding = ((array.Length != 0) ? text.Substring(array[array.Length - 1]) : "");
		if (!timestamp.HasValue)
		{
			saveFileType = SaveFileType.Single;
			saveName = text2;
			return true;
		}
		int[] array3 = text.AllIndicesOf('_');
		if (array3.Length >= 1)
		{
			if (array3.Length >= 2 && text.Length - array3[array3.Length - 2] >= "_backup_".Length && text.Substring(array3[array3.Length - 2], "_backup_".Length) == "_backup_")
			{
				if (text.Length - array3[array3.Length - 2] >= "_backup_auto-".Length && text.Substring(array3[array3.Length - 2], "_backup_auto-".Length) == "_backup_auto-")
				{
					saveFileType = SaveFileType.AutoBackup;
				}
				else if (text.Length - array3[array3.Length - 2] >= "_backup_cloud-".Length && text.Substring(array3[array3.Length - 2], "_backup_cloud-".Length) == "_backup_cloud-")
				{
					saveFileType = SaveFileType.CloudBackup;
				}
				else if (text.Length - array3[array3.Length - 2] >= "_backup_restore-".Length && text.Substring(array3[array3.Length - 2], "_backup_restore-".Length) == "_backup_restore-")
				{
					saveFileType = SaveFileType.RestoredBackup;
				}
				else
				{
					saveFileType = SaveFileType.StandardBackup;
				}
			}
			else
			{
				saveFileType = SaveFileType.Rolling;
			}
			saveName = text.Substring(0, array3[array3.Length - ((saveFileType == SaveFileType.Rolling) ? 1 : 2)]);
			if (saveName.Length == 0)
			{
				timestamp = null;
				saveFileType = SaveFileType.Single;
				saveName = text2;
			}
		}
		else
		{
			timestamp = null;
			saveFileType = SaveFileType.Single;
			saveName = text2;
		}
		return true;
	}

	public static string RemoveDirectoryPart(string path)
	{
		int num = path.LastIndexOfAny(new char[2] { '/', '\\' });
		if (num >= 0)
		{
			return path.Substring(num + 1);
		}
		return path;
	}

	public static bool TryConvertSource(string sourcePath, FileHelpers.FileSource sourceLocation, FileHelpers.FileSource destinationLocation, out string destinationPath)
	{
		string text = NormalizePath(sourcePath, sourceLocation);
		if (sourceLocation == destinationLocation)
		{
			destinationPath = text;
			return true;
		}
		string text2 = NormalizePath(World.GetWorldSavePath(sourceLocation), sourceLocation);
		if (text.StartsWith(text2))
		{
			destinationPath = NormalizePath(World.GetWorldSavePath(destinationLocation), destinationLocation) + text.Substring(text2.Length);
			return true;
		}
		string text3 = NormalizePath(PlayerProfile.GetCharacterFolderPath(sourceLocation), sourceLocation);
		if (text.StartsWith(text3))
		{
			destinationPath = NormalizePath(PlayerProfile.GetCharacterFolderPath(destinationLocation), destinationLocation) + text.Substring(text3.Length);
			return true;
		}
		destinationPath = null;
		return false;
	}

	public static string NormalizePath(string path, FileHelpers.FileSource source)
	{
		char[] array = new char[path.Length];
		int num = 0;
		for (int i = 0; i < path.Length; i++)
		{
			char c = path[i];
			if (c == '\\')
			{
				c = '/';
			}
			if (c == '/')
			{
				if (num > 0)
				{
					if (array[num - 1] == '/')
					{
						continue;
					}
				}
				else if (source == FileHelpers.FileSource.Cloud)
				{
					continue;
				}
			}
			array[num++] = c;
		}
		return new string(array, 0, num);
	}

	public static string NormalizePath(string path)
	{
		char[] array = new char[path.Length];
		int num = 0;
		for (int i = 0; i < path.Length; i++)
		{
			char c = path[i];
			if (c == '\\')
			{
				c = '/';
			}
			if (c != '/' || num <= 0 || array[num - 1] != '/')
			{
				array[num++] = c;
			}
		}
		return new string(array, 0, num);
	}

	public static void ClearWorldListCache(bool reload)
	{
		m_cachedWorlds.Clear();
		if (reload)
		{
			GetWorldList();
		}
	}

	public static List<World> GetWorldList()
	{
		SaveWithBackups[] savesByType = GetSavesByType(SaveDataType.World);
		List<World> list = new List<World>();
		HashSet<FilePathAndSource> hashSet = new HashSet<FilePathAndSource>();
		for (int i = 0; i < savesByType.Length; i++)
		{
			if (savesByType[i].IsDeleted)
			{
				continue;
			}
			World value;
			if (savesByType[i].PrimaryFile.PathPrimary.EndsWith(".db"))
			{
				if (GetSaveInfo(savesByType[i].PrimaryFile.PathPrimary, out var _, out var _, out var _, out var _))
				{
					value = new World(savesByType[i], FileHelpers.IsFileCorrupt(savesByType[i].PrimaryFile.PathPrimary, savesByType[i].PrimaryFile.m_source) ? World.SaveDataError.Corrupt : World.SaveDataError.MissingMeta);
					list.Add(value);
				}
			}
			else
			{
				if (!savesByType[i].PrimaryFile.PathPrimary.EndsWith(".fwl"))
				{
					continue;
				}
				FilePathAndSource filePathAndSource = new FilePathAndSource(savesByType[i].PrimaryFile.PathPrimary, savesByType[i].PrimaryFile.m_source);
				if (m_cachedWorlds.TryGetValue(filePathAndSource, out value))
				{
					list.Add(value);
					hashSet.Add(filePathAndSource);
					continue;
				}
				value = World.LoadWorld(savesByType[i]);
				if (value != null)
				{
					list.Add(value);
					hashSet.Add(filePathAndSource);
					m_cachedWorlds.Add(filePathAndSource, value);
				}
			}
		}
		List<FilePathAndSource> list2 = new List<FilePathAndSource>();
		foreach (KeyValuePair<FilePathAndSource, World> cachedWorld in m_cachedWorlds)
		{
			FilePathAndSource key = cachedWorld.Key;
			if (!hashSet.Contains(key))
			{
				list2.Add(key);
			}
		}
		for (int j = 0; j < list2.Count; j++)
		{
			m_cachedWorlds.Remove(list2[j]);
		}
		return list;
	}

	public static List<PlayerProfile> GetAllPlayerProfiles()
	{
		SaveWithBackups[] savesByType = GetSavesByType(SaveDataType.Character);
		List<PlayerProfile> list = new List<PlayerProfile>();
		for (int i = 0; i < savesByType.Length; i++)
		{
			if (!savesByType[i].IsDeleted)
			{
				PlayerProfile playerProfile = new PlayerProfile(savesByType[i].m_name, savesByType[i].PrimaryFile.m_source);
				if (!playerProfile.Load())
				{
					ZLog.Log("Failed to load " + savesByType[i].m_name);
				}
				else
				{
					list.Add(playerProfile);
				}
			}
		}
		return list;
	}

	public static bool CanSaveToCloudStorage(World world, PlayerProfile playerProfile)
	{
		bool flag = world != null && (FileHelpers.m_cloudOnly || world.m_fileSource == FileHelpers.FileSource.Cloud || (FileHelpers.m_cloudEnabled && world.m_fileSource == FileHelpers.FileSource.Legacy));
		bool flag2 = playerProfile != null && (FileHelpers.m_cloudOnly || playerProfile.m_fileSource == FileHelpers.FileSource.Cloud || (FileHelpers.m_cloudEnabled && playerProfile.m_fileSource == FileHelpers.FileSource.Legacy));
		if (!flag && !flag2)
		{
			return true;
		}
		ulong num = 0uL;
		if (flag)
		{
			string metaPath = world.GetMetaPath(world.m_fileSource);
			string dBPath = world.GetDBPath(world.m_fileSource);
			num += 104857600;
			if (FileHelpers.Exists(metaPath, world.m_fileSource))
			{
				num += FileHelpers.GetFileSize(metaPath, world.m_fileSource) * 2;
				if (FileHelpers.Exists(dBPath, world.m_fileSource))
				{
					num += FileHelpers.GetFileSize(dBPath, world.m_fileSource) * 2;
				}
			}
			else
			{
				ZLog.LogError("World save file doesn't exist! Using less accurate storage usage estimate.");
			}
		}
		if (flag2)
		{
			string path = playerProfile.GetPath();
			num += 2097152;
			if (FileHelpers.Exists(path, playerProfile.m_fileSource))
			{
				num += FileHelpers.GetFileSize(path, playerProfile.m_fileSource) * 2;
			}
			else
			{
				ZLog.LogError("Player save file doesn't exist! Using less accurate storage usage estimate.");
			}
		}
		return !FileHelpers.OperationExceedsCloudCapacity(num);
	}
}

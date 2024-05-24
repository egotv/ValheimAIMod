using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class World
{
	public enum SaveDataError
	{
		None,
		BadVersion,
		LoadError,
		Corrupt,
		MissingMeta,
		MissingDB
	}

	public string m_fileName = "";

	public string m_name = "";

	public string m_seedName = "";

	public int m_seed;

	public long m_uid;

	public List<string> m_startingGlobalKeys = new List<string>();

	public bool m_startingKeysChanged;

	public int m_worldGenVersion;

	public int m_worldVersion;

	public bool m_menu;

	public bool m_needsDB;

	public bool m_createBackupBeforeSaving;

	public SaveWithBackups saves;

	public SaveDataError m_dataError;

	public FileHelpers.FileSource m_fileSource = FileHelpers.FileSource.Local;

	public World()
	{
	}

	public World(SaveWithBackups save, SaveDataError dataError)
	{
		m_fileName = (m_name = save.m_name);
		m_dataError = dataError;
		m_fileSource = save.PrimaryFile.m_source;
	}

	public World(string name, string seed)
	{
		m_fileName = (m_name = name);
		m_seedName = seed;
		m_seed = ((!(m_seedName == "")) ? m_seedName.GetStableHashCode() : 0);
		m_uid = name.GetStableHashCode() + Utils.GenerateUID();
		m_worldGenVersion = 2;
	}

	public static string GetWorldSavePath(FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return Utils.GetSaveDataPath(fileSource) + ((fileSource == FileHelpers.FileSource.Local) ? "/worlds_local" : "/worlds");
	}

	public static void RemoveWorld(string name, FileHelpers.FileSource fileSource)
	{
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out var save) && !save.IsDeleted)
		{
			SaveSystem.Delete(save.PrimaryFile);
		}
	}

	public string GetRootPath(FileHelpers.FileSource fileSource)
	{
		return GetWorldSavePath(fileSource) + "/" + m_fileName;
	}

	public string GetDBPath()
	{
		return GetDBPath(m_fileSource);
	}

	public string GetDBPath(FileHelpers.FileSource fileSource)
	{
		return GetWorldSavePath(fileSource) + "/" + m_fileName + ".db";
	}

	public static string GetDBPath(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return GetWorldSavePath(fileSource) + "/" + name + ".db";
	}

	public string GetMetaPath()
	{
		return GetMetaPath(m_fileSource);
	}

	public string GetMetaPath(FileHelpers.FileSource fileSource)
	{
		return GetWorldSavePath(fileSource) + "/" + m_fileName + ".fwl";
	}

	public static string GetMetaPath(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return GetWorldSavePath(fileSource) + "/" + name + ".fwl";
	}

	public static bool HaveWorld(string name)
	{
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out var save))
		{
			return !save.IsDeleted;
		}
		return false;
	}

	public static World GetMenuWorld()
	{
		return new World("menu", "")
		{
			m_menu = true
		};
	}

	public static World GetEditorWorld()
	{
		return new World("editor", "");
	}

	public static string GenerateSeed()
	{
		string text = "";
		for (int i = 0; i < 10; i++)
		{
			text += "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789"[UnityEngine.Random.Range(0, "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789".Length)];
		}
		return text;
	}

	public static World GetCreateWorld(string name, FileHelpers.FileSource source)
	{
		ZLog.Log("Get create world " + name);
		World world;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out var save) && !save.IsDeleted)
		{
			world = LoadWorld(save);
			if (world.m_dataError == SaveDataError.None)
			{
				return world;
			}
			ZLog.LogError($"Failed to load world with name \"{name}\", data error {world.m_dataError}.");
		}
		ZLog.Log(" creating");
		world = new World(name, GenerateSeed());
		world.m_fileSource = source;
		world.SaveWorldMetaData(DateTime.Now);
		return world;
	}

	public static World GetDevWorld()
	{
		World world;
		if (SaveSystem.TryGetSaveByName(Game.instance.m_devWorldName, SaveDataType.World, out var save) && !save.IsDeleted)
		{
			world = LoadWorld(save);
			if (world.m_dataError == SaveDataError.None)
			{
				return world;
			}
			ZLog.Log($"Failed to load dev world, data error {world.m_dataError}. Creating...");
		}
		world = new World(Game.instance.m_devWorldName, Game.instance.m_devWorldSeed);
		world.m_fileSource = FileHelpers.FileSource.Local;
		world.SaveWorldMetaData(DateTime.Now);
		return world;
	}

	public void SaveWorldMetaData(DateTime backupTimestamp)
	{
		SaveWorldMetaData(backupTimestamp, considerBackup: true, out var _, out var _);
	}

	public void SaveWorldMetaData(DateTime now, bool considerBackup, out bool cloudSaveFailed, out FileWriter metaWriter)
	{
		GetDBPath();
		SaveSystem.CheckMove(m_fileName, SaveDataType.World, ref m_fileSource, now, 0uL);
		ZPackage zPackage = new ZPackage();
		zPackage.Write(34);
		zPackage.Write(m_name);
		zPackage.Write(m_seedName);
		zPackage.Write(m_seed);
		zPackage.Write(m_uid);
		zPackage.Write(m_worldGenVersion);
		zPackage.Write(m_needsDB);
		zPackage.Write(m_startingGlobalKeys.Count);
		for (int i = 0; i < m_startingGlobalKeys.Count; i++)
		{
			zPackage.Write(m_startingGlobalKeys[i]);
		}
		if (m_fileSource != FileHelpers.FileSource.Cloud)
		{
			Directory.CreateDirectory(GetWorldSavePath(m_fileSource));
		}
		string metaPath = GetMetaPath();
		string text = metaPath + ".new";
		string oldFile = metaPath + ".old";
		byte[] array = zPackage.GetArray();
		bool flag = m_fileSource == FileHelpers.FileSource.Cloud;
		FileWriter fileWriter = new FileWriter(flag ? metaPath : text, FileHelpers.FileHelperType.Binary, m_fileSource);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		SaveSystem.InvalidateCache();
		cloudSaveFailed = fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && m_fileSource == FileHelpers.FileSource.Cloud;
		if (!cloudSaveFailed)
		{
			if (!flag)
			{
				FileHelpers.ReplaceOldFile(metaPath, text, oldFile, m_fileSource);
				SaveSystem.InvalidateCache();
			}
			if (considerBackup)
			{
				ZNet.ConsiderAutoBackup(m_fileName, SaveDataType.World, now);
			}
		}
		metaWriter = fileWriter;
	}

	public static World LoadWorld(SaveWithBackups saveFile)
	{
		FileReader fileReader = null;
		if (saveFile.IsDeleted)
		{
			ZLog.Log("save deleted " + saveFile.m_name);
			return new World(saveFile, SaveDataError.LoadError);
		}
		FileHelpers.FileSource source = saveFile.PrimaryFile.m_source;
		string pathPrimary = saveFile.PrimaryFile.PathPrimary;
		string text = ((saveFile.PrimaryFile.PathsAssociated.Length != 0) ? saveFile.PrimaryFile.PathsAssociated[0] : null);
		if (FileHelpers.IsFileCorrupt(pathPrimary, source) || (text != null && FileHelpers.IsFileCorrupt(text, source)))
		{
			ZLog.Log("  corrupt save " + saveFile.m_name);
			return new World(saveFile, SaveDataError.Corrupt);
		}
		try
		{
			fileReader = new FileReader(pathPrimary, source);
		}
		catch (Exception ex)
		{
			fileReader?.Dispose();
			ZLog.Log("  failed to load " + saveFile.m_name + " Exception: " + ex);
			return new World(saveFile, SaveDataError.LoadError);
		}
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int count = binary.ReadInt32();
			ZPackage zPackage = new ZPackage(binary.ReadBytes(count));
			int num = zPackage.ReadInt();
			if (!Version.IsWorldVersionCompatible(num))
			{
				ZLog.Log("incompatible world version " + num);
				return new World(saveFile, SaveDataError.BadVersion);
			}
			World world = new World();
			world.m_fileSource = source;
			world.m_fileName = saveFile.m_name;
			world.m_name = zPackage.ReadString();
			world.m_seedName = zPackage.ReadString();
			world.m_seed = zPackage.ReadInt();
			world.m_uid = zPackage.ReadLong();
			world.m_worldVersion = num;
			if (num >= 26)
			{
				world.m_worldGenVersion = zPackage.ReadInt();
			}
			world.m_needsDB = num >= 30 && zPackage.ReadBool();
			if (num != 34)
			{
				world.m_createBackupBeforeSaving = true;
			}
			if (world.CheckDbFile())
			{
				world.m_dataError = SaveDataError.MissingDB;
			}
			if (num >= 32)
			{
				int num2 = zPackage.ReadInt();
				for (int i = 0; i < num2; i++)
				{
					world.m_startingGlobalKeys.Add(zPackage.ReadString());
				}
			}
			return world;
		}
		catch
		{
			ZLog.LogWarning("  error loading world " + saveFile.m_name);
			return new World(saveFile, SaveDataError.LoadError);
		}
		finally
		{
			fileReader?.Dispose();
		}
	}

	private bool CheckDbFile()
	{
		if (m_needsDB)
		{
			return !FileHelpers.Exists(GetDBPath(), m_fileSource);
		}
		return false;
	}
}

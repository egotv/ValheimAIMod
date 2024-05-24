using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Steamworks;
using UnityEngine;

public static class FileHelpers
{
	public enum FileHelperType
	{
		Binary,
		Stream
	}

	public enum FileSource
	{
		Auto,
		Local,
		Cloud,
		Legacy
	}

	public static bool m_cloudEnabled;

	public static bool m_cloudOnly;

	public static string BytesAsNumberString(ulong bytes, uint decimalCount)
	{
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		uint num = 0u;
		float num2 = bytes;
		while (num2 >= 1000f && num < array.Length - 1)
		{
			num++;
			num2 *= 0.001f;
		}
		return num2.ToString("N" + decimalCount) + " " + array[num];
	}

	public static void SplitFilePath(string path, out string directory, out string fileName, out string fileExtension)
	{
		int num = path.LastIndexOfAny(new char[2] { '/', '\\' });
		int num2 = path.LastIndexOf('.');
		directory = ((num < 0) ? "" : path.Substring(0, num + 1));
		if (num2 < 0 || num2 <= num)
		{
			fileName = path.Substring(directory.Length);
			fileExtension = "";
		}
		else
		{
			fileName = path.Substring(directory.Length).Substring(0, num2 - directory.Length);
			fileExtension = path.Substring(directory.Length + fileName.Length);
		}
	}

	public static void UpdateCloudEnabledStatus()
	{
		m_cloudEnabled = SteamRemoteStorage.IsCloudEnabledForAccount() && SteamRemoteStorage.IsCloudEnabledForApp();
		m_cloudOnly = false;
	}

	public static void ReplaceOldFile(string saveFile, string newFile, string oldFile, FileSource fileSource = FileSource.Auto)
	{
		if (m_cloudEnabled && (fileSource == FileSource.Auto || fileSource == FileSource.Cloud))
		{
			if (SteamRemoteStorage.FileExists(saveFile))
			{
				if (SteamRemoteStorage.FileExists(oldFile))
				{
					SteamRemoteStorage.FileDelete(oldFile);
				}
				CloudMove(saveFile, oldFile);
			}
			CloudMove(newFile, saveFile);
			return;
		}
		if (File.Exists(saveFile))
		{
			if (File.Exists(oldFile))
			{
				File.Delete(oldFile);
			}
			File.Move(saveFile, oldFile);
		}
		File.Move(newFile, saveFile);
	}

	public static void Copy(string source, string dest, FileSource fileSource)
	{
		if (m_cloudEnabled && fileSource == FileSource.Cloud)
		{
			int fileSize = SteamRemoteStorage.GetFileSize(source);
			byte[] array = new byte[fileSize];
			ZLog.Log($"Cloud Copy: {fileSize} bytes. {source} -> {dest}");
			SteamRemoteStorage.FileRead(source, array, fileSize);
			CloudFileWriteInChunks(dest, array);
		}
		else
		{
			File.Copy(source, dest);
		}
	}

	public static bool Copy(string source, FileSource sourceLocation, string dest, FileSource destLocation = FileSource.Auto)
	{
		if (sourceLocation == FileSource.Auto)
		{
			ZLog.LogError($"Can't copy file from source location {sourceLocation}");
			return false;
		}
		if (destLocation == FileSource.Auto)
		{
			destLocation = sourceLocation;
		}
		if (sourceLocation == FileSource.Cloud == (destLocation == FileSource.Cloud))
		{
			Copy(source, dest, sourceLocation);
		}
		else if (destLocation == FileSource.Cloud)
		{
			if (!FileCopyIntoCloud(source, dest))
			{
				return false;
			}
		}
		else
		{
			FileCopyOutFromCloud(source, dest, deleteOnCloud: false);
		}
		return true;
	}

	public static bool CloudMove(string source, string dest)
	{
		int fileSize = SteamRemoteStorage.GetFileSize(source);
		byte[] array = new byte[fileSize];
		ZLog.Log($"Steam Cloud Move: {fileSize} bytes. {source} -> {dest}");
		SteamRemoteStorage.FileRead(source, array, fileSize);
		bool num = CloudFileWriteInChunks(dest, array);
		if (num)
		{
			SteamRemoteStorage.FileDelete(source);
			return num;
		}
		ZLog.LogError("Failed to write data to new location!");
		return num;
	}

	public static bool FileCopyIntoCloud(string source, string target)
	{
		byte[] data = File.ReadAllBytes(source);
		return CloudFileWriteInChunks(target, data);
	}

	public static void FileCopyOutFromCloud(string cloudFilePath, string target, bool deleteOnCloud)
	{
		if (!SteamRemoteStorage.FileExists(cloudFilePath))
		{
			throw new FileNotFoundException();
		}
		EnsureDirectoryExists(target);
		int fileSize = SteamRemoteStorage.GetFileSize(cloudFilePath);
		byte[] array = new byte[fileSize];
		SteamRemoteStorage.FileRead(cloudFilePath, array, fileSize);
		File.WriteAllBytes(target, array);
		if (deleteOnCloud)
		{
			SteamRemoteStorage.FileDelete(cloudFilePath);
		}
	}

	public static bool FileExistsCloud(string cloudFilePath)
	{
		return SteamRemoteStorage.FileExists(cloudFilePath);
	}

	public static bool CloudFileWriteInChunks(string pchFile, byte[] data)
	{
		if (data.Length == 0)
		{
			ZLog.LogWarning("Trying to write 0 bytes in CloudFileWriteInChunks(). Does the read file exist?");
			return false;
		}
		int num = 104857600;
		UGCFileWriteStreamHandle_t writeHandle = SteamRemoteStorage.FileWriteStreamOpen(pchFile);
		byte[] array = new byte[num];
		int num2 = data.Length / num + 1;
		for (int i = 0; i < num2; i++)
		{
			ZLog.Log($"Steam writing file chunks {i + 1} / {num2}, ({data.Length} bytes)");
			int num3 = ((i + 1 == num2) ? (data.Length % num) : num);
			Array.Copy(data, i * num, array, 0, num3);
			if (!SteamRemoteStorage.FileWriteStreamWriteChunk(writeHandle, array, num3))
			{
				ZLog.LogError("Steam FileWriteStreamWriteChunk() failed! See: https://partner.steamgames.com/doc/api/ISteamRemoteStorage#FileWriteStreamWriteChunk");
				return false;
			}
		}
		if (!SteamRemoteStorage.FileWriteStreamClose(writeHandle))
		{
			ZLog.LogError("Steam FileWriteStreamClose() failed! possible reasons: https://partner.steamgames.com/doc/api/ISteamRemoteStorage#FileWriteStreamClose");
			return false;
		}
		return true;
	}

	public static string GetSourceString(FileSource source)
	{
		switch (source)
		{
		case FileSource.Local:
			return "$settings_localsave";
		case FileSource.Cloud:
			return "$settings_cloudsave";
		case FileSource.Legacy:
			return "$settings_legacysave";
		case FileSource.Auto:
			if (!m_cloudEnabled)
			{
				return GetSourceString(FileSource.Local);
			}
			return GetSourceString(FileSource.Cloud);
		default:
			throw new Exception();
		}
	}

	public static string[] GetFiles(FileSource fileSource, string path = null, string fileSuffix = null, string searchPattern = null)
	{
		if (m_cloudEnabled && (fileSource == FileSource.Auto || fileSource == FileSource.Cloud))
		{
			path = normalizePath(path);
			if (!string.IsNullOrEmpty(searchPattern))
			{
				searchPattern = searchPattern.Replace("*", "").ToLower();
			}
			if (!string.IsNullOrEmpty(fileSuffix))
			{
				fileSuffix = fileSuffix.Replace("*", "").ToLower();
			}
			List<string> list = new List<string>();
			int fileCount = SteamRemoteStorage.GetFileCount();
			for (int i = 0; i < fileCount; i++)
			{
				int pnFileSizeInBytes;
				string fileNameAndSize = SteamRemoteStorage.GetFileNameAndSize(i, out pnFileSizeInBytes);
				string text = normalizePath(fileNameAndSize);
				if ((string.IsNullOrEmpty(path) || (text.Length >= path.Length && text.Substring(0, path.Length) == path)) && (string.IsNullOrEmpty(searchPattern) || Path.GetFileName(text).Contains(searchPattern)) && (string.IsNullOrEmpty(fileSuffix) || Path.GetExtension(text) == fileSuffix))
				{
					list.Add(fileNameAndSize);
				}
			}
			return list.ToArray();
		}
		if (fileSource == FileSource.Cloud)
		{
			throw new Exception("Cloud not enabled");
		}
		string[] array = ((fileSuffix != null) ? Directory.GetFiles(path, fileSuffix) : Directory.GetFiles(path));
		if (searchPattern != null)
		{
			List<string> list2 = array.ToList();
			for (int num = list2.Count - 1; num >= 0; num--)
			{
				if (!Path.GetFileName(list2[num]).Contains(searchPattern))
				{
					list2.RemoveAt(num);
				}
			}
			array = list2.ToArray();
		}
		return array;
		static string normalizePath(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return str;
			}
			str = str.Replace('\\', '/');
			if (str.Length > 0 && str[0] == '.')
			{
				str = str.Substring(1);
			}
			if (str.Length > 0 && str[0] == '/')
			{
				str = str.Substring(1);
			}
			return str.ToLowerInvariant();
		}
	}

	public static bool IsFileCorrupt(string path, FileSource fileSource)
	{
		return false;
	}

	public static string[] GetCorruptFiles(FileSource fileSource)
	{
		return new string[0];
	}

	public static bool Delete(string path, FileSource fileSource)
	{
		if (fileSource == FileSource.Cloud)
		{
			if (!m_cloudEnabled)
			{
				return false;
			}
			return SteamRemoteStorage.FileDelete(path);
		}
		if (!File.Exists(path))
		{
			return false;
		}
		File.Delete(path);
		return true;
	}

	public static DateTime GetLastWriteTime(string path, FileSource fileSource)
	{
		if (m_cloudEnabled && (fileSource == FileSource.Auto || fileSource == FileSource.Cloud))
		{
			return DateTimeOffset.FromUnixTimeSeconds(SteamRemoteStorage.GetFileTimestamp(path)).DateTime.ToLocalTime();
		}
		return File.GetLastWriteTime(path);
	}

	public static ulong GetTotalCloudUsage()
	{
		if (m_cloudEnabled && SteamRemoteStorage.GetQuota(out var pnTotalBytes, out var puAvailableBytes))
		{
			return pnTotalBytes - puAvailableBytes;
		}
		return 0uL;
	}

	public static ulong GetTotalCloudCapacity()
	{
		if (m_cloudEnabled && SteamRemoteStorage.GetQuota(out var pnTotalBytes, out var _))
		{
			return pnTotalBytes;
		}
		return 0uL;
	}

	public static long GetRemainingCloudCapacity()
	{
		if (m_cloudEnabled && SteamRemoteStorage.GetQuota(out var _, out var puAvailableBytes))
		{
			return (long)puAvailableBytes;
		}
		return 0L;
	}

	public static ulong GetFileSize(string path, FileSource fileSource)
	{
		if (m_cloudEnabled && fileSource == FileSource.Cloud)
		{
			return (ulong)SteamRemoteStorage.GetFileSize(path);
		}
		return (ulong)new FileInfo(path).Length;
	}

	public static bool OperationExceedsCloudCapacity(ulong requiredBytes)
	{
		return GetRemainingCloudCapacity() < (long)requiredBytes;
	}

	public static void CheckDiskSpace(string worldSavePath, string playerProfileSavePath, FileSource worldFileSource, FileSource playerFileSource, out ulong availableFreeSpace, out ulong byteLimitWarning, out ulong byteLimitBlock)
	{
		ulong num = ((!Exists(worldSavePath, worldFileSource)) ? 104857600 : GetFileSize(worldSavePath, worldFileSource));
		ulong num2 = ((!Exists(playerProfileSavePath, playerFileSource)) ? 2097152 : GetFileSize(playerProfileSavePath, playerFileSource));
		availableFreeSpace = ulong.MaxValue;
		byteLimitWarning = (num + num2) * 4;
		byteLimitBlock = (num + num2) * 2;
		if (string.IsNullOrEmpty(worldSavePath) || worldFileSource == FileSource.Cloud)
		{
			worldSavePath = Application.persistentDataPath;
		}
		string folderName = Path.GetDirectoryName(worldSavePath);
		if (m_cloudEnabled && (worldFileSource == FileSource.Cloud || worldFileSource == FileSource.Auto || worldFileSource == FileSource.Legacy))
		{
			folderName = GetSteamPathWin();
		}
		availableFreeSpace = GetFreeSpaceWindows(folderName);
		ZLog.Log($"Available space to current user: {availableFreeSpace}. Saving is blocked if below: {byteLimitBlock} bytes. Warnings are given if below: {byteLimitWarning}");
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

	public static ulong GetFreeSpaceWindows(string folderName)
	{
		if (!GetDiskFreeSpaceEx(folderName, out var lpFreeBytesAvailable, out var _, out var _))
		{
			Debug.LogError("Error encountered while getting free disk space - returning max amount of disk space in order to not block saving.");
			return ulong.MaxValue;
		}
		return lpFreeBytesAvailable;
	}

	public static string GetSteamPathWin()
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			Type type = Type.GetType("Microsoft.Win32.Registry, mscorlib");
			if (type != null)
			{
				MethodInfo method = type.GetMethod("GetValue", new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(object)
				});
				if (method != null)
				{
					string text = (string)method.Invoke(null, new object[3] { "HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null });
					if (string.IsNullOrEmpty(text))
					{
						text = (string)method.Invoke(null, new object[3] { "HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null });
					}
					return text;
				}
			}
		}
		return Application.persistentDataPath;
	}

	public static void TerminateCloudStorage()
	{
	}

	public static bool Exists(string path, FileSource fileSource)
	{
		if (m_cloudEnabled && (fileSource == FileSource.Auto || fileSource == FileSource.Cloud))
		{
			return SteamRemoteStorage.FileExists(path);
		}
		return File.Exists(path);
	}

	public static void EnsureDirectoryExists(string path)
	{
		string directoryName = Path.GetDirectoryName(path);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
	}
}

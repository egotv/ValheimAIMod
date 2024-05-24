using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UserManagement;

public static class BlockList
{
	private static readonly HashSet<string> _blockedUsers = new HashSet<string>();

	private static readonly HashSet<string> _platformBlockedUsers = new HashSet<string>();

	private static bool _hasBeenLoaded;

	private static bool _isLoading;

	public static Func<Action<string[]>, string[]> GetPlatformBlocksFunc;

	private static readonly string _block_list_file_name = "blocked_players";

	private static readonly string _block_list_file_name_noncloud = Path.Combine(Application.persistentDataPath, _block_list_file_name) + ".txt";

	public static bool IsBlocked(string user)
	{
		if (!IsGameBlocked(user))
		{
			return IsPlatformBlocked(user);
		}
		return true;
	}

	public static bool IsGameBlocked(string user)
	{
		return _blockedUsers.Contains(user);
	}

	public static bool IsPlatformBlocked(string user)
	{
		return _platformBlockedUsers.Contains(user);
	}

	public static void Block(string user)
	{
		if (!_blockedUsers.Contains(user))
		{
			_blockedUsers.Add(user);
		}
	}

	public static void Unblock(string user)
	{
		if (_blockedUsers.Contains(user))
		{
			_blockedUsers.Remove(user);
		}
	}

	public static string GetBlockListFileName()
	{
		if (!FileHelpers.m_cloudEnabled)
		{
			return _block_list_file_name_noncloud;
		}
		return _block_list_file_name;
	}

	public static void Persist()
	{
		FileWriter fileWriter = new FileWriter(GetBlockListFileName(), FileHelpers.FileHelperType.Binary, (!FileHelpers.m_cloudEnabled) ? FileHelpers.FileSource.Local : FileHelpers.FileSource.Cloud);
		fileWriter.m_binary.Write(Encode());
		fileWriter.Finish();
	}

	public static void UpdateAvoidList(Action onUpdated = null)
	{
		UpdateAvoidList(GetPlatformBlocksFunc?.Invoke(delegate(string[] networkIds)
		{
			UpdateAvoidList(networkIds);
			onUpdated?.Invoke();
		}));
		onUpdated?.Invoke();
	}

	private static void UpdateAvoidList(string[] networkIds)
	{
		_platformBlockedUsers.Clear();
		if (networkIds != null)
		{
			foreach (string item in networkIds)
			{
				_platformBlockedUsers.Add(item);
			}
		}
	}

	public static void Load(Action onLoaded)
	{
		if (_isLoading)
		{
			return;
		}
		if (!_hasBeenLoaded)
		{
			if (FileHelpers.Exists(GetBlockListFileName(), FileHelpers.FileSource.Auto))
			{
				_isLoading = true;
				FileReader fileReader;
				try
				{
					fileReader = new FileReader(GetBlockListFileName(), (!FileHelpers.m_cloudEnabled) ? FileHelpers.FileSource.Local : FileHelpers.FileSource.Cloud, FileHelpers.FileHelperType.Stream);
				}
				catch (Exception ex)
				{
					ZLog.Log("Failed to load: " + GetBlockListFileName() + " (" + ex.Message + ")");
					_isLoading = false;
					_hasBeenLoaded = true;
					onLoaded?.Invoke();
					return;
				}
				try
				{
					BlockUsers(fileReader.m_stream.ReadToEnd());
				}
				catch (Exception ex2)
				{
					ZLog.LogError("error loading blocked_players. FileName: " + GetBlockListFileName() + ", Error: " + ex2.Message);
					fileReader.Dispose();
				}
				fileReader.Dispose();
				_isLoading = false;
			}
			_hasBeenLoaded = true;
			onLoaded?.Invoke();
		}
		else
		{
			onLoaded?.Invoke();
		}
	}

	private static byte[] Encode()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string blockedUser in _blockedUsers)
		{
			stringBuilder.Append(blockedUser).Append('\n');
		}
		return Encoding.UTF8.GetBytes(stringBuilder.ToString());
	}

	private static void BlockUsers(string textUsers)
	{
		_blockedUsers.Clear();
		string[] array = textUsers.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		foreach (string text in array)
		{
			if (!string.IsNullOrEmpty(text))
			{
				Block(text);
			}
		}
	}
}

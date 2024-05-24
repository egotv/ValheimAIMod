using System;
using System.Collections.Generic;
using PlatformTools.Core;

public class PrivilegeManager
{
	public enum Platform
	{
		Unknown,
		Steam,
		Xbox,
		PlayFab,
		None
	}

	public struct User
	{
		public readonly Platform platform;

		public readonly ulong id;

		public User(Platform p, ulong i)
		{
			platform = p;
			id = i;
		}

		public override string ToString()
		{
			return $"{platform}_{id}";
		}
	}

	public enum Result
	{
		Allowed,
		NotAllowed,
		Failed
	}

	public enum Permission
	{
		CommunicateUsingText,
		ViewTargetUserCreatedContent
	}

	private struct PrivilegeLookupKey
	{
		internal readonly Permission permission;

		internal readonly User user;

		internal PrivilegeLookupKey(Permission p, User u)
		{
			permission = p;
			user = u;
		}
	}

	private static readonly Dictionary<PrivilegeLookupKey, Result> Cache = new Dictionary<PrivilegeLookupKey, Result>();

	private static PrivilegeData? privilegeData;

	private static string s_networkUserId;

	private static Dictionary<Platform, string> s_platformPrefix = new Dictionary<Platform, string>();

	public static bool HasPrivilegeData => privilegeData.HasValue;

	public static ulong PlatformUserId
	{
		get
		{
			if (privilegeData.HasValue)
			{
				return privilegeData.Value.platformUserId;
			}
			ZLog.LogError("Can't get PlatformUserId before the privilege manager has been initialized!");
			return 0uL;
		}
	}

	public static bool CanAccessOnlineMultiplayer
	{
		get
		{
			if (privilegeData.HasValue)
			{
				return privilegeData.Value.canAccessOnlineMultiplayer;
			}
			ZLog.LogError("Can't check \"CanAccessOnlineMultiplayer\" privilege before the privilege manager has been initialized!");
			return false;
		}
	}

	public static bool CanViewUserGeneratedContentAll
	{
		get
		{
			if (privilegeData.HasValue)
			{
				return privilegeData.Value.canViewUserGeneratedContentAll;
			}
			ZLog.LogError("Can't check \"CanViewUserGeneratedContentAll\" privilege before the privilege manager has been initialized!");
			return false;
		}
	}

	public static bool CanCrossplay
	{
		get
		{
			if (privilegeData.HasValue)
			{
				return privilegeData.Value.canCrossplay;
			}
			ZLog.LogError("Can't check \"CanCrossplay\" privilege before the privilege manager has been initialized!");
			return false;
		}
	}

	public static UserType ToUserType(Platform platform)
	{
		if (platform == Platform.Xbox)
		{
			return UserType.XboxLive;
		}
		return UserType.CrossNetworkUser;
	}

	public static void SetPrivilegeData(PrivilegeData privilegeData)
	{
		if (privilegeData.platformCanAccess == null)
		{
			ZLog.LogError("The platformCanAccess delegate cannot be null!");
			throw new ArgumentException("The platformCanAccess delegate cannot be null!");
		}
		s_networkUserId = null;
		PrivilegeManager.privilegeData = privilegeData;
	}

	public static void ResetPrivilegeData()
	{
		s_networkUserId = null;
		privilegeData = null;
	}

	public static string GetNetworkUserId()
	{
		if (string.IsNullOrEmpty(s_networkUserId))
		{
			s_networkUserId = $"{GetPlatformPrefix(GetCurrentPlatform())}{PlatformUserId}";
		}
		return s_networkUserId;
	}

	public static Platform GetCurrentPlatform()
	{
		return Platform.Steam;
	}

	public static string GetPlatformName(Platform platform)
	{
		return $"{platform}";
	}

	public static string GetPlatformPrefix(Platform platform)
	{
		if (s_platformPrefix.TryGetValue(platform, out var value))
		{
			return value;
		}
		value = GetPlatformName(platform) + "_";
		s_platformPrefix.Add(platform, value);
		return value;
	}

	public static void FlushCache()
	{
		Cache.Clear();
	}

	public static void CanViewUserGeneratedContent(string user, CanAccessResult canViewUserGeneratedContentResult)
	{
		CanAccess(Permission.ViewTargetUserCreatedContent, user, canViewUserGeneratedContentResult);
	}

	public static void CanCommunicateWith(string user, CanAccessResult canCommunicateWithResult)
	{
		CanAccess(Permission.CommunicateUsingText, user, canCommunicateWithResult);
	}

	private static void CanAccess(Permission permission, string platformUser, CanAccessResult canAccessResult)
	{
		User user = ParseUser(platformUser);
		PrivilegeLookupKey key = new PrivilegeLookupKey(permission, user);
		if (Cache.TryGetValue(key, out var value))
		{
			canAccessResult(value);
		}
		else if (privilegeData.HasValue)
		{
			if (user.id == PlatformUserId)
			{
				canAccessResult(Result.Allowed);
				return;
			}
			privilegeData.Value.platformCanAccess(permission, user, delegate(Result res)
			{
				CacheAndDeliverResult(res, canAccessResult, key);
			});
		}
		else
		{
			ZLog.LogError("Can't check \"" + permission.ToString() + "\" privilege before the privilege manager has been initialized!");
			canAccessResult?.Invoke(Result.Failed);
		}
	}

	private static void CacheAndDeliverResult(Result res, CanAccessResult canAccessResult, PrivilegeLookupKey key)
	{
		if (res != Result.Failed)
		{
			Cache[key] = res;
		}
		canAccessResult(res);
	}

	public static User ParseUser(string platformUser)
	{
		User result = new User(Platform.Unknown, 0uL);
		string[] array = platformUser.Split('_', StringSplitOptions.None);
		ulong result3;
		if (array.Length == 2)
		{
			if (ulong.TryParse(array[1], out var result2))
			{
				if (array[0] == GetPlatformName(Platform.Steam))
				{
					result = new User(Platform.Steam, result2);
				}
				else if (array[0] == GetPlatformName(Platform.Xbox))
				{
					result = new User(Platform.Xbox, result2);
				}
				else if (array[0] == GetPlatformName(Platform.PlayFab))
				{
					result = new User(Platform.PlayFab, result2);
				}
			}
		}
		else if (array.Length == 1 && ulong.TryParse(array[0], out result3))
		{
			result = new User(Platform.Steam, result3);
		}
		return result;
	}

	public static Platform ParsePlatform(string platformString)
	{
		if (Enum.TryParse<Platform>(platformString, out var result))
		{
			return result;
		}
		ZLog.LogError("Failed to parse platform!");
		return Platform.Unknown;
	}
}

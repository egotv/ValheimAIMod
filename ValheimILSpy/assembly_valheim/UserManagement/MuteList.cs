using System.Collections.Generic;

namespace UserManagement;

public static class MuteList
{
	private static readonly HashSet<string> _mutedUsers = new HashSet<string>();

	public static bool IsMuted(string userId)
	{
		return _mutedUsers.Contains(userId);
	}

	public static void Mute(string userId)
	{
		_mutedUsers.Add(userId);
	}

	public static void Unmute(string userId)
	{
		_mutedUsers.Remove(userId);
	}
}

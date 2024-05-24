using System;
using PlatformTools.Core;

public class UserInfo : ISerializableParameter
{
	public string Name;

	public string Gamertag;

	public string NetworkUserId;

	public static Action<PrivilegeManager.User, Action<Profile>> GetProfile = delegate(PrivilegeManager.User user, Action<Profile> callback)
	{
		callback?.Invoke(new Profile(user.id));
	};

	public static Action<Action<Profile, Profile>> PlatformRegisterForProfileUpdates;

	public static Action<Action<Profile, Profile>> PlatformUnregisterForProfileUpdates;

	public static Func<string> GetLocalGamerTagFunc;

	public static UserInfo GetLocalUser()
	{
		return new UserInfo
		{
			Name = Game.instance.GetPlayerProfile().GetName(),
			Gamertag = GetLocalPlayerGamertag(),
			NetworkUserId = PrivilegeManager.GetNetworkUserId()
		};
	}

	public void Deserialize(ref ZPackage pkg)
	{
		Name = pkg.ReadString();
		Gamertag = pkg.ReadString();
		NetworkUserId = pkg.ReadString();
	}

	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(Name);
		pkg.Write(Gamertag);
		pkg.Write(NetworkUserId);
	}

	public string GetDisplayName(string networkUserId)
	{
		return CensorShittyWords.FilterUGC(Name, UGCType.CharacterName, networkUserId, 0L) + GamertagSuffix(Gamertag);
	}

	public void UpdateGamertag(string gamertag)
	{
		Gamertag = gamertag;
	}

	private static string GetLocalPlayerGamertag()
	{
		if (GetLocalGamerTagFunc != null)
		{
			return GetLocalGamerTagFunc();
		}
		return "";
	}

	public static string GamertagSuffix(string gamertag)
	{
		if (string.IsNullOrEmpty(gamertag))
		{
			return "";
		}
		return " [" + gamertag + "]";
	}
}

using System.Collections.Generic;

public class PlayFabMatchmakingServerData
{
	public string serverName;

	public string worldName;

	public GameVersion gameVersion;

	public List<string> modifiers;

	public uint networkVersion;

	public string networkId = "";

	public string joinCode;

	public string remotePlayerId;

	public string lobbyId;

	public string xboxUserId = "";

	public string serverIp = "";

	public string platformRestriction = "None";

	public bool isDedicatedServer;

	public bool isCommunityServer;

	public bool havePassword;

	public uint numPlayers;

	public long tickCreated;

	public override bool Equals(object obj)
	{
		if (obj is PlayFabMatchmakingServerData playFabMatchmakingServerData && remotePlayerId == playFabMatchmakingServerData.remotePlayerId && serverIp == playFabMatchmakingServerData.serverIp)
		{
			return isDedicatedServer == playFabMatchmakingServerData.isDedicatedServer;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((1416698207 * -1521134295 + EqualityComparer<string>.Default.GetHashCode(remotePlayerId)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(serverIp)) * -1521134295 + isDedicatedServer.GetHashCode();
	}

	public override string ToString()
	{
		return $"Server Name : {serverName}\nServer IP : {serverIp}\nGame Version : {gameVersion}\nNetwork Version : {networkVersion}\nPlayer ID : {remotePlayerId}\nPlayers : {numPlayers}\nLobby ID : {lobbyId}\nNetwork ID : {networkId}\nJoin Code : {joinCode}\nPlatform Restriction : {platformRestriction}\nDedicated : {isDedicatedServer}\nCommunity : {isCommunityServer}\nTickCreated : {tickCreated}\nModifiers : {modifiers}\n";
	}
}

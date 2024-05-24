using System.Collections.Generic;

public class ServerStatus
{
	private PrivilegeManager.Platform m_platformRestriction;

	private bool m_isAwaitingSteamPingResponse;

	private bool m_isAwaitingPlayFabPingResponse;

	public ServerJoinData m_joinData { get; private set; }

	public string m_hostId { get; private set; }

	public ServerPingStatus PingStatus { get; private set; }

	public OnlineStatus OnlineStatus { get; private set; }

	public bool IsCrossplay => PlatformRestriction == PrivilegeManager.Platform.None;

	public bool IsRestrictedToOwnPlatform => PlatformRestriction == PrivilegeManager.GetCurrentPlatform();

	public bool IsJoinable
	{
		get
		{
			if (!IsRestrictedToOwnPlatform)
			{
				if (PrivilegeManager.CanCrossplay)
				{
					return IsCrossplay;
				}
				return false;
			}
			return true;
		}
	}

	public uint m_playerCount { get; private set; }

	public List<string> m_modifiers { get; private set; } = new List<string>();


	public GameVersion m_gameVersion { get; private set; }

	public uint m_networkVersion { get; private set; }

	public bool m_isPasswordProtected { get; private set; }

	public PrivilegeManager.Platform PlatformRestriction
	{
		get
		{
			if (m_joinData is ServerJoinDataSteamUser)
			{
				return PrivilegeManager.Platform.Steam;
			}
			if (OnlineStatus == OnlineStatus.Online && m_platformRestriction == PrivilegeManager.Platform.Unknown)
			{
				ZLog.LogError("Platform restriction must always be set when the online status is online, but it wasn't!\nServer: " + m_joinData.m_serverName);
			}
			return m_platformRestriction;
		}
		private set
		{
			if (m_joinData is ServerJoinDataSteamUser && value != PrivilegeManager.Platform.Steam)
			{
				ZLog.LogError("Can't set platform restriction of Steam server to anything other than Steam - it's always restricted to Steam!");
			}
			else
			{
				m_platformRestriction = value;
			}
		}
	}

	private bool DoSteamPing
	{
		get
		{
			if (!(m_joinData is ServerJoinDataSteamUser))
			{
				return m_joinData is ServerJoinDataDedicated;
			}
			return true;
		}
	}

	private bool DoPlayFabPing
	{
		get
		{
			if (!(m_joinData is ServerJoinDataPlayFabUser))
			{
				return m_joinData is ServerJoinDataDedicated;
			}
			return true;
		}
	}

	public ServerStatus(ServerJoinData joinData)
	{
		m_joinData = joinData;
		OnlineStatus = OnlineStatus.Unknown;
	}

	public void UpdateStatus(OnlineStatus onlineStatus, string serverName, uint playerCount, GameVersion gameVersion, List<string> modifiers, uint networkVersion, bool isPasswordProtected, PrivilegeManager.Platform platformRestriction, string host, bool affectPingStatus = true)
	{
		PlatformRestriction = platformRestriction;
		OnlineStatus = onlineStatus;
		m_joinData.m_serverName = serverName;
		m_playerCount = playerCount;
		m_gameVersion = gameVersion;
		m_modifiers = modifiers;
		m_networkVersion = networkVersion;
		m_isPasswordProtected = isPasswordProtected;
		m_hostId = host;
		if (affectPingStatus)
		{
			switch (onlineStatus)
			{
			case OnlineStatus.Online:
				PingStatus = ServerPingStatus.Success;
				break;
			case OnlineStatus.Offline:
				PingStatus = ServerPingStatus.CouldNotReach;
				break;
			default:
				PingStatus = ServerPingStatus.NotStarted;
				break;
			}
		}
	}

	private void PlayFabPingSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (PingStatus == ServerPingStatus.AwaitingResponse && OnlineStatus != 0)
		{
			if (serverData != null)
			{
				UpdateStatus(OnlineStatus.Online, serverData.serverName, serverData.numPlayers, serverData.gameVersion, serverData.modifiers, serverData.networkVersion, serverData.havePassword, PrivilegeManager.ParsePlatform(serverData.platformRestriction), PrivilegeManager.Platform.Xbox.ToString() + "_" + serverData.xboxUserId, affectPingStatus: false);
			}
			m_isAwaitingPlayFabPingResponse = false;
		}
	}

	private void PlayFabPingFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (PingStatus == ServerPingStatus.AwaitingResponse)
		{
			m_isAwaitingPlayFabPingResponse = false;
		}
	}

	public void Ping()
	{
		PingStatus = ServerPingStatus.AwaitingResponse;
		if (DoPlayFabPing)
		{
			if (!PlayFabManager.IsLoggedIn)
			{
				return;
			}
			if (m_joinData is ServerJoinDataPlayFabUser)
			{
				ZPlayFabMatchmaking.CheckHostOnlineStatus((m_joinData as ServerJoinDataPlayFabUser).m_remotePlayerId, PlayFabPingSuccess, PlayFabPingFailed);
			}
			else if (m_joinData is ServerJoinDataDedicated)
			{
				ZPlayFabMatchmaking.FindHostByIp((m_joinData as ServerJoinDataDedicated).GetIPPortString(), PlayFabPingSuccess, PlayFabPingFailed);
			}
			else
			{
				ZLog.LogError("Tried to ping an unsupported server type with server data " + m_joinData.ToString());
			}
			m_isAwaitingPlayFabPingResponse = true;
		}
		if (DoSteamPing)
		{
			m_isAwaitingSteamPingResponse = true;
		}
	}

	private void Update()
	{
		if (!DoSteamPing || !m_isAwaitingSteamPingResponse)
		{
			return;
		}
		ServerStatus status = null;
		if (ZSteamMatchmaking.instance.CheckIfOnline(m_joinData, ref status))
		{
			if (status.m_joinData != null && status.OnlineStatus == OnlineStatus.Online && OnlineStatus != 0)
			{
				UpdateStatus(OnlineStatus.Online, status.m_joinData.m_serverName, status.m_playerCount, status.m_gameVersion, status.m_modifiers, status.m_networkVersion, status.m_isPasswordProtected, status.PlatformRestriction, status.m_hostId);
			}
			m_isAwaitingSteamPingResponse = false;
		}
	}

	public bool TryGetResult()
	{
		Update();
		uint num = 0u;
		uint num2 = 0u;
		if (DoPlayFabPing)
		{
			num++;
			if (!m_isAwaitingPlayFabPingResponse)
			{
				num2++;
				if (OnlineStatus == OnlineStatus.Online)
				{
					PingStatus = ServerPingStatus.Success;
					return true;
				}
			}
		}
		if (DoSteamPing)
		{
			num++;
			if (!m_isAwaitingSteamPingResponse)
			{
				num2++;
				if (OnlineStatus == OnlineStatus.Online)
				{
					PingStatus = ServerPingStatus.Success;
					return true;
				}
			}
		}
		if (num == num2)
		{
			PingStatus = ServerPingStatus.CouldNotReach;
			OnlineStatus = OnlineStatus.Offline;
			return true;
		}
		return false;
	}

	public void Reset()
	{
		PingStatus = ServerPingStatus.NotStarted;
		OnlineStatus = OnlineStatus.Unknown;
		m_playerCount = 0u;
		m_gameVersion = default(GameVersion);
		m_modifiers = null;
		m_networkVersion = 0u;
		m_isPasswordProtected = false;
		if (!(m_joinData is ServerJoinDataSteamUser))
		{
			PlatformRestriction = PrivilegeManager.Platform.Unknown;
		}
		m_isAwaitingSteamPingResponse = false;
		m_isAwaitingPlayFabPingResponse = false;
	}
}

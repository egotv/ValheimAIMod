using System;
using System.Collections.Generic;
using System.Threading;
using PartyCSharpSDK;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using PlayFab.Party;
using UnityEngine;

public class ZPlayFabMatchmaking
{
	private enum State
	{
		Uninitialized,
		Creating,
		RegenerateJoinCode,
		Active
	}

	private static ZPlayFabMatchmaking m_instance;

	private static string m_publicIP = "";

	private static readonly object m_mtx = new object();

	private static Thread m_publicIpLookupThread;

	public const uint JoinStringLength = 6u;

	public const uint MaxPlayers = 10u;

	internal const int NumSearchPages = 4;

	public const string RemotePlayerIdSearchKey = "string_key1";

	public const string IsActiveSearchKey = "string_key2";

	public const string IsCommunityServerSearchKey = "string_key3";

	public const string JoinCodeSearchKey = "string_key4";

	public const string ServerNameSearchKey = "string_key5";

	public const string GameVersionSearchKey = "string_key6";

	public const string IsDedicatedServerSearchKey = "string_key7";

	public const string XboxUserIdSearchKey = "string_key8";

	public const string CreatedSearchKey = "string_key9";

	public const string ServerIpSearchKey = "string_key10";

	public const string PageSearchKey = "number_key11";

	public const string PlatformRestrictionKey = "string_key12";

	public const string NetworkVersionSearchKey = "number_key13";

	public const string ModifiersSearchKey = "string_key14";

	private const int NumStringSearchKeys = 14;

	private State m_state;

	private PlayFabMatchmakingServerData m_serverData;

	private int m_retries;

	private float m_retryIn = -1f;

	private const float LostNetworkRetryDuration = 30f;

	private float m_lostNetworkRetryIn = -1f;

	private bool m_isConnectingToNetwork;

	private bool m_isResettingNetwork;

	private float m_submitBackgroundSearchIn = -1f;

	private int m_serverPort = -1;

	private float m_refreshLobbyTimer;

	private const float RefreshLobbyDurationMin = 540f;

	private const float RefreshLobbyDurationMax = 840f;

	private const float DurationBetwenBackgroundSearches = 2f;

	private readonly List<ZPlayFabLobbySearch> m_activeSearches = new List<ZPlayFabLobbySearch>();

	private readonly Queue<ZPlayFabLobbySearch> m_pendingSearches = new Queue<ZPlayFabLobbySearch>();

	private readonly HashSet<string> m_friends = new HashSet<string>();

	private Action m_pendingRegisterServer;

	public static ZPlayFabMatchmaking instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = new ZPlayFabMatchmaking();
			}
			return m_instance;
		}
	}

	public static string JoinCode { get; internal set; }

	public static string MyXboxUserId { get; set; } = "";


	public static string PublicIP
	{
		get
		{
			lock (m_mtx)
			{
				return m_publicIP;
			}
		}
		private set
		{
			lock (m_mtx)
			{
				m_publicIP = value;
			}
		}
	}

	public static event ZPlayFabMatchmakeServerStarted ServerStarted;

	public static event ZPlayFabMatchmakeServerStopped ServerStopped;

	public static event ZPlayFabMatchmakeLobbyLeftCallback LobbyLeft;

	public static void Initialize(bool isServer)
	{
		JoinCode = (isServer ? "" : "000000");
	}

	public void Update(float deltaTime)
	{
		if (!ReconnectNetwork(deltaTime))
		{
			RefreshLobby(deltaTime);
			RetryJoinCodeUniquenessCheck(deltaTime);
			UpdateActiveLobbySearches(deltaTime);
			UpdateBackgroundLobbySearches(deltaTime);
		}
	}

	private bool IsJoinedToNetwork()
	{
		if (m_serverData != null)
		{
			return !string.IsNullOrEmpty(m_serverData.networkId);
		}
		return false;
	}

	private bool IsReconnectNetworkTimerActive()
	{
		return m_lostNetworkRetryIn > 0f;
	}

	private void StartReconnectNetworkTimer(int code = -1)
	{
		m_lostNetworkRetryIn = 30f;
		if (DoFastRecovery(code))
		{
			ZLog.Log("PlayFab host fast recovery");
			m_lostNetworkRetryIn = 12f;
		}
	}

	private static bool DoFastRecovery(int code)
	{
		if (code != 63)
		{
			return code == 11;
		}
		return true;
	}

	private void StopReconnectNetworkTimer()
	{
		m_isResettingNetwork = false;
		m_lostNetworkRetryIn = -1f;
		if (m_serverData != null && !IsJoinedToNetwork())
		{
			CreateAndJoinNetwork();
		}
	}

	private bool ReconnectNetwork(float deltaTime)
	{
		if (!IsReconnectNetworkTimerActive())
		{
			if (IsJoinedToNetwork() && !PlayFabMultiplayerManager.Get().IsConnectedToNetworkState())
			{
				PlayFabMultiplayerManager.Get().ResetParty();
				StartReconnectNetworkTimer();
				m_serverData.networkId = null;
			}
			return false;
		}
		m_lostNetworkRetryIn -= deltaTime;
		if (m_lostNetworkRetryIn <= 0f)
		{
			ZLog.Log($"PlayFab reconnect server '{m_serverData.serverName}'");
			m_isConnectingToNetwork = false;
			m_serverData.networkId = null;
			StopReconnectNetworkTimer();
		}
		else if (!m_isConnectingToNetwork && !m_isResettingNetwork && m_lostNetworkRetryIn <= 12f)
		{
			PlayFabMultiplayerManager.Get().ResetParty();
			m_isResettingNetwork = true;
			m_isConnectingToNetwork = false;
		}
		return true;
	}

	private void StartRefreshLobbyTimer()
	{
		m_refreshLobbyTimer = UnityEngine.Random.Range(540f, 840f);
	}

	private void RefreshLobby(float deltaTime)
	{
		if (m_serverData == null || m_serverData.networkId == null)
		{
			return;
		}
		bool flag = m_serverData.isDedicatedServer && string.IsNullOrEmpty(m_serverData.serverIp) && !string.IsNullOrEmpty(PublicIP);
		m_refreshLobbyTimer -= deltaTime;
		if (m_refreshLobbyTimer < 0f || flag)
		{
			StartRefreshLobbyTimer();
			UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest
			{
				LobbyId = m_serverData.lobbyId
			};
			if (flag)
			{
				m_serverData.serverIp = GetServerIP();
				ZLog.Log("Updating lobby with public IP " + m_serverData.serverIp);
				Dictionary<string, string> searchData = new Dictionary<string, string> { ["string_key10"] = m_serverData.serverIp };
				updateLobbyRequest.SearchData = searchData;
			}
			PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, delegate
			{
				ZLog.Log($"Lobby {m_serverData.lobbyId} for world '{m_serverData.serverName}' and network {m_serverData.networkId} refreshed");
			}, OnRefreshFailed);
		}
	}

	private void OnRefreshFailed(PlayFabError err)
	{
		CreateLobby(refresh: true, delegate
		{
			ZLog.Log($"Lobby {m_serverData.lobbyId} for world '{m_serverData.serverName}' recreated");
		}, delegate(PlayFabError err)
		{
			ZLog.LogWarning($"Failed to refresh lobby {m_serverData.lobbyId} for world '{m_serverData.serverName}': {err.GenerateErrorReport()}");
		});
	}

	private void RetryJoinCodeUniquenessCheck(float deltaTime)
	{
		if (m_retryIn > 0f)
		{
			m_retryIn -= deltaTime;
			if (m_retryIn <= 0f)
			{
				CheckJoinCodeIsUnique();
			}
		}
	}

	private void UpdateActiveLobbySearches(float deltaTime)
	{
		for (int i = 0; i < m_activeSearches.Count; i++)
		{
			ZPlayFabLobbySearch zPlayFabLobbySearch = m_activeSearches[i];
			if (zPlayFabLobbySearch.IsDone)
			{
				m_activeSearches.RemoveAt(i);
				i--;
			}
			else
			{
				zPlayFabLobbySearch.Update(deltaTime);
			}
		}
	}

	private void UpdateBackgroundLobbySearches(float deltaTime)
	{
		if (m_submitBackgroundSearchIn >= 0f)
		{
			m_submitBackgroundSearchIn -= deltaTime;
		}
		else if (m_pendingSearches.Count > 0)
		{
			m_submitBackgroundSearchIn = 2f;
			ZPlayFabLobbySearch zPlayFabLobbySearch = m_pendingSearches.Dequeue();
			zPlayFabLobbySearch.FindLobby();
			m_activeSearches.Add(zPlayFabLobbySearch);
		}
	}

	private void OnFailed(string what, PlayFabError error)
	{
		ZLog.LogError("PlayFab " + what + " failed: " + error.ToString());
		UnregisterServer();
	}

	private void OnSessionUpdated(State newState)
	{
		m_state = newState;
		switch (m_state)
		{
		case State.Creating:
			ZLog.Log($"Session \"{m_serverData.serverName}\" registered with join code {JoinCode}");
			m_retries = 100;
			CheckJoinCodeIsUnique();
			break;
		case State.RegenerateJoinCode:
			RegenerateLobbyJoinCode();
			ZLog.Log($"Created new join code {JoinCode} for session \"{m_serverData.serverName}\"");
			break;
		case State.Active:
			ZPlayFabMatchmaking.ServerStarted?.Invoke(m_serverData.remotePlayerId);
			ZLog.Log($"Session \"{m_serverData.serverName}\" with join code {JoinCode} is active with {m_serverData.numPlayers} player(s)");
			break;
		}
	}

	private void UpdateNumPlayers(string info)
	{
		m_serverData.numPlayers = ZPlayFabSocket.NumSockets();
		if (!m_serverData.isDedicatedServer)
		{
			m_serverData.numPlayers++;
		}
		ZLog.Log($"{info} server \"{m_serverData.serverName}\" that has join code {JoinCode}, now {m_serverData.numPlayers} player(s)");
	}

	private void OnRemotePlayerLeft(object sender, PlayFabPlayer player)
	{
		ZPlayFabSocket.LostConnection(player);
		UpdateNumPlayers("Player connection lost");
	}

	private void OnRemotePlayerJoined(object sender, PlayFabPlayer player)
	{
		StopReconnectNetworkTimer();
		ZPlayFabSocket.QueueConnection(player);
		UpdateNumPlayers("Player joined");
	}

	private void OnNetworkJoined(object sender, string networkId)
	{
		ZLog.Log($"Joined PlayFab Party network with ID \"{networkId}\"");
		if (m_serverData.networkId == null || m_serverData.networkId != networkId)
		{
			m_serverData.networkId = networkId;
			CreateLobby(refresh: false, OnCreateLobbySuccess, delegate(PlayFabError error)
			{
				OnFailed("create lobby", error);
			});
		}
		m_isConnectingToNetwork = false;
		m_isResettingNetwork = false;
		StopReconnectNetworkTimer();
		StartRefreshLobbyTimer();
	}

	private void CreateLobby(bool refresh, Action<CreateLobbyResult> resultCallback, Action<PlayFabError> errorCallback)
	{
		PlayFab.MultiplayerModels.EntityKey entityKeyForLocalUser = GetEntityKeyForLocalUser();
		List<Member> members = new List<Member>
		{
			new Member
			{
				MemberEntity = entityKeyForLocalUser
			}
		};
		Dictionary<string, string> lobbyData = new Dictionary<string, string>
		{
			[PlayFabAttrKey.HavePassword.ToKeyString()] = m_serverData.havePassword.ToString(),
			[PlayFabAttrKey.WorldName.ToKeyString()] = m_serverData.worldName,
			[PlayFabAttrKey.NetworkId.ToKeyString()] = m_serverData.networkId
		};
		string value = "";
		if (ServerOptionsGUI.TryConvertModifierKeysToCompactKVP<Dictionary<string, string>>(m_serverData.modifiers, out var result))
		{
			value = StringUtils.EncodeDictionaryAsString(result, encloseInQuotes: false);
		}
		Dictionary<string, string> searchData = new Dictionary<string, string>
		{
			["string_key9"] = DateTime.UtcNow.Ticks.ToString(),
			["string_key5"] = m_serverData.serverName,
			["string_key3"] = m_serverData.isCommunityServer.ToString(),
			["string_key4"] = m_serverData.joinCode,
			["string_key2"] = refresh.ToString(),
			["string_key1"] = m_serverData.remotePlayerId,
			["string_key6"] = m_serverData.gameVersion.ToString(),
			["string_key14"] = value,
			["number_key13"] = m_serverData.networkVersion.ToString(),
			["string_key7"] = m_serverData.isDedicatedServer.ToString(),
			["string_key8"] = m_serverData.xboxUserId,
			["string_key10"] = m_serverData.serverIp,
			["number_key11"] = GetSearchPage().ToString(),
			["string_key12"] = (PrivilegeManager.CanCrossplay ? "None" : PrivilegeManager.GetCurrentPlatform().ToString())
		};
		CreateLobbyRequest request = new CreateLobbyRequest
		{
			AccessPolicy = AccessPolicy.Public,
			MaxPlayers = 10u,
			Members = members,
			Owner = entityKeyForLocalUser,
			LobbyData = lobbyData,
			SearchData = searchData
		};
		if (m_serverData.isCommunityServer)
		{
			AddNameSearchFilter(searchData, m_serverData.serverName);
		}
		PlayFabMultiplayerAPI.CreateLobby(request, resultCallback, errorCallback);
	}

	private static int GetSearchPage()
	{
		return UnityEngine.Random.Range(0, 4);
	}

	internal static PlayFab.MultiplayerModels.EntityKey GetEntityKeyForLocalUser()
	{
		PlayFab.ClientModels.EntityKey entity = PlayFabManager.instance.Entity;
		return new PlayFab.MultiplayerModels.EntityKey
		{
			Id = entity.Id,
			Type = entity.Type
		};
	}

	private void OnCreateLobbySuccess(CreateLobbyResult result)
	{
		ZLog.Log($"Created PlayFab lobby with ID \"{result.LobbyId}\", ConnectionString \"{result.ConnectionString}\" and owned by \"{m_serverData.remotePlayerId}\"");
		m_serverData.lobbyId = result.LobbyId;
		OnSessionUpdated(State.Creating);
	}

	private void GenerateJoinCode()
	{
		JoinCode = UnityEngine.Random.Range(0, (int)Math.Pow(10.0, 6.0)).ToString("D" + 6u);
		m_serverData.joinCode = JoinCode;
	}

	private void RegenerateLobbyJoinCode()
	{
		GenerateJoinCode();
		PlayFabMultiplayerAPI.UpdateLobby(new UpdateLobbyRequest
		{
			LobbyId = m_serverData.lobbyId,
			SearchData = new Dictionary<string, string> { ["string_key4"] = JoinCode }
		}, OnSetLobbyJoinCodeSuccess, delegate(PlayFabError error)
		{
			OnFailed("set lobby join-code", error);
		});
	}

	private void OnSetLobbyJoinCodeSuccess(LobbyEmptyResult _)
	{
		CheckJoinCodeIsUnique();
	}

	private void CheckJoinCodeIsUnique()
	{
		PlayFabMultiplayerAPI.FindLobbies(new FindLobbiesRequest
		{
			Filter = string.Format("{0} eq '{1}'", "string_key4", JoinCode)
		}, OnCheckJoinCodeSuccess, delegate(PlayFabError error)
		{
			OnFailed("find lobbies", error);
		});
	}

	private void ScheduleJoinCodeCheck()
	{
		m_retryIn = 1f;
	}

	private void OnCheckJoinCodeSuccess(FindLobbiesResult result)
	{
		if (result.Lobbies.Count == 0)
		{
			if (m_retries > 0)
			{
				m_retries--;
				ZLog.Log("Retry join-code check " + m_retries);
				ScheduleJoinCodeCheck();
			}
			else
			{
				ZLog.LogWarning("Zero lobbies returned, should be at least one");
				UnregisterServer();
			}
		}
		else if (result.Lobbies.Count == 1 && result.Lobbies[0].Owner.Id == GetEntityKeyForLocalUser().Id)
		{
			ActivateSession();
		}
		else
		{
			OnSessionUpdated(State.RegenerateJoinCode);
		}
	}

	private void ActivateSession()
	{
		PlayFabMultiplayerAPI.UpdateLobby(new UpdateLobbyRequest
		{
			LobbyId = m_serverData.lobbyId,
			SearchData = new Dictionary<string, string> { ["string_key2"] = true.ToString() }
		}, OnActivateLobbySuccess, delegate(PlayFabError error)
		{
			OnFailed("activate lobby", error);
		});
	}

	private void OnActivateLobbySuccess(LobbyEmptyResult _)
	{
		OnSessionUpdated(State.Active);
	}

	public void RegisterServer(string name, bool havePassword, bool isCommunityServer, GameVersion gameVersion, List<string> modifiers, uint networkVersion, string worldName, bool needServerAccount = true)
	{
		bool flag = false;
		if (!PlayFabMultiplayerAPI.IsEntityLoggedIn())
		{
			ZLog.LogWarning("Calling ZPlayFabMatchmaking.RegisterServer() without logged in user");
			m_pendingRegisterServer = delegate
			{
				RegisterServer(name, havePassword, isCommunityServer, gameVersion, modifiers, networkVersion, worldName, needServerAccount);
			};
			return;
		}
		m_serverData = new PlayFabMatchmakingServerData
		{
			havePassword = havePassword,
			isCommunityServer = isCommunityServer,
			isDedicatedServer = flag,
			remotePlayerId = PlayFabManager.instance.Entity.Id,
			serverName = name,
			gameVersion = gameVersion,
			modifiers = modifiers,
			networkVersion = networkVersion,
			worldName = worldName
		};
		m_serverData.serverIp = GetServerIP();
		UpdateNumPlayers("New session");
		ZLog.Log(string.Format("Register PlayFab server \"{0}\"{1}", name, flag ? (" with IP " + m_serverData.serverIp) : ""));
		GenerateJoinCode();
		CreateAndJoinNetwork();
		PlayFabMultiplayerManager playFabMultiplayerManager = PlayFabMultiplayerManager.Get();
		playFabMultiplayerManager.OnNetworkJoined -= OnNetworkJoined;
		playFabMultiplayerManager.OnNetworkJoined += OnNetworkJoined;
		playFabMultiplayerManager.OnNetworkChanged -= OnNetworkChanged;
		playFabMultiplayerManager.OnNetworkChanged += OnNetworkChanged;
		playFabMultiplayerManager.OnError -= OnNetworkError;
		playFabMultiplayerManager.OnError += OnNetworkError;
		playFabMultiplayerManager.OnRemotePlayerJoined -= OnRemotePlayerJoined;
		playFabMultiplayerManager.OnRemotePlayerJoined += OnRemotePlayerJoined;
		playFabMultiplayerManager.OnRemotePlayerLeft -= OnRemotePlayerLeft;
		playFabMultiplayerManager.OnRemotePlayerLeft += OnRemotePlayerLeft;
	}

	private string GetServerIP()
	{
		if (!m_serverData.isDedicatedServer || string.IsNullOrEmpty(PublicIP))
		{
			return "";
		}
		return $"{PublicIP}:{m_serverPort}";
	}

	public static void LookupPublicIP()
	{
		if (string.IsNullOrEmpty(PublicIP) && m_publicIpLookupThread == null)
		{
			m_publicIpLookupThread = new Thread(BackgroundLookupPublicIP);
			m_publicIpLookupThread.Name = "PlayfabLooupThread";
			m_publicIpLookupThread.Start();
		}
	}

	private static void BackgroundLookupPublicIP(object obj)
	{
		while (string.IsNullOrEmpty(PublicIP))
		{
			PublicIP = ZNet.GetPublicIP();
			Thread.Sleep(10);
		}
	}

	private void CreateAndJoinNetwork()
	{
		PlayFabNetworkConfiguration networkConfiguration = new PlayFabNetworkConfiguration
		{
			MaxPlayerCount = 10u,
			DirectPeerConnectivityOptions = (PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS)15u
		};
		ZLog.Log($"Server '{m_serverData.serverName}' begin PlayFab create and join network for server ");
		PlayFabMultiplayerManager.Get().CreateAndJoinNetwork(networkConfiguration);
		m_isConnectingToNetwork = true;
		StartReconnectNetworkTimer();
	}

	public void UnregisterServer()
	{
		Debug.Log("ZPlayFabMatchmaking::UnregisterServer - unregistering server now. State: " + m_state);
		if (m_state == State.Active)
		{
			ZPlayFabMatchmaking.ServerStopped?.Invoke();
		}
		if (m_state != 0)
		{
			ZLog.Log($"Unregister PlayFab server \"{m_serverData.serverName}\" and leaving network \"{m_serverData.networkId}\"");
			DeleteLobby(m_serverData.lobbyId);
			ZPlayFabSocket.DestroyListenSocket();
			PlayFabMultiplayerManager.Get().LeaveNetwork();
			PlayFabMultiplayerManager.Get().OnNetworkJoined -= OnNetworkJoined;
			PlayFabMultiplayerManager.Get().OnNetworkChanged -= OnNetworkChanged;
			PlayFabMultiplayerManager.Get().OnError -= OnNetworkError;
			PlayFabMultiplayerManager.Get().OnRemotePlayerJoined -= OnRemotePlayerJoined;
			PlayFabMultiplayerManager.Get().OnRemotePlayerLeft -= OnRemotePlayerLeft;
			m_serverData = null;
			m_retries = 0;
			m_state = State.Uninitialized;
			StopReconnectNetworkTimer();
		}
		else
		{
			ZPlayFabMatchmaking.LobbyLeft?.Invoke(success: true);
		}
	}

	internal static void ResetParty()
	{
		if (instance != null && instance.IsJoinedToNetwork())
		{
			instance.OnNetworkError(null, new PlayFabMultiplayerManagerErrorArgs(9999, "Forced ResetParty", PlayFabMultiplayerManagerErrorType.Error));
		}
		else
		{
			ZLog.Log("No active PlayFab Party to reset");
		}
	}

	private void OnNetworkError(object sender, PlayFabMultiplayerManagerErrorArgs args)
	{
		if (!IsReconnectNetworkTimerActive())
		{
			ZLog.LogWarning($"PlayFab network error in session '{m_serverData.serverName}' and network {m_serverData.networkId} with type '{args.Type}' and code '{args.Code}': {args.Message}");
			StartReconnectNetworkTimer(args.Code);
		}
	}

	private void OnNetworkChanged(object sender, string newNetworkId)
	{
		ZLog.LogWarning($"PlayFab network session '{m_serverData.serverName}' and network {m_serverData.networkId} changed to network {newNetworkId}");
		m_serverData.networkId = newNetworkId;
		Dictionary<string, string> lobbyData = new Dictionary<string, string> { [PlayFabAttrKey.NetworkId.ToKeyString()] = m_serverData.networkId };
		PlayFabMultiplayerAPI.UpdateLobby(new UpdateLobbyRequest
		{
			LobbyId = m_serverData.lobbyId,
			LobbyData = lobbyData
		}, delegate
		{
			ZLog.Log($"Lobby {m_serverData.lobbyId} for world '{m_serverData.serverName}' change to network {m_serverData.networkId}");
		}, OnRefreshFailed);
	}

	private static void DeleteLobby(string lobbyId)
	{
		PlayFabMultiplayerAPI.UpdateLobby(new UpdateLobbyRequest
		{
			LobbyId = lobbyId,
			SearchData = new Dictionary<string, string> { ["string_key2"] = false.ToString() }
		}, delegate
		{
			ZLog.Log("Deactivated PlayFab lobby " + lobbyId);
		}, delegate(PlayFabError error)
		{
			ZLog.LogWarning($"Failed to deactive lobby '{lobbyId}': {error.GenerateErrorReport()}");
		});
		LeaveLobby(lobbyId);
	}

	public static void LeaveLobby(string lobbyId)
	{
		PlayFabMultiplayerAPI.LeaveLobby(new LeaveLobbyRequest
		{
			LobbyId = lobbyId,
			MemberEntity = GetEntityKeyForLocalUser()
		}, delegate
		{
			ZLog.Log("Left PlayFab lobby " + lobbyId);
			ZPlayFabMatchmaking.LobbyLeft?.Invoke(success: true);
		}, delegate(PlayFabError error)
		{
			ZLog.LogError($"Failed to leave lobby '{lobbyId}': {error.GenerateErrorReport()}");
			ZPlayFabMatchmaking.LobbyLeft?.Invoke(success: false);
		});
	}

	public static void LeaveEmptyLobby()
	{
		ZPlayFabMatchmaking.LobbyLeft?.Invoke(success: true);
	}

	public static void ResolveJoinCode(string joinCode, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction)
	{
		string searchFilter = string.Format("{0} eq '{1}' and {2} eq '{3}'", "string_key4", joinCode, "string_key2", true.ToString());
		instance.m_activeSearches.Add(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, null));
	}

	public static void CheckHostOnlineStatus(string hostName, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby = false)
	{
		FindHostSession(string.Format("{0} eq '{1}' and {2} eq '{3}'", "string_key1", hostName, "string_key2", true.ToString()), successAction, failedAction, joinLobby);
	}

	public static void FindHostByIp(string hostIp, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby = false)
	{
		FindHostSession(string.Format("{0} eq '{1}' and {2} eq '{3}'", "string_key10", hostIp, "string_key2", true.ToString()), successAction, failedAction, joinLobby);
	}

	private static Dictionary<char, int> CreateCharHistogram(string str)
	{
		Dictionary<char, int> dictionary = new Dictionary<char, int>();
		string text = str.ToLowerInvariant();
		foreach (char key in text)
		{
			if (dictionary.ContainsKey(key))
			{
				dictionary[key]++;
			}
			else
			{
				dictionary.Add(key, 1);
			}
		}
		return dictionary;
	}

	private static void AddNameSearchFilter(Dictionary<string, string> searchData, string serverName)
	{
		Dictionary<char, int> dictionary = CreateCharHistogram(serverName);
		for (char c = 'a'; c <= 'z'; c = (char)(c + 1))
		{
			if (CharToKeyName(c, out var key))
			{
				dictionary.TryGetValue(c, out var value);
				searchData.Add(key, value.ToString());
			}
		}
	}

	private static string CreateNameSearchFilter(string name)
	{
		Dictionary<char, int> dictionary = CreateCharHistogram(name);
		string text = "";
		string text2 = name.ToLowerInvariant();
		foreach (char c in text2)
		{
			if (CharToKeyName(c, out var key) && dictionary.TryGetValue(c, out var value))
			{
				text += $" and {key} ge {value}";
			}
		}
		return text;
	}

	private static bool CharToKeyName(char ch, out string key)
	{
		int num = "eariotnslcudpmhgbfywkvxzjq".IndexOf(ch);
		if (num < 0 || num >= 16)
		{
			key = null;
			return false;
		}
		key = $"number_key{num + 14 + 1}";
		return true;
	}

	private void CancelPendingSearches()
	{
		foreach (ZPlayFabLobbySearch activeSearch in instance.m_activeSearches)
		{
			activeSearch.Cancel();
		}
		m_pendingSearches.Clear();
	}

	private static void FindHostSession(string searchFilter, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby)
	{
		if (joinLobby)
		{
			instance.CancelPendingSearches();
			instance.m_activeSearches.Add(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, joinLobby: true));
		}
		else
		{
			instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, joinLobby: false));
		}
	}

	public static void ListServers(string nameFilter, ZPlayFabMatchmakingSuccessCallback serverFoundAction, ZPlayFabMatchmakingFailedCallback listDone, bool listP2P = false)
	{
		instance.CancelPendingSearches();
		string text = (listP2P ? string.Format("{0} eq '{1}' and {2} eq '{3}'", "string_key7", false.ToString(), "string_key2", true.ToString()) : string.Format("{0} eq '{1}' and {2} eq '{3}'", "string_key3", true.ToString(), "string_key2", true.ToString()));
		text = ((!string.IsNullOrEmpty(nameFilter)) ? (text + CreateNameSearchFilter(nameFilter)) : (text + string.Format(" and {0} eq {1}", "number_key13", 27u)));
		if (listP2P)
		{
			foreach (string allFriend in GetAllFriends())
			{
				string searchFilter = CreateSearchFilter(text, includeFriend: true, allFriend, PrivilegeManager.CanCrossplay);
				instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(serverFoundAction, listDone, searchFilter, null, findFriendsOnly: true));
			}
			return;
		}
		string searchFilter2 = CreateSearchFilter(text, includeFriend: false, "", PrivilegeManager.CanCrossplay);
		instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(serverFoundAction, listDone, searchFilter2, nameFilter));
	}

	private static string CreateSearchFilter(string baseFilter, bool includeFriend, string friend = "", bool isCrossplay = false)
	{
		string text = (isCrossplay ? "None" : PrivilegeManager.GetCurrentPlatform().ToString());
		return baseFilter + (includeFriend ? (" and string_key8 eq '" + friend + "'") : "") + " and string_key12 eq '" + text + "'";
	}

	public static void AddFriend(string xboxUserId)
	{
		instance.m_friends.Add(xboxUserId);
	}

	public static bool IsFriendWith(string xboxUserId)
	{
		return instance.m_friends.Contains(xboxUserId);
	}

	public static HashSet<string> GetAllFriends()
	{
		return instance.m_friends;
	}

	public static bool IsJoinCode(string joinString)
	{
		int result;
		if ((long)joinString.Length == 6)
		{
			return int.TryParse(joinString, out result);
		}
		return false;
	}

	public static void SetDataPort(int serverPort)
	{
		if (instance != null)
		{
			instance.m_serverPort = serverPort;
		}
	}

	public static void OnLogin()
	{
		if (instance != null && instance.m_pendingRegisterServer != null)
		{
			instance.m_pendingRegisterServer();
			instance.m_pendingRegisterServer = null;
		}
	}

	internal static void ForwardProgress()
	{
		if (instance != null)
		{
			instance.StopReconnectNetworkTimer();
		}
	}
}

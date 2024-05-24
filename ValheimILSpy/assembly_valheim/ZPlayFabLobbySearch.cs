using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.MultiplayerModels;

internal class ZPlayFabLobbySearch
{
	private delegate void QueueableAPICall();

	private readonly ZPlayFabMatchmakingSuccessCallback m_successAction;

	private readonly ZPlayFabMatchmakingFailedCallback m_failedAction;

	private readonly string m_searchFilter;

	private readonly string m_serverFilter;

	private readonly Queue<int> m_pages;

	private readonly bool m_joinLobby;

	private readonly bool m_verboseLog;

	private readonly bool m_findFriendsOnly;

	private int m_retries;

	private float m_retryIn = -1f;

	private const float rateLimit = 2f;

	private DateTime m_previousAPICallTime = DateTime.MinValue;

	private Queue<QueueableAPICall> m_APICallQueue = new Queue<QueueableAPICall>();

	internal bool IsDone { get; private set; }

	internal ZPlayFabLobbySearch(ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, string searchFilter, string serverFilter, bool findFriendsOnly = false)
	{
		m_successAction = successAction;
		m_failedAction = failedAction;
		m_searchFilter = searchFilter;
		m_serverFilter = serverFilter;
		m_findFriendsOnly = findFriendsOnly;
		if (serverFilter == null)
		{
			FindLobby();
			m_retries = 1;
		}
		else
		{
			m_pages = CreatePages();
		}
	}

	internal ZPlayFabLobbySearch(ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, string searchFilter, bool joinLobby)
	{
		m_successAction = successAction;
		m_failedAction = failedAction;
		m_searchFilter = searchFilter;
		m_joinLobby = joinLobby;
		if (joinLobby)
		{
			FindLobby();
			m_retries = 3;
		}
	}

	private Queue<int> CreatePages()
	{
		Queue<int> queue = new Queue<int>();
		for (int i = 0; i < 4; i++)
		{
			queue.Enqueue(i);
		}
		return queue;
	}

	internal void Update(float deltaTime)
	{
		if (m_retryIn > 0f)
		{
			m_retryIn -= deltaTime;
			if (m_retryIn <= 0f)
			{
				FindLobby();
			}
		}
		TickAPICallRateLimiter();
	}

	internal void FindLobby()
	{
		if (m_serverFilter == null)
		{
			FindLobbiesRequest request = new FindLobbiesRequest
			{
				Filter = m_searchFilter
			};
			QueueAPICall(delegate
			{
				PlayFabMultiplayerAPI.FindLobbies(request, OnFindLobbySuccess, OnFindLobbyFailed);
			});
		}
		else
		{
			FindLobbyWithPagination(m_pages.Dequeue());
		}
	}

	private void FindLobbyWithPagination(int page)
	{
		FindLobbiesRequest request = new FindLobbiesRequest
		{
			Filter = m_searchFilter + string.Format(" and {0} eq {1}", "number_key11", page),
			Pagination = new PaginationRequest
			{
				PageSizeRequested = 50u
			}
		};
		if (m_verboseLog)
		{
			ZLog.Log($"Page {page}, {m_pages.Count} remains: {request.Filter}");
		}
		QueueAPICall(delegate
		{
			PlayFabMultiplayerAPI.FindLobbies(request, OnFindServersSuccess, OnFindLobbyFailed);
		});
	}

	private void RetryOrFail(string error)
	{
		if (m_retries > 0)
		{
			m_retries--;
			m_retryIn = 1f;
		}
		else
		{
			ZLog.Log($"PlayFab lobby matching search filter '{m_searchFilter}': {error}");
			OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
		}
	}

	private void OnFindLobbyFailed(PlayFabError error)
	{
		if (!IsDone)
		{
			RetryOrFail(error.ToString());
		}
	}

	private void OnFindLobbySuccess(FindLobbiesResult result)
	{
		if (IsDone)
		{
			return;
		}
		if (result.Lobbies.Count == 0)
		{
			RetryOrFail("Got back zero lobbies");
			return;
		}
		LobbySummary lobbySummary = result.Lobbies[0];
		if (result.Lobbies.Count > 1)
		{
			ZLog.LogWarning($"Expected zero or one lobby got {result.Lobbies.Count} matching lobbies, returning newest lobby");
			long num = long.Parse(lobbySummary.SearchData["string_key9"]);
			foreach (LobbySummary lobby in result.Lobbies)
			{
				long num2 = long.Parse(lobby.SearchData["string_key9"]);
				if (num < num2)
				{
					lobbySummary = lobby;
					num = num2;
				}
			}
		}
		if (m_joinLobby)
		{
			JoinLobby(lobbySummary.LobbyId, lobbySummary.ConnectionString);
			ZPlayFabMatchmaking.JoinCode = lobbySummary.SearchData["string_key4"];
		}
		else
		{
			DeliverLobby(lobbySummary);
			IsDone = true;
		}
	}

	private void JoinLobby(string lobbyId, string connectionString)
	{
		JoinLobbyRequest request = new JoinLobbyRequest
		{
			ConnectionString = connectionString,
			MemberEntity = ZPlayFabMatchmaking.GetEntityKeyForLocalUser()
		};
		QueueAPICall(delegate
		{
			PlayFabMultiplayerAPI.JoinLobby(request, delegate(JoinLobbyResult result)
			{
				OnJoinLobbySuccess(result.LobbyId);
			}, delegate(PlayFabError error)
			{
				OnJoinLobbyFailed(error, lobbyId);
			});
		});
	}

	private void OnJoinLobbySuccess(string lobbyId)
	{
		if (!IsDone)
		{
			GetLobbyRequest request = new GetLobbyRequest
			{
				LobbyId = lobbyId
			};
			QueueAPICall(delegate
			{
				PlayFabMultiplayerAPI.GetLobby(request, OnGetLobbySuccess, OnGetLobbyFailed);
			});
		}
	}

	private void OnJoinLobbyFailed(PlayFabError error, string lobbyId)
	{
		switch (error.Error)
		{
		case PlayFabErrorCode.LobbyPlayerAlreadyJoined:
			OnJoinLobbySuccess(lobbyId);
			break;
		case PlayFabErrorCode.LobbyNotJoinable:
			ZLog.Log("Can't join lobby because it's not joinable, likely because it's full.");
			OnFailed(ZPLayFabMatchmakingFailReason.ServerFull);
			break;
		case PlayFabErrorCode.APIRequestLimitExceeded:
		case PlayFabErrorCode.APIClientRequestRateLimitExceeded:
		case PlayFabErrorCode.LobbyPlayerMaxLobbyLimitExceeded:
			OnFailed(ZPLayFabMatchmakingFailReason.APIRequestLimitExceeded);
			break;
		default:
			ZLog.LogError("Failed to get lobby: " + error.ToString());
			OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
			break;
		}
	}

	private void DeliverLobby(LobbySummary lobbySummary)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = ToServerData(lobbySummary.LobbyId, lobbySummary.CurrentPlayers, lobbySummary.SearchData, null, subtractOneFromPlayerCountIfDedicated: true);
		if (m_verboseLog && playFabMatchmakingServerData != null)
		{
			ZLog.Log("Deliver server data\n" + playFabMatchmakingServerData.ToString());
		}
		m_successAction(playFabMatchmakingServerData);
		if (m_findFriendsOnly)
		{
			m_failedAction(ZPLayFabMatchmakingFailReason.None);
		}
	}

	private void OnFindServersSuccess(FindLobbiesResult result)
	{
		if (IsDone)
		{
			return;
		}
		foreach (LobbySummary lobby in result.Lobbies)
		{
			if (lobby.SearchData["string_key5"].ToLowerInvariant().Contains(m_serverFilter.ToLowerInvariant()))
			{
				DeliverLobby(lobby);
			}
		}
		if (m_pages.Count == 0)
		{
			OnFailed(ZPLayFabMatchmakingFailReason.None);
		}
		else
		{
			FindLobbyWithPagination(m_pages.Dequeue());
		}
	}

	private void OnGetLobbySuccess(GetLobbyResult result)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = ToServerData(result);
		if (IsDone)
		{
			OnFailed(ZPLayFabMatchmakingFailReason.Cancelled);
			return;
		}
		if (playFabMatchmakingServerData == null)
		{
			OnFailed(ZPLayFabMatchmakingFailReason.InvalidServerData);
			return;
		}
		IsDone = true;
		ZLog.Log("Get Lobby\n" + playFabMatchmakingServerData.ToString());
		m_successAction(playFabMatchmakingServerData);
	}

	private void OnGetLobbyFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to get lobby: " + error.ToString());
		OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	private static PlayFabMatchmakingServerData ToServerData(string lobbyID, uint playerCount, Dictionary<string, string> searchData, Dictionary<string, string> lobbyData = null, bool subtractOneFromPlayerCountIfDedicated = false)
	{
		try
		{
			if (!bool.TryParse(searchData["string_key3"], out var result) || !bool.TryParse(searchData["string_key7"], out var result2) || !long.TryParse(searchData["string_key9"], out var result3))
			{
				ZLog.LogWarning("Got PlayFab lobby entry with invalid data");
				return null;
			}
			string versionString = searchData["string_key6"];
			uint num = uint.Parse(searchData["number_key13"]);
			if (!GameVersion.TryParseGameVersion(versionString, out var version) || version < Version.FirstVersionWithNetworkVersion)
			{
				num = 0u;
			}
			if (num != 27 || !searchData.TryGetValue("string_key14", out var value) || !StringUtils.TryDecodeStringAsIDictionary<Dictionary<string, string>>(value, out var decodedDictionary) || !ServerOptionsGUI.TryConvertCompactKVPToModifierKeys<List<string>>(decodedDictionary, out var result4))
			{
				result4 = new List<string>();
			}
			PlayFabMatchmakingServerData playFabMatchmakingServerData = new PlayFabMatchmakingServerData
			{
				isCommunityServer = result,
				isDedicatedServer = result2,
				joinCode = searchData["string_key4"],
				lobbyId = lobbyID,
				numPlayers = ((result2 && subtractOneFromPlayerCountIfDedicated) ? (playerCount - 1) : playerCount),
				remotePlayerId = searchData["string_key1"],
				serverIp = searchData["string_key10"],
				serverName = searchData["string_key5"],
				tickCreated = result3,
				gameVersion = version,
				modifiers = result4,
				networkVersion = num,
				xboxUserId = searchData["string_key8"],
				platformRestriction = searchData["string_key12"]
			};
			if (lobbyData != null)
			{
				playFabMatchmakingServerData.havePassword = bool.Parse(lobbyData[PlayFabAttrKey.HavePassword.ToKeyString()]);
				playFabMatchmakingServerData.networkId = lobbyData[PlayFabAttrKey.NetworkId.ToKeyString()];
				playFabMatchmakingServerData.worldName = lobbyData[PlayFabAttrKey.WorldName.ToKeyString()];
			}
			return playFabMatchmakingServerData;
		}
		catch (KeyNotFoundException)
		{
			ZLog.LogWarning("Got PlayFab lobby entry with missing key(s)");
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static PlayFabMatchmakingServerData ToServerData(GetLobbyResult result)
	{
		return ToServerData(result.Lobby.LobbyId, (uint)result.Lobby.Members.Count, result.Lobby.SearchData, result.Lobby.LobbyData);
	}

	private void OnFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (!IsDone)
		{
			IsDone = true;
			if (m_failedAction != null)
			{
				m_failedAction(failReason);
			}
		}
	}

	internal void Cancel()
	{
		IsDone = true;
	}

	private void QueueAPICall(QueueableAPICall apiCallDelegate)
	{
		m_APICallQueue.Enqueue(apiCallDelegate);
		TickAPICallRateLimiter();
	}

	private void TickAPICallRateLimiter()
	{
		if (m_APICallQueue.Count > 0 && (DateTime.UtcNow - m_previousAPICallTime).TotalSeconds >= 2.0)
		{
			m_APICallQueue.Dequeue()();
			m_previousAPICallTime = DateTime.UtcNow;
		}
	}
}

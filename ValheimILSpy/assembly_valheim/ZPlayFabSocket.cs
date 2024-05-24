using System;
using System.Collections.Generic;
using PlayFab.Party;
using UnityEngine;

public class ZPlayFabSocket : ZNetStats, IDisposable, ISocket
{
	public class InFlightQueue
	{
		private readonly Queue<byte[]> m_payloads = new Queue<byte[]>();

		private float m_nextResend;

		private uint m_size;

		private uint m_head;

		private uint m_tail;

		public uint Bytes => m_size;

		public uint Head => m_head;

		public uint Tail => m_tail;

		public bool IsEmpty => m_payloads.Count == 0;

		public float NextResend => m_nextResend;

		public void Enqueue(byte[] payload)
		{
			m_payloads.Enqueue(payload);
			m_size += (uint)payload.Length;
			m_head++;
		}

		public void Drop()
		{
			m_size -= (uint)m_payloads.Dequeue().Length;
			m_tail++;
			ResetRetransTimer();
		}

		public byte[] Peek()
		{
			return m_payloads.Peek();
		}

		public void CopyPayloads(List<byte[]> payloads)
		{
			while (m_payloads.Count > 0)
			{
				payloads.Add(m_payloads.Dequeue());
			}
			foreach (byte[] payload in payloads)
			{
				m_payloads.Enqueue(payload);
			}
		}

		public void ResetRetransTimer(bool small = false)
		{
			m_nextResend = Time.time + (small ? 1f : 3f);
		}

		public void ResetAll()
		{
			m_payloads.Clear();
			m_nextResend = 0f;
			m_size = 0u;
			m_head = 0u;
			m_tail = 0u;
		}
	}

	private const byte PAYLOAD_DAT = 17;

	private const byte PAYLOAD_ACK = 42;

	private const byte PAYLOAD_INT = 64;

	private const int PAYLOAD_HEADER_LEN = 5;

	private const float PARTY_RESET_GRACE_SEC = 3f;

	private const float PARTY_RESET_TIMEOUT_SEC = 20f;

	private const float KICKSTART_COOLDOWN = 6f;

	private const float NETWORK_ERROR_WATCHDOG = 26f;

	private const float INFLIGHT_SCALING_FACTOR = 0.25f;

	private const byte INT_PLATFORM_ID = 1;

	private static ZPlayFabSocket s_listenSocket;

	private static readonly Dictionary<string, ZPlayFabSocket> s_connectSockets = new Dictionary<string, ZPlayFabSocket>();

	private static float s_durationToPartyReset;

	private static DateTime s_lastReception;

	private ZPlayFabSocketState m_state;

	private PlayFabPlayer[] m_peer;

	private string m_lobbyId;

	private readonly byte[] m_sndMsg = new byte[5];

	private readonly bool m_isClient;

	private readonly string m_remotePlayerId;

	private string m_platformPlayerId;

	private readonly Queue<ZPackage> m_recvQueue = new Queue<ZPackage>();

	private readonly Dictionary<uint, byte[]> m_outOfOrderQueue = new Dictionary<uint, byte[]>();

	private readonly Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private readonly InFlightQueue m_inFlightQueue = new InFlightQueue();

	private readonly List<byte[]> m_retransmitCache = new List<byte[]>();

	private readonly List<Action> m_delayedInitActions = new List<Action>();

	private readonly PlayFabZLibWorkQueue m_zlibWorkQueue = new PlayFabZLibWorkQueue();

	private readonly Queue<ZPlayFabSocket> m_backlog = new Queue<ZPlayFabSocket>();

	private uint m_next;

	private float m_partyResetTimeout;

	private float m_partyResetConnectTimeout;

	private bool m_partyNetworkLeft;

	private bool m_didRecover;

	private float m_canKickstartIn;

	private bool m_useCompression;

	private Action<PlayFabMatchmakingServerData> m_serverDataFoundCallback;

	public ZPlayFabSocket()
	{
		m_state = ZPlayFabSocketState.LISTEN;
		PlayFabMultiplayerManager.Get().LogLevel = PlayFabMultiplayerManager.LogLevelType.None;
	}

	public ZPlayFabSocket(string remotePlayerId, Action<PlayFabMatchmakingServerData> serverDataFoundCallback)
	{
		PlayFabMultiplayerManager.Get().LogLevel = PlayFabMultiplayerManager.LogLevelType.None;
		m_state = ZPlayFabSocketState.CONNECTING;
		m_remotePlayerId = remotePlayerId;
		ClientConnect();
		PlayFabMultiplayerManager.Get().OnDataMessageReceived += OnDataMessageReceived;
		PlayFabMultiplayerManager.Get().OnRemotePlayerJoined += OnRemotePlayerJoined;
		m_isClient = true;
		m_platformPlayerId = PrivilegeManager.GetNetworkUserId();
		m_serverDataFoundCallback = serverDataFoundCallback;
		ZPackage zPackage = new ZPackage();
		zPackage.Write((byte)1);
		zPackage.Write(m_platformPlayerId);
		Send(zPackage, 64);
		ZLog.Log("PlayFab socket with remote ID " + remotePlayerId + " sent local Platform ID " + GetHostName());
	}

	private void ClientConnect()
	{
		ZPlayFabMatchmaking.CheckHostOnlineStatus(m_remotePlayerId, OnRemotePlayerSessionFound, OnRemotePlayerNotFound, joinLobby: true);
	}

	private ZPlayFabSocket(PlayFabPlayer remotePlayer)
	{
		InitRemotePlayer(remotePlayer);
		Connect(remotePlayer);
		m_isClient = false;
		m_remotePlayerId = remotePlayer.EntityKey.Id;
		PlayFabMultiplayerManager.Get().OnDataMessageReceived += OnDataMessageReceived;
		ZLog.Log("PlayFab listen socket child connected to remote player " + m_remotePlayerId);
	}

	private void InitRemotePlayer(PlayFabPlayer remotePlayer)
	{
		m_delayedInitActions.Add(delegate
		{
			remotePlayer.IsMuted = true;
			ZLog.Log("Muted PlayFab remote player " + remotePlayer.EntityKey.Id);
		});
	}

	private void OnRemotePlayerSessionFound(PlayFabMatchmakingServerData serverData)
	{
		m_serverDataFoundCallback?.Invoke(serverData);
		if (m_state != 0)
		{
			string networkId = PlayFabMultiplayerManager.Get().NetworkId;
			m_lobbyId = serverData.lobbyId;
			if (m_state == ZPlayFabSocketState.CONNECTING)
			{
				ZLog.Log("Joining server '" + serverData.serverName + "' at PlayFab network " + serverData.networkId + " from lobby " + serverData.lobbyId);
				PlayFabMultiplayerManager.Get().JoinNetwork(serverData.networkId);
				PlayFabMultiplayerManager.Get().OnNetworkJoined += OnNetworkJoined;
			}
			else if (networkId == null || networkId != serverData.networkId || m_partyNetworkLeft)
			{
				ZLog.Log("Re-joining server '" + serverData.serverName + "' at new PlayFab network " + serverData.networkId);
				PlayFabMultiplayerManager.Get().JoinNetwork(serverData.networkId);
				m_partyNetworkLeft = false;
			}
			else if (PartyResetInProgress())
			{
				ZLog.Log("Leave server '" + serverData.serverName + "' at new PlayFab network " + serverData.networkId + ", try to re-join later");
				ResetPartyTimeout();
				PlayFabMultiplayerManager.Get().LeaveNetwork();
				m_partyNetworkLeft = true;
			}
		}
	}

	private void OnRemotePlayerNotFound(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.LogWarning("Failed to locate network session for PlayFab player " + m_remotePlayerId);
		switch (failReason)
		{
		case ZPLayFabMatchmakingFailReason.InvalidServerData:
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorVersion);
			break;
		case ZPLayFabMatchmakingFailReason.ServerFull:
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorFull);
			break;
		case ZPLayFabMatchmakingFailReason.APIRequestLimitExceeded:
			ResetPartyTimeout();
			return;
		}
		Close();
	}

	private void CheckReestablishConnection(byte[] maybeCompressedBuffer)
	{
		try
		{
			OnDataMessageReceivedCont(m_zlibWorkQueue.UncompressOnThisThread(maybeCompressedBuffer));
			return;
		}
		catch
		{
		}
		byte msgType = GetMsgType(maybeCompressedBuffer);
		if (GetMsgId(maybeCompressedBuffer) == 0 && msgType == 64)
		{
			ZLog.Log("Assume restarted game session for remote ID " + GetEndPointString() + " and Platform ID " + GetHostName());
			ResetAll();
			OnDataMessageReceivedCont(maybeCompressedBuffer);
		}
	}

	private void ResetAll()
	{
		m_recvQueue.Clear();
		m_outOfOrderQueue.Clear();
		m_sendQueue.Clear();
		m_inFlightQueue.ResetAll();
		m_retransmitCache.Clear();
		m_zlibWorkQueue.Poll(out var _, out var _);
		m_next = 0u;
		m_canKickstartIn = 0f;
		m_useCompression = false;
		m_didRecover = false;
		CancelResetParty();
	}

	private void OnDataMessageReceived(object sender, PlayFabPlayer from, byte[] compressedBuffer)
	{
		if (!(from.EntityKey.Id == m_remotePlayerId))
		{
			return;
		}
		DelayedInit();
		if (m_useCompression)
		{
			if (!m_isClient && m_didRecover)
			{
				CheckReestablishConnection(compressedBuffer);
			}
			else
			{
				m_zlibWorkQueue.Decompress(compressedBuffer);
			}
		}
		else
		{
			OnDataMessageReceivedCont(compressedBuffer);
		}
	}

	private void OnDataMessageReceivedCont(byte[] buffer)
	{
		byte msgType = GetMsgType(buffer);
		uint msgId = GetMsgId(buffer);
		s_lastReception = DateTime.UtcNow;
		IncRecvBytes(buffer.Length);
		if (msgType == 42)
		{
			ProcessAck(msgId);
			return;
		}
		if (m_next != msgId)
		{
			SendAck(m_next);
			if (msgId - m_next < int.MaxValue && !m_outOfOrderQueue.ContainsKey(msgId))
			{
				m_outOfOrderQueue.Add(msgId, buffer);
			}
			return;
		}
		switch (msgType)
		{
		case 17:
			m_recvQueue.Enqueue(new ZPackage(buffer, buffer.Length - 5));
			break;
		case 64:
			InternalReceive(new ZPackage(buffer, buffer.Length - 5));
			break;
		default:
			ZLog.LogError("Unknown message type " + msgType + " received by socket!\nByte array:\n" + BitConverter.ToString(buffer));
			return;
		}
		SendAck(++m_next);
		if (m_outOfOrderQueue.Count != 0)
		{
			TryDeliverOutOfOrder();
		}
	}

	private void ProcessAck(uint msgId)
	{
		while (m_inFlightQueue.Tail != msgId)
		{
			if (m_inFlightQueue.IsEmpty)
			{
				Close();
				break;
			}
			m_inFlightQueue.Drop();
		}
	}

	private void TryDeliverOutOfOrder()
	{
		byte[] value;
		while (m_outOfOrderQueue.TryGetValue(m_next, out value))
		{
			m_outOfOrderQueue.Remove(m_next);
			OnDataMessageReceivedCont(value);
		}
	}

	private void InternalReceive(ZPackage pkg)
	{
		if (pkg.ReadByte() == 1)
		{
			m_platformPlayerId = pkg.ReadString();
			ZLog.Log("PlayFab socket with remote ID " + GetEndPointString() + " received local Platform ID " + GetHostName());
		}
		else
		{
			ZLog.LogError("Unknown data in internal receive! Ignoring");
		}
	}

	private void SendAck(uint nextMsgId)
	{
		SetMsgType(m_sndMsg, 42);
		SetMsgId(m_sndMsg, nextMsgId);
		InternalSend(m_sndMsg);
	}

	private static void SetMsgType(byte[] payload, byte t)
	{
		payload[4] = t;
	}

	private static void SetMsgId(byte[] payload, uint id)
	{
		payload[0] = (byte)id;
		payload[1] = (byte)(id >> 8);
		payload[2] = (byte)(id >> 16);
		payload[3] = (byte)(id >> 24);
	}

	private uint GetMsgId(byte[] buffer)
	{
		int num = buffer.Length - 5;
		return (uint)(0 + buffer[num] + (buffer[num + 1] << 8) + (buffer[num + 2] << 16) + (buffer[num + 3] << 24));
	}

	private byte GetMsgType(byte[] buffer)
	{
		return buffer[buffer.Length - 1];
	}

	private void DelayedInit()
	{
		if (m_delayedInitActions.Count == 0)
		{
			return;
		}
		foreach (Action delayedInitAction in m_delayedInitActions)
		{
			delayedInitAction();
		}
		m_delayedInitActions.Clear();
	}

	private void OnNetworkJoined(object sender, string networkId)
	{
		ZLog.Log("PlayFab client socket to remote player " + m_remotePlayerId + " joined network " + networkId);
		if (m_isClient && m_state == ZPlayFabSocketState.CONNECTED)
		{
			ClientConnect();
		}
		ZRpc.SetLongTimeout(enable: true);
	}

	private void OnRemotePlayerJoined(object sender, PlayFabPlayer player)
	{
		InitRemotePlayer(player);
		if (player.EntityKey.Id == m_remotePlayerId)
		{
			ZLog.Log("PlayFab socket connected to remote player " + m_remotePlayerId);
			Connect(player);
		}
	}

	private void Connect(PlayFabPlayer remotePlayer)
	{
		string id = remotePlayer.EntityKey.Id;
		if (!s_connectSockets.ContainsKey(id))
		{
			s_connectSockets.Add(id, this);
			s_lastReception = DateTime.UtcNow;
		}
		if (m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZLog.Log("Resume TX on " + GetEndPointString());
		}
		m_peer = new PlayFabPlayer[1] { remotePlayer };
		m_state = ZPlayFabSocketState.CONNECTED;
		CancelResetParty();
		if (m_sendQueue.Count > 0)
		{
			m_inFlightQueue.ResetRetransTimer();
			while (m_sendQueue.Count > 0)
			{
				InternalSend(m_sendQueue.Dequeue());
			}
		}
		else
		{
			KickstartAfterRecovery();
		}
	}

	private bool PartyResetInProgress()
	{
		return m_partyResetTimeout > 0f;
	}

	private void CancelResetParty()
	{
		m_didRecover = PartyResetInProgress();
		m_partyNetworkLeft = false;
		m_partyResetTimeout = 0f;
		m_partyResetConnectTimeout = 0f;
		s_durationToPartyReset = 0f;
	}

	private void InternalSend(byte[] payload)
	{
		if (PartyResetInProgress())
		{
			return;
		}
		IncSentBytes(payload.Length);
		if (m_useCompression)
		{
			if (ZNet.instance != null && ZNet.instance.HaveStopped)
			{
				InternalSendCont(m_zlibWorkQueue.CompressOnThisThread(payload));
			}
			else
			{
				m_zlibWorkQueue.Compress(payload);
			}
		}
		else
		{
			InternalSendCont(payload);
		}
	}

	private void InternalSendCont(byte[] compressedPayload)
	{
		if (PartyResetInProgress())
		{
			return;
		}
		if (PlayFabMultiplayerManager.Get().SendDataMessage(compressedPayload, m_peer, DeliveryOption.Guaranteed))
		{
			if (!m_isClient)
			{
				ZPlayFabMatchmaking.ForwardProgress();
			}
			return;
		}
		if (m_isClient)
		{
			ScheduleResetParty();
		}
		ResetPartyTimeout();
		ZLog.Log("Failed to send, suspend TX on " + GetEndPointString() + " while trying to reconnect");
	}

	private void ResetPartyTimeout()
	{
		m_partyResetConnectTimeout = UnityEngine.Random.Range(9f, 11f) + s_durationToPartyReset;
		m_partyResetTimeout = UnityEngine.Random.Range(18f, 22f) + s_durationToPartyReset;
	}

	internal static void ScheduleResetParty()
	{
		if (s_durationToPartyReset <= 0f)
		{
			s_durationToPartyReset = UnityEngine.Random.Range(2.6999998f, 3.3000002f);
		}
	}

	public void Dispose()
	{
		Debug.Log("ZPlayFabSocket::Dispose. State: " + m_state);
		m_zlibWorkQueue.Dispose();
		ResetAll();
		if (m_state == ZPlayFabSocketState.CLOSED)
		{
			return;
		}
		if (m_state == ZPlayFabSocketState.LISTEN)
		{
			s_listenSocket = null;
			foreach (ZPlayFabSocket item in m_backlog)
			{
				item.Close();
			}
		}
		else
		{
			PlayFabMultiplayerManager.Get().OnDataMessageReceived -= OnDataMessageReceived;
		}
		if (!ZNet.instance.IsServer())
		{
			PlayFabMultiplayerManager.Get().OnRemotePlayerJoined -= OnRemotePlayerJoined;
			PlayFabMultiplayerManager.Get().OnNetworkJoined -= OnNetworkJoined;
			PlayFabMultiplayerManager.Get().LeaveNetwork();
		}
		if (m_state == ZPlayFabSocketState.CONNECTED)
		{
			s_connectSockets.Remove(m_peer[0].EntityKey.Id);
		}
		Debug.Log("ZPlayFabSocket::Dispose. leave lobby. LobbyId: " + m_lobbyId);
		if (m_lobbyId != null)
		{
			ZPlayFabMatchmaking.LeaveLobby(m_lobbyId);
		}
		else
		{
			ZPlayFabMatchmaking.LeaveEmptyLobby();
		}
		m_state = ZPlayFabSocketState.CLOSED;
	}

	private void Update(float dt)
	{
		if (m_canKickstartIn >= 0f)
		{
			m_canKickstartIn -= dt;
		}
		if (!m_isClient)
		{
			return;
		}
		if (PartyResetInProgress())
		{
			m_partyResetTimeout -= dt;
			if (m_partyResetConnectTimeout > 0f)
			{
				m_partyResetConnectTimeout -= dt;
				if (m_partyResetConnectTimeout <= 0f)
				{
					ClientConnect();
				}
			}
		}
		else if ((DateTime.UtcNow - s_lastReception).TotalSeconds >= 26.0 && m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZLog.Log("Do a reset party as nothing seems to be received");
			ResetPartyTimeout();
			PlayFabMultiplayerManager.Get().ResetParty();
		}
	}

	private void LateUpdate()
	{
		m_zlibWorkQueue.Poll(out var compressedBuffers, out var decompressedBuffers);
		if (compressedBuffers != null)
		{
			foreach (byte[] item in compressedBuffers)
			{
				InternalSendCont(item);
			}
		}
		if (decompressedBuffers == null)
		{
			return;
		}
		foreach (byte[] item2 in decompressedBuffers)
		{
			OnDataMessageReceivedCont(item2);
		}
	}

	public bool IsConnected()
	{
		if (m_state != ZPlayFabSocketState.CONNECTED)
		{
			return m_state == ZPlayFabSocketState.CONNECTING;
		}
		return true;
	}

	public void VersionMatch()
	{
		m_useCompression = true;
	}

	public void Send(ZPackage pkg, byte messageType)
	{
		if (pkg.Size() != 0 && IsConnected())
		{
			pkg.Write(m_inFlightQueue.Head);
			pkg.Write(messageType);
			byte[] array = pkg.GetArray();
			m_inFlightQueue.Enqueue(array);
			if (m_state == ZPlayFabSocketState.CONNECTED)
			{
				InternalSend(array);
			}
			else
			{
				m_sendQueue.Enqueue(array);
			}
		}
	}

	public void Send(ZPackage pkg)
	{
		Send(pkg, 17);
	}

	public ZPackage Recv()
	{
		CheckRetransmit();
		if (!GotNewData())
		{
			return null;
		}
		return m_recvQueue.Dequeue();
	}

	private void CheckRetransmit()
	{
		if (!m_inFlightQueue.IsEmpty && !PartyResetInProgress() && m_state == ZPlayFabSocketState.CONNECTED && !(Time.time < m_inFlightQueue.NextResend))
		{
			DoRetransmit();
		}
	}

	private void DoRetransmit(bool canKickstart = true)
	{
		if (canKickstart && CanKickstartRatelimit())
		{
			KickstartAfterRecovery();
		}
		else if (!m_inFlightQueue.IsEmpty)
		{
			InternalSend(m_inFlightQueue.Peek());
			m_inFlightQueue.ResetRetransTimer(small: true);
		}
	}

	private bool CanKickstartRatelimit()
	{
		return m_canKickstartIn <= 0f;
	}

	private void KickstartAfterRecovery()
	{
		try
		{
			TryKickstartAfterRecovery();
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Failed to resend data on $" + GetEndPointString() + ", closing socket: " + ex.Message);
			Close();
		}
	}

	private void TryKickstartAfterRecovery()
	{
		if (!m_inFlightQueue.IsEmpty)
		{
			m_inFlightQueue.CopyPayloads(m_retransmitCache);
			foreach (byte[] item in m_retransmitCache)
			{
				InternalSend(item);
			}
			m_retransmitCache.Clear();
			m_inFlightQueue.ResetRetransTimer();
		}
		m_canKickstartIn = 6f;
	}

	public int GetSendQueueSize()
	{
		return (int)((float)m_inFlightQueue.Bytes * 0.25f);
	}

	public int GetCurrentSendRate()
	{
		throw new NotImplementedException();
	}

	internal void StartHost()
	{
		if (s_listenSocket != null)
		{
			ZLog.LogError("Multiple PlayFab listen sockets");
		}
		else
		{
			s_listenSocket = this;
		}
	}

	public bool IsHost()
	{
		return m_state == ZPlayFabSocketState.LISTEN;
	}

	public bool GotNewData()
	{
		return m_recvQueue.Count > 0;
	}

	public string GetEndPointString()
	{
		string text = "";
		if (m_peer != null)
		{
			text = m_peer[0].EntityKey.Id;
		}
		return "playfab/" + text;
	}

	public ISocket Accept()
	{
		if (m_backlog.Count == 0)
		{
			return null;
		}
		ZRpc.SetLongTimeout(enable: true);
		return m_backlog.Dequeue();
	}

	public int GetHostPort()
	{
		if (!IsHost())
		{
			return -1;
		}
		return 0;
	}

	public bool Flush()
	{
		throw new NotImplementedException();
	}

	public string GetHostName()
	{
		return m_platformPlayerId;
	}

	public void Close()
	{
		Dispose();
	}

	internal static void LostConnection(PlayFabPlayer player)
	{
		string id = player.EntityKey.Id;
		if (s_connectSockets.TryGetValue(id, out var value))
		{
			ZLog.Log("Keep socket for " + value.GetEndPointString() + ", try to reconnect before timeout");
		}
	}

	internal static void QueueConnection(PlayFabPlayer player)
	{
		string id = player.EntityKey.Id;
		if (s_connectSockets.TryGetValue(id, out var value))
		{
			ZLog.Log("Resume TX on " + value.GetEndPointString());
			value.Connect(player);
		}
		else if (s_listenSocket != null)
		{
			s_listenSocket.m_backlog.Enqueue(new ZPlayFabSocket(player));
		}
		else
		{
			ZLog.LogError("Incoming PlayFab connection without any open listen socket");
		}
	}

	internal static void DestroyListenSocket()
	{
		while (s_connectSockets.Count > 0)
		{
			Dictionary<string, ZPlayFabSocket>.Enumerator enumerator = s_connectSockets.GetEnumerator();
			enumerator.MoveNext();
			enumerator.Current.Value.Close();
		}
		s_listenSocket.Close();
		s_listenSocket = null;
	}

	internal static uint NumSockets()
	{
		return (uint)s_connectSockets.Count;
	}

	internal static void UpdateAllSockets(float dt)
	{
		if (s_durationToPartyReset > 0f)
		{
			s_durationToPartyReset -= dt;
			if (s_durationToPartyReset < 0f)
			{
				ZLog.Log("Reset party to clear network error");
				PlayFabMultiplayerManager.Get().ResetParty();
			}
		}
		foreach (ZPlayFabSocket value in s_connectSockets.Values)
		{
			value.Update(dt);
		}
	}

	internal static void LateUpdateAllSocket()
	{
		foreach (ZPlayFabSocket value in s_connectSockets.Values)
		{
			value.LateUpdate();
		}
	}
}

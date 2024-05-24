using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Steamworks;
using UnityEngine;

public class ZSteamSocket : IDisposable, ISocket
{
	private static List<ZSteamSocket> m_sockets = new List<ZSteamSocket>();

	private static Callback<SteamNetConnectionStatusChangedCallback_t> m_statusChanged;

	private static int m_steamDataPort = 2459;

	private Queue<ZSteamSocket> m_pendingConnections = new Queue<ZSteamSocket>();

	private HSteamNetConnection m_con = HSteamNetConnection.Invalid;

	private SteamNetworkingIdentity m_peerID;

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;

	private HSteamListenSocket m_listenSocket = HSteamListenSocket.Invalid;

	private static ZSteamSocket m_hostSocket;

	private static ESteamNetworkingConfigValue[] m_configValues = new ESteamNetworkingConfigValue[1];

	public ZSteamSocket()
	{
		RegisterGlobalCallbacks();
		m_sockets.Add(this);
	}

	public ZSteamSocket(SteamNetworkingIPAddr host)
	{
		RegisterGlobalCallbacks();
		host.ToString(out var buf, bWithPort: true);
		ZLog.Log("Starting to connect to " + buf);
		m_con = SteamNetworkingSockets.ConnectByIPAddress(ref host, 0, null);
		m_sockets.Add(this);
	}

	public ZSteamSocket(CSteamID peerID)
	{
		RegisterGlobalCallbacks();
		m_peerID.SetSteamID(peerID);
		m_con = SteamNetworkingSockets.ConnectP2P(ref m_peerID, 0, 0, null);
		ZLog.Log("Connecting to " + m_peerID.GetSteamID().ToString());
		m_sockets.Add(this);
	}

	public ZSteamSocket(HSteamNetConnection con)
	{
		RegisterGlobalCallbacks();
		m_con = con;
		SteamNetworkingSockets.GetConnectionInfo(m_con, out var pInfo);
		m_peerID = pInfo.m_identityRemote;
		ZLog.Log("Connecting to " + m_peerID);
		m_sockets.Add(this);
	}

	private static void RegisterGlobalCallbacks()
	{
		if (m_statusChanged == null)
		{
			m_statusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnStatusChanged);
			GCHandle gCHandle = GCHandle.Alloc(30000f, GCHandleType.Pinned);
			GCHandle gCHandle2 = GCHandle.Alloc(1, GCHandleType.Pinned);
			GCHandle gCHandle3 = GCHandle.Alloc(153600, GCHandleType.Pinned);
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float, gCHandle.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_IP_AllowWithoutAuth, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle2.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle3.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gCHandle3.AddrOfPinnedObject());
			gCHandle.Free();
			gCHandle2.Free();
			gCHandle3.Free();
		}
	}

	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + m_sockets.Count);
		if (m_statusChanged != null)
		{
			m_statusChanged.Dispose();
			m_statusChanged = null;
		}
	}

	private static void OnStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
	{
		ZLog.Log("Got status changed msg " + data.m_info.m_eState);
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected && data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
		{
			ZLog.Log("Connected");
			ZSteamSocket zSteamSocket = FindSocket(data.m_hConn);
			if (zSteamSocket != null)
			{
				if (SteamNetworkingSockets.GetConnectionInfo(data.m_hConn, out var pInfo))
				{
					zSteamSocket.m_peerID = pInfo.m_identityRemote;
				}
				ZLog.Log("Got connection SteamID " + zSteamSocket.m_peerID.GetSteamID().ToString());
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting && data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None)
		{
			ZLog.Log("New connection");
			GetListner()?.OnNewConnection(data.m_hConn);
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
		{
			ZLog.Log("Got problem " + data.m_info.m_eEndReason + ":" + data.m_info.m_szEndDebug);
			ZSteamSocket zSteamSocket2 = FindSocket(data.m_hConn);
			if (zSteamSocket2 != null)
			{
				ZLog.Log("  Closing socket " + zSteamSocket2.GetHostName());
				zSteamSocket2.Close();
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer)
		{
			ZLog.Log("Socket closed by peer " + data);
			ZSteamSocket zSteamSocket3 = FindSocket(data.m_hConn);
			if (zSteamSocket3 != null)
			{
				ZLog.Log("  Closing socket " + zSteamSocket3.GetHostName());
				zSteamSocket3.Close();
			}
		}
	}

	private static ZSteamSocket FindSocket(HSteamNetConnection con)
	{
		foreach (ZSteamSocket socket in m_sockets)
		{
			if (socket.m_con == con)
			{
				return socket;
			}
		}
		return null;
	}

	public void Dispose()
	{
		ZLog.Log("Disposing socket");
		Close();
		m_pkgQueue.Clear();
		m_sockets.Remove(this);
		if (m_sockets.Count == 0)
		{
			ZLog.Log("Last socket, unregistering callback");
			UnregisterGlobalCallbacks();
		}
	}

	public void Close()
	{
		if (m_con != HSteamNetConnection.Invalid)
		{
			ZLog.Log("Closing socket " + GetEndPointString());
			Flush();
			ZLog.Log("  send queue size:" + m_sendQueue.Count);
			Thread.Sleep(100);
			CSteamID steamID = m_peerID.GetSteamID();
			SteamNetworkingSockets.CloseConnection(m_con, 0, "", bEnableLinger: false);
			SteamUser.EndAuthSession(steamID);
			m_con = HSteamNetConnection.Invalid;
		}
		if (m_listenSocket != HSteamListenSocket.Invalid)
		{
			ZLog.Log("Stopping listening socket");
			SteamNetworkingSockets.CloseListenSocket(m_listenSocket);
			m_listenSocket = HSteamListenSocket.Invalid;
		}
		if (m_hostSocket == this)
		{
			m_hostSocket = null;
		}
		m_peerID.Clear();
	}

	public bool StartHost()
	{
		if (m_hostSocket != null)
		{
			ZLog.Log("Listen socket already started");
			return false;
		}
		m_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
		m_hostSocket = this;
		m_pendingConnections.Clear();
		return true;
	}

	private void OnNewConnection(HSteamNetConnection con)
	{
		EResult eResult = SteamNetworkingSockets.AcceptConnection(con);
		ZLog.Log("Accepting connection " + eResult);
		if (eResult == EResult.k_EResultOK)
		{
			QueuePendingConnection(con);
		}
	}

	private void QueuePendingConnection(HSteamNetConnection con)
	{
		ZSteamSocket item = new ZSteamSocket(con);
		m_pendingConnections.Enqueue(item);
	}

	public ISocket Accept()
	{
		if (m_listenSocket == HSteamListenSocket.Invalid)
		{
			return null;
		}
		if (m_pendingConnections.Count > 0)
		{
			return m_pendingConnections.Dequeue();
		}
		return null;
	}

	public bool IsConnected()
	{
		return m_con != HSteamNetConnection.Invalid;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() != 0 && IsConnected())
		{
			byte[] array = pkg.GetArray();
			m_sendQueue.Enqueue(array);
			SendQueuedPackages();
		}
	}

	public bool Flush()
	{
		SendQueuedPackages();
		_ = m_con;
		SteamNetworkingSockets.FlushMessagesOnConnection(m_con);
		return m_sendQueue.Count == 0;
	}

	private void SendQueuedPackages()
	{
		if (!IsConnected())
		{
			return;
		}
		while (m_sendQueue.Count > 0)
		{
			byte[] array = m_sendQueue.Peek();
			IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
			Marshal.Copy(array, 0, intPtr, array.Length);
			long pOutMessageNumber;
			EResult eResult = SteamNetworkingSockets.SendMessageToConnection(m_con, intPtr, (uint)array.Length, 8, out pOutMessageNumber);
			Marshal.FreeHGlobal(intPtr);
			if (eResult == EResult.k_EResultOK)
			{
				m_totalSent += array.Length;
				m_sendQueue.Dequeue();
				continue;
			}
			ZLog.Log("Failed to send data " + eResult);
			break;
		}
	}

	public static void UpdateAllSockets(float dt)
	{
		foreach (ZSteamSocket socket in m_sockets)
		{
			socket.Update(dt);
		}
	}

	private void Update(float dt)
	{
		SendQueuedPackages();
	}

	private static ZSteamSocket GetListner()
	{
		return m_hostSocket;
	}

	public ZPackage Recv()
	{
		if (!IsConnected())
		{
			return null;
		}
		IntPtr[] array = new IntPtr[1];
		if (SteamNetworkingSockets.ReceiveMessagesOnConnection(m_con, array, 1) == 1)
		{
			SteamNetworkingMessage_t steamNetworkingMessage_t = Marshal.PtrToStructure<SteamNetworkingMessage_t>(array[0]);
			byte[] array2 = new byte[steamNetworkingMessage_t.m_cbSize];
			Marshal.Copy(steamNetworkingMessage_t.m_pData, array2, 0, steamNetworkingMessage_t.m_cbSize);
			ZPackage zPackage = new ZPackage(array2);
			SteamNetworkingMessage_t.Release(array[0]);
			m_totalRecv += zPackage.Size();
			m_gotData = true;
			return zPackage;
		}
		return null;
	}

	public string GetEndPointString()
	{
		return m_peerID.GetSteamID().ToString();
	}

	public string GetHostName()
	{
		return m_peerID.GetSteamID().ToString();
	}

	public CSteamID GetPeerID()
	{
		return m_peerID.GetSteamID();
	}

	public bool IsHost()
	{
		return m_hostSocket != null;
	}

	public int GetSendQueueSize()
	{
		if (!IsConnected())
		{
			return 0;
		}
		int num = 0;
		foreach (byte[] item in m_sendQueue)
		{
			num += item.Length;
		}
		SteamNetConnectionRealTimeStatus_t pStatus = default(SteamNetConnectionRealTimeStatus_t);
		SteamNetConnectionRealTimeLaneStatus_t pLanes = default(SteamNetConnectionRealTimeLaneStatus_t);
		if (SteamNetworkingSockets.GetConnectionRealTimeStatus(m_con, ref pStatus, 0, ref pLanes) == EResult.k_EResultOK)
		{
			num += pStatus.m_cbPendingReliable + pStatus.m_cbPendingUnreliable + pStatus.m_cbSentUnackedReliable;
		}
		return num;
	}

	public int GetCurrentSendRate()
	{
		SteamNetConnectionRealTimeStatus_t pStatus = default(SteamNetConnectionRealTimeStatus_t);
		SteamNetConnectionRealTimeLaneStatus_t pLanes = default(SteamNetConnectionRealTimeLaneStatus_t);
		if (SteamNetworkingSockets.GetConnectionRealTimeStatus(m_con, ref pStatus, 0, ref pLanes) != EResult.k_EResultOK)
		{
			return 0;
		}
		int num = pStatus.m_cbPendingReliable + pStatus.m_cbPendingUnreliable + pStatus.m_cbSentUnackedReliable;
		foreach (byte[] item in m_sendQueue)
		{
			num += item.Length;
		}
		return num / Mathf.Clamp(pStatus.m_nPing, 5, 250) * 1000;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		SteamNetConnectionRealTimeStatus_t pStatus = default(SteamNetConnectionRealTimeStatus_t);
		SteamNetConnectionRealTimeLaneStatus_t pLanes = default(SteamNetConnectionRealTimeLaneStatus_t);
		if (SteamNetworkingSockets.GetConnectionRealTimeStatus(m_con, ref pStatus, 0, ref pLanes) == EResult.k_EResultOK)
		{
			localQuality = pStatus.m_flConnectionQualityLocal;
			remoteQuality = pStatus.m_flConnectionQualityRemote;
			ping = pStatus.m_nPing;
			outByteSec = pStatus.m_flOutBytesPerSec;
			inByteSec = pStatus.m_flInBytesPerSec;
		}
		else
		{
			localQuality = 0f;
			remoteQuality = 0f;
			ping = 0;
			outByteSec = 0f;
			inByteSec = 0f;
		}
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = m_totalSent;
		totalRecv = m_totalRecv;
		m_totalSent = 0;
		m_totalRecv = 0;
	}

	public bool GotNewData()
	{
		bool gotData = m_gotData;
		m_gotData = false;
		return gotData;
	}

	public int GetHostPort()
	{
		if (IsHost())
		{
			return 1;
		}
		return -1;
	}

	public static void SetDataPort(int port)
	{
		m_steamDataPort = port;
	}

	public void VersionMatch()
	{
	}
}

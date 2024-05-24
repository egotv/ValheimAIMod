using System;
using System.Collections.Generic;
using System.Threading;
using Steamworks;

public class ZSteamSocketOLD : IDisposable, ISocket
{
	private static List<ZSteamSocketOLD> m_sockets = new List<ZSteamSocketOLD>();

	private static Callback<P2PSessionRequest_t> m_SessionRequest;

	private static Callback<P2PSessionConnectFail_t> m_connectionFailed;

	private Queue<ZSteamSocketOLD> m_pendingConnections = new Queue<ZSteamSocketOLD>();

	private CSteamID m_peerID = CSteamID.Nil;

	private bool m_listner;

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;

	public ZSteamSocketOLD()
	{
		m_sockets.Add(this);
		RegisterGlobalCallbacks();
	}

	public ZSteamSocketOLD(CSteamID peerID)
	{
		m_sockets.Add(this);
		m_peerID = peerID;
		RegisterGlobalCallbacks();
	}

	private static void RegisterGlobalCallbacks()
	{
		if (m_connectionFailed == null)
		{
			ZLog.Log("ZSteamSocketOLD  Registering global callbacks");
			m_connectionFailed = Callback<P2PSessionConnectFail_t>.Create(OnConnectionFailed);
		}
		if (m_SessionRequest == null)
		{
			m_SessionRequest = Callback<P2PSessionRequest_t>.Create(OnSessionRequest);
		}
	}

	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + m_sockets.Count);
		if (m_connectionFailed != null)
		{
			m_connectionFailed.Dispose();
			m_connectionFailed = null;
		}
		if (m_SessionRequest != null)
		{
			m_SessionRequest.Dispose();
			m_SessionRequest = null;
		}
	}

	private static void OnConnectionFailed(P2PSessionConnectFail_t data)
	{
		CSteamID steamIDRemote = data.m_steamIDRemote;
		ZLog.Log("Got connection failed callback: " + steamIDRemote.ToString());
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			if (socket.IsPeer(data.m_steamIDRemote))
			{
				socket.Close();
			}
		}
	}

	private static void OnSessionRequest(P2PSessionRequest_t data)
	{
		CSteamID steamIDRemote = data.m_steamIDRemote;
		ZLog.Log("Got session request from " + steamIDRemote.ToString());
		if (SteamNetworking.AcceptP2PSessionWithUser(data.m_steamIDRemote))
		{
			GetListner()?.QueuePendingConnection(data.m_steamIDRemote);
		}
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
		ZLog.Log("Closing socket " + GetEndPointString());
		if (m_peerID != CSteamID.Nil)
		{
			Flush();
			ZLog.Log("  send queue size:" + m_sendQueue.Count);
			Thread.Sleep(100);
			SteamNetworking.GetP2PSessionState(m_peerID, out var pConnectionState);
			ZLog.Log("  P2P state, bytes in send queue:" + pConnectionState.m_nBytesQueuedForSend);
			SteamNetworking.CloseP2PSessionWithUser(m_peerID);
			SteamUser.EndAuthSession(m_peerID);
			m_peerID = CSteamID.Nil;
		}
		m_listner = false;
	}

	public bool StartHost()
	{
		m_listner = true;
		m_pendingConnections.Clear();
		return true;
	}

	private ZSteamSocketOLD QueuePendingConnection(CSteamID id)
	{
		foreach (ZSteamSocketOLD pendingConnection in m_pendingConnections)
		{
			if (pendingConnection.IsPeer(id))
			{
				return pendingConnection;
			}
		}
		ZSteamSocketOLD zSteamSocketOLD = new ZSteamSocketOLD(id);
		m_pendingConnections.Enqueue(zSteamSocketOLD);
		return zSteamSocketOLD;
	}

	public ISocket Accept()
	{
		if (!m_listner)
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
		return m_peerID != CSteamID.Nil;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() != 0 && IsConnected())
		{
			byte[] array = pkg.GetArray();
			byte[] bytes = BitConverter.GetBytes(array.Length);
			byte[] array2 = new byte[array.Length + bytes.Length];
			bytes.CopyTo(array2, 0);
			array.CopyTo(array2, 4);
			m_sendQueue.Enqueue(array);
			SendQueuedPackages();
		}
	}

	public bool Flush()
	{
		SendQueuedPackages();
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
			EP2PSend eP2PSendType = EP2PSend.k_EP2PSendReliable;
			if (SteamNetworking.SendP2PPacket(m_peerID, array, (uint)array.Length, eP2PSendType))
			{
				m_totalSent += array.Length;
				m_sendQueue.Dequeue();
				continue;
			}
			break;
		}
	}

	public static void Update()
	{
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			socket.SendQueuedPackages();
		}
		ReceivePackages();
	}

	private static void ReceivePackages()
	{
		uint pcubMsgSize;
		while (SteamNetworking.IsP2PPacketAvailable(out pcubMsgSize))
		{
			byte[] array = new byte[pcubMsgSize];
			if (SteamNetworking.ReadP2PPacket(array, pcubMsgSize, out var _, out var psteamIDRemote))
			{
				QueueNewPkg(psteamIDRemote, array);
				continue;
			}
			break;
		}
	}

	private static void QueueNewPkg(CSteamID sender, byte[] data)
	{
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			if (socket.IsPeer(sender))
			{
				socket.QueuePackage(data);
				return;
			}
		}
		ZSteamSocketOLD listner = GetListner();
		if (listner != null)
		{
			CSteamID cSteamID = sender;
			ZLog.Log("Got package from unconnected peer " + cSteamID.ToString());
			listner.QueuePendingConnection(sender).QueuePackage(data);
		}
		else
		{
			CSteamID cSteamID = sender;
			ZLog.Log("Got package from unkown peer " + cSteamID.ToString() + " but no active listner");
		}
	}

	private static ZSteamSocketOLD GetListner()
	{
		foreach (ZSteamSocketOLD socket in m_sockets)
		{
			if (socket.IsHost())
			{
				return socket;
			}
		}
		return null;
	}

	private void QueuePackage(byte[] data)
	{
		ZPackage item = new ZPackage(data);
		m_pkgQueue.Enqueue(item);
		m_gotData = true;
		m_totalRecv += data.Length;
	}

	public ZPackage Recv()
	{
		if (!IsConnected())
		{
			return null;
		}
		if (m_pkgQueue.Count > 0)
		{
			return m_pkgQueue.Dequeue();
		}
		return null;
	}

	public string GetEndPointString()
	{
		return m_peerID.ToString();
	}

	public string GetHostName()
	{
		return m_peerID.ToString();
	}

	public CSteamID GetPeerID()
	{
		return m_peerID;
	}

	public bool IsPeer(CSteamID peer)
	{
		if (!IsConnected())
		{
			return false;
		}
		return peer == m_peerID;
	}

	public bool IsHost()
	{
		return m_listner;
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
		return num;
	}

	public bool IsSending()
	{
		if (!IsConnected())
		{
			return false;
		}
		return m_sendQueue.Count > 0;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
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

	public int GetCurrentSendRate()
	{
		return 0;
	}

	public int GetAverageSendRate()
	{
		return 0;
	}

	public int GetHostPort()
	{
		if (IsHost())
		{
			return 1;
		}
		return -1;
	}

	public void VersionMatch()
	{
	}
}
